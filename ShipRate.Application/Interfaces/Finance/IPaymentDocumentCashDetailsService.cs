using ShipRate.Application.ViewModels;

namespace ShipRate.Application.Interfaces.Finance
{
    public interface IPaymentDocumentCashDetailsService
    {
        Task<CashDetailsModalViewModel> GetCashDetailsModalAsync(Guid paymentDocumentId, CancellationToken cancellationToken = default);
        Task<SaveCashDetailsResultDto> SaveCashDetailsAsync(SaveCashDetailsRequest request, CancellationToken cancellationToken = default);
        Task<(byte[]? ImageData, string ContentType)> GetItemImageAsync(Guid paymentDocumentItemId, CancellationToken cancellationToken = default);
        Task<byte[]?> GetSerialImageBytesAsync(Guid paymentDocumentItemId);
    }
}
