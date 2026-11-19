using System.Text.Json.Serialization;

namespace ShipRate.Application.Services.External
{
    public static class UnpaidChequeService
    {
        private const string Url = "https://s.api.ir/api/sw1/UnpaidCheque";
        private static int DefaultTimeoutSeconds => ApiIrConfig.TimeoutSeconds;
        public static Task<ApiResult<UnpaidChequeData>> PostAsync(
            string nationalCode,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            var body = new
            {
                nationalCode
            };

            return ApiIrClient.SendAsync<UnpaidChequeData>(
                Url, body, timeoutSeconds ?? DefaultTimeoutSeconds, cancellationToken);
        }
    }
    public sealed record UnpaidChequeData(
        [property: JsonPropertyName("count")] int? Count,
        [property: JsonPropertyName("sumAmount")] long? SumAmount,
        [property: JsonPropertyName("sumBouncedAmount")] long? SumBouncedAmount);
}
