using System.Text.Json.Serialization;

namespace ShipRate.Application.Services.External
{
    public static class TaxRecordsService
    {
        private const string Url = "https://s.api.ir/api/sw1/TaxRecords";
        private static int DefaultTimeoutSeconds => ApiIrConfig.TimeoutSeconds;
        public static Task<ApiResult<TaxRecordsData>> PostAsync(
            string inquiryCode,
            int? timeoutSeconds = null,
            CancellationToken cancellationToken = default)
        {
            var body = new
            {
                inquiryCode
            };

            return ApiIrClient.SendAsync<TaxRecordsData>(
                Url, body, timeoutSeconds ?? DefaultTimeoutSeconds, cancellationToken);
        }
    }
    public sealed record TaxRecordsData(
        [property: JsonPropertyName("records")] IReadOnlyList<TaxRecordsItem>? Records);

    public sealed record TaxRecordsItem(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("economicCode")] string? EconomicCode,
        [property: JsonPropertyName("registrationStatus")] string? RegistrationStatus,
        [property: JsonPropertyName("registrationStep")] string? RegistrationStep);
}
