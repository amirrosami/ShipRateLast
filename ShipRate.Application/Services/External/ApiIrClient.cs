using ShipRate.Application.DTOs.ApiIr;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipRate.Application.Services.External
{
    public static class ApiIrClient
    {
        /// <summary>یک نمونه‌ی مشترک برای کل SDK — ساخت HttpClient به ازای هر درخواست باعث Socket Exhaustion می‌شود</summary>
        private static readonly HttpClient Http = CreateClient();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // متن فارسی بدون escape ارسال شود
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // پارامترهای اختیاری که null هستند ارسال نشوند
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // success و Success هر دو خوانده شوند
            PropertyNameCaseInsensitive = true,
            // در OpenAPI فیلدهای عددی می‌توانند به‌صورت رشته هم بیایند ("code": "0")
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();

            if (!ApiIrConfig.SslVerify)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return new HttpClient(handler);
        }

        /// <summary>ارسال درخواست به وب‌سرویس‌های api.ir</summary>
        /// <param name="url">آدرس کامل و ثابت سرویس</param>
        /// <param name="body">بدنه‌ی درخواست</param>
        /// <param name="timeoutSeconds">مهلت پاسخ به ثانیه — از سرویس می‌آید؛ null یعنی مقدار سراسری</param>
        /// <param name="cancellationToken">توکن لغو اختیاری</param>
        /// <returns>در هر شرایطی (موفق، ناموفق، قطعی شبکه، مهلت تمام‌شده) یک ApiResult کامل</returns>
        public static async Task<ApiResult<T>> SendAsync<T>(
            string url,
            object body,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            // ۱) نمونه‌ی خروجی — قبل از try ساخته می‌شود و در هر شرایطی همین برگردانده می‌شود
            var result = ApiResult<T>.Fail(DefaultMessage);

            try
            {
                // ۲) ارتباط با سرور
                var timeout = TimeSpan.FromSeconds(timeoutSeconds ?? ApiIrConfig.TimeoutSeconds);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiIrConfig.Token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

                using var response = await Http.SendAsync(request, cts.Token);

                // پاسخ سرور، با هر کد HTTP، به قالب استاندارد تبدیل می‌شود
                // JSON نامعتبر یا خارج از قالب → استثنا → catch
                var raw = await response.Content.ReadAsStringAsync(cts.Token);
                var envelope = JsonSerializer.Deserialize<Envelope<T>>(raw, JsonOptions)
                    ?? throw new JsonException("پاسخ سرور قالب استاندارد ندارد.");

                var message = string.IsNullOrWhiteSpace(envelope.Message) ? null : envelope.Message;

                // کاربر هرگز نباید خطای بی‌پیام ببیند
                if (!envelope.Success && message is null)
                {
                    message = DefaultMessage;
                }

                // پر کردن result — آخرین دستور داخل try
                result = new ApiResult<T>(envelope.Success, envelope.Code, message, envelope.Data);
            }
            catch (Exception ex)
            {
                // ۳) هر خطایی (اتصال، مهلت پاسخ، لغو، JSON نامعتبر) — فقط Message پر می‌شود
                result = result with
                {
                    Message = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message
                };
            }

            // ۴) تنها نقطه‌ی بازگشت
            return result;
        }

        /// <summary>پیام پیش‌فرض خطا وقتی سرویس پیامی نداده باشد</summary>
        private const string DefaultMessage = "درخواست ناموفق بود.";

        /// <summary>ساختار پاکت پاسخ api.ir</summary>
        private sealed class Envelope<T>
        {
            public bool Success { get; set; }
            public int Code { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
