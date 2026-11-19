using ShipRate.Application.DTOs;
using ShipRate.Application.ViewModels;

namespace ShipRate.Application.Interfaces
{
    public interface IFACContractService
    {
        Task<PagedResultDto<FACContractListItemVm>> GetPagedAsync(FACContractSearchVm filter, CancellationToken cancellationToken = default);
        Task<FACContractFormVm> GetCreateModelAsync(CancellationToken cancellationToken = default);
        Task<FACContractFormVm?> GetEditModelAsync(Guid id, CancellationToken cancellationToken = default);
        Task<FACContractFormVm?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(FACContractFormVm model, CancellationToken cancellationToken = default);
        Task UpdateAsync(FACContractFormVm model, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<FACContractRateLineFormVm> GetEmptyRateLineAsync(CancellationToken cancellationToken = default);
        Task<Select2ResultDto> SearchForwardersAsync(string? term, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<JobFACSectionVm> GetJobFACSectionAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task AddFACContractToJobAsync(Guid jobId, Guid facContractId, CancellationToken cancellationToken = default);
        Task RemoveFACContractFromJobAsync(Guid jobFACContractId, CancellationToken cancellationToken = default);
        //---
        Task ChangeFACStatusAsync(Guid id, int flag, CancellationToken cancellationToken = default);
    }
}