using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.Finance;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipRate.Application.Interfaces
{
    public interface ITaxProviderClient
    {
        Task<TaxProviderSendResult> SendInvoiceAsync(Invoice invoice, CancellationToken ct);
    }
    public class AvatspTaxProviderClient : ITaxProviderClient
    {
        private readonly HttpClient _httpClient;
        private readonly AvatspOptions _options;
        private readonly ILogger<AvatspTaxProviderClient> _logger;


        // متغیرهای استاتیک برای کش کردن توکن ۲۴ ساعته در سطح پروسس
        private static string? _cachedToken;
        private static DateTime _tokenExpiresAt = DateTime.MinValue;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);




        public AvatspTaxProviderClient(HttpClient httpClient, ILogger<AvatspTaxProviderClient> logger, IOptions<AvatspOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;

            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://avatsp.net" : _options.BaseUrl;
            _httpClient.BaseAddress = new Uri(baseUrl);

        }

        private async Task<string?> GetValidTokenAsync(CancellationToken cancellationToken)
        {
            // اگر توکن موجود است و حداقل ۱۰ دقیقه دیگر اعتبار دارد، از همان استفاده کن
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddMinutes(10) < _tokenExpiresAt)
            {
                return _cachedToken;
            }

            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow.AddMinutes(10) < _tokenExpiresAt)
                {
                    return _cachedToken;
                }

                var loginPayload = new
                {
                    username = _options.Username,
                    password = _options.Password
                };

                var content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/auth/login", content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Avatsp Login Failed with status {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                // استخراج توکن از مسیر data.token طبق مستندات
                if (doc.RootElement.TryGetProperty("data", out var dataElem) &&
                    dataElem.TryGetProperty("token", out var tokenElem))
                {
                    _cachedToken = tokenElem.GetString();
                    _tokenExpiresAt = DateTime.UtcNow.AddHours(23); // توکن ۲۴ ساعت معتبر است
                    return _cachedToken;
                }

                return null;
            }
            finally
            {
                _tokenLock.Release();
            }
        }
        public async Task<TaxProviderSendResult> SendInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            try
            {
                // ۱. دریافت توکن معتبر
                var token = await GetValidTokenAsync(cancellationToken);
                if (string.IsNullOrEmpty(token))
                {
                    return new TaxProviderSendResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "احراز هویت با وب‌سرویس معتمد ناموفق بود (نام کاربری یا رمز عبور نامعتبر است)."
                    };
                }

                // تنظیم هدر Bearer
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // ۲. ساخت پلود ثبت فاکتور (SAVE)
                var savePayload = BuildSavePayload(invoice, Convert.ToInt64(_options.CartableId));
                var saveJson = JsonSerializer.Serialize(savePayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                var saveContent = new StringContent(saveJson, Encoding.UTF8, "application/json");

                // ۳. گام اول: ثبت صورتحساب (POST /api/v1/invoice)
                var saveResponse = await _httpClient.PostAsync("/api/v1/invoice", saveContent, cancellationToken);
                var saveResponseBody = await saveResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!saveResponse.IsSuccessStatusCode)
                {
                    // اگر 401 داد، توکن منقضی شده؛ کش را خالی می‌کنیم برای درخواست بعدی
                    if (saveResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _cachedToken = null;
                    }

                    return new TaxProviderSendResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"خطا در ثبت صورتحساب ({saveResponse.StatusCode}): {saveResponseBody}"
                    };
                }

                using var doc = JsonDocument.Parse(saveResponseBody);
                long avatspInvoiceId = 0;
                if (doc.RootElement.TryGetProperty("id", out var idElement))
                {
                    avatspInvoiceId = idElement.GetInt64();
                }

                if (avatspInvoiceId == 0)
                {
                    return new TaxProviderSendResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "شناسه فاکتور بازگردانده شده از معتمد نامعتبر است."
                    };
                }

                // ۴. گام دوم: ارسال به سامانه مؤدیان (POST /api/v1/invoice/send)
                var sendUrl = $"/api/v1/invoice/send?cartableId={_options.CartableId}&invoiceId={avatspInvoiceId}";
                var sendResponse = await _httpClient.PostAsync(sendUrl, new StringContent("", Encoding.UTF8, "application/json"), cancellationToken);

                if (sendResponse.IsSuccessStatusCode)
                {
                    return new TaxProviderSendResult
                    {
                        IsSuccess = true,
                        TrackingUid = avatspInvoiceId.ToString(),
                        Status = "SENT_TO_MOADIAN"
                    };
                }
                else
                {
                    var sendError = await sendResponse.Content.ReadAsStringAsync(cancellationToken);
                    return new TaxProviderSendResult
                    {
                        IsSuccess = false,
                        TrackingUid = avatspInvoiceId.ToString(),
                        ErrorMessage = $"صورتحساب ثبت شد ولی ارسال به مؤدیان ناموفق بود ({sendResponse.StatusCode}): {sendError}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice {InvoiceId} to Avatsp", invoice.Id);
                return new TaxProviderSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }


        private object BuildSavePayload(Invoice invoice, long cartableId)
        {
            // طبق مستندات فن‌آوا، تاریخ باید شمسی yyyy/MM/dd باشد
            var pc = new System.Globalization.PersianCalendar();
            var dt = invoice.InvoiceDate ?? DateTime.Now;
            string shamsiDate = $"{pc.GetYear(dt):0000}/{pc.GetMonth(dt):00}/{pc.GetDayOfMonth(dt):00}";

            var bodyItems = new List<object>();
            decimal tprdis = 0, tdis = 0, tadis = 0, tvam = 0, tbill = 0;

            if (invoice.InvoiceItems != null)
            {
                foreach (var item in invoice.InvoiceItems)
                {
                    decimal qty = item.QTY > 0 ? item.QTY : 1;
                    decimal fee = item.UnitPrice;
                    decimal prdis = qty * fee;
                    decimal dis = item.Discount;
                    decimal adis = prdis - dis;
                    decimal vra = item.TaxPercent;
                    decimal vam = item.TaxAmount > 0 ? item.TaxAmount : Math.Round(adis * (vra / 100m));
                    decimal tsstam = adis + vam;

                    tprdis += prdis;
                    tdis += dis;
                    tadis += adis;
                    tvam += vam;
                    tbill += tsstam;

                    bodyItems.Add(new
                    {
                        sstid = item.ChargeType.ChargeTypeCode2, // شناسه ۱۳ رقمی کالا/خدمت سازمان مالیاتی
                        sstt = item.Description ?? "خدمات حمل و نقل بین‌المللی",
                        am = qty,
                        mu = "164",
                        fee = fee,
                        prdis = prdis,
                        dis = dis,
                        adis = adis,
                        vra = vra,
                        vam = vam,
                        tsstam = tsstam
                    });
                }
            }

            return new
            {
                cartableId = cartableId,
                status = "SAVE",
                inputType = "APP",
                invoiceDetail = new
                {
                    header = new
                    {
                        indatim = shamsiDate, // مثلاً 1403/08/10
                        inty = "1",          // رشته "1" طبق مستندات فن‌آوا
                        inp = "1",           // الگوی اول (فروش)
                        ins = "1",           // موضوع: اصلی
                        inno = invoice.InvoiceNo,
                        tob = "1",           // ۱: شخص حقوقی، ۲: حقیقی
                        tinb = invoice.Party.NationalID,
                        bpc = invoice.Party.ZipCode,
                        tprdis = tprdis,
                        tdis = tdis,
                        tadis = tadis,
                        tvam = tvam,
                        tbill = tbill,
                        setm = "1"           // نقدی
                    },
                    body = bodyItems
                }
            };
        }
    }
    // کلاس‌های کمکی برای پاسخ‌های API
    public class AvatspLoginResponse
    {
        public string? Token { get; set; }
    }

    public class AvatspApiResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public AvatspApiData? Data { get; set; }
    }

    public class AvatspApiData
    {
        public string? Uid { get; set; }
        public string? TrackingNo { get; set; }
    }

}
