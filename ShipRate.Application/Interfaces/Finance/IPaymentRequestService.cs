using Microsoft.AspNetCore.Mvc.Rendering;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.Finance;

namespace ShipRate.Application.Interfaces.Finance
{
    public interface IPaymentRequestService
    {
        Task<PagedResultDto<PaymentRequestListDto>> GetPagedAsync(PaymentRequestFilterDto input);
        Task<PaymentRequestEditDto> GetForEditAsync(Guid? id, CancellationToken cancellationToken);
        Task<Guid> CreateAsync(PaymentRequestEditDto input, CancellationToken cancellationToken);
        Task UpdateAsync(PaymentRequestEditDto input, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);

        Task<long> GetNextRequestNoAsync(int companyId);

        Task<List<SelectListItem>> GetStatusesSelectListAsync();
        Task<List<SelectListItem>> GetCategoriesSelectListAsync();
        Task<List<SelectListItem>> GetCurrenciesSelectListAsync();
        Task<List<SelectListItem>> GetPaymentMethodsSelectListAsync();
        Task<List<SelectListItem>> GetVaultsSelectListAsync();
        Task<List<SelectListItem>> GetBranchesSelectListAsync(int companyId);
        Task<List<SelectListItem>> GetPartiesSelectListAsync();
        Task<List<SelectListItem>> GetCompaniesSelectListAsync();
        Task<List<LookupDto>> GetPartyServiceRequestsAsync(Guid partyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PaymentDocumentChoiceDto>> GetPaymentDocumentChoicesAsync(Guid partyId, Guid? serviceRequestId, CancellationToken cancellationToken = default);
        Task<List<VendorInvoiceChoiceDto>> GetVendorInvoiceChoicesAsync(Guid partyId, CancellationToken cancellationToken = default);
        Task SendRequestAsync(Guid id, CancellationToken cancellationToken = default);
        Task ConfirmRequestAsync(Guid id, CancellationToken cancellationToken = default);
        Task ApproveRequestAsync(Guid id, CancellationToken cancellationToken = default);
        Task PayRequestAsync(Guid id, CancellationToken cancellationToken = default);
        Task RejectRequestAsync(Guid id, CancellationToken cancellationToken = default);
        //--Stimul Report--
        Task<PaymentRequestDetailsViewModel?> GetPaymentRequestDetailsAsync(Guid paymentRequestId, CancellationToken cancellationToken = default);
    }
}