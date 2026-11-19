using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace ShipRate.Application.Services.SMS
{
    public class SmsAuthenticationHandler : DelegatingHandler
    {
        private readonly ISmsAuthService _authService;
        private readonly ILogger<SmsAuthenticationHandler> _logger;

        public SmsAuthenticationHandler(ISmsAuthService authService, ILogger<SmsAuthenticationHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _authService.GetAccessTokenAsync(forceRefresh: false, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                // اگر توکن به هر دلیلی سمت سرور نامعتبر اعلام شد (401)، توکن تازه دریافت کرده و درخواست را تکرار کن
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    response.Dispose();

                    token = await _authService.GetAccessTokenAsync(forceRefresh: true, cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    return await base.SendAsync(request, cancellationToken);
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "عدم برقراری ارتباط با شبکه جهت ارسال پیامک.");
                // ایجاد یک Response دستی برای جلوگیری از Throw شدن Exception
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request,
                    ReasonPhrase = "Network Connection Failed",
                    Content = new StringContent("{\"error\":\"ارتباط با سرور پیامک یا اینترنت برقرار نشد.\"}")
                };
            }
        }
    }
}
