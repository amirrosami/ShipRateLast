using System.Text.Json.Serialization;

namespace ShipRate.Application.Services.External
{
    public static class ChequeColorService
    {
        private const string Url = "https://s.api.ir/api/sw1/ChequeColor";
        private static int DefaultTimeoutSeconds => ApiIrConfig.TimeoutSeconds;
        public static Task<ApiResult<ChequeColorData>> PostAsync(
            string nationalCode,
            bool isCompany = false,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            var body = new
            {
                nationalCode,
                isCompany
            };

            return ApiIrClient.SendAsync<ChequeColorData>(
                Url, body, timeoutSeconds ?? DefaultTimeoutSeconds, cancellationToken);
        }
    }
    public sealed record ChequeColorData(
        [property: JsonPropertyName("chequeColor")] string? ChequeColor,
        [property: JsonPropertyName("chequeColorCode")] int ChequeColorCode);
}
