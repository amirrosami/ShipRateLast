using ShipRate.Application.DTOs.Persons;

namespace ShipRate.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerSelect2Dto>> SearchCustomersAsync(string term, int take = 20);
        Task<CustomerProfileDto?> GetCustomerProfileAsync(Guid customerId);
        Task<CustomerFinanceSummaryDto> GetCustomerFinanceSummaryAsync(Guid customerPartyId, CancellationToken ct = default);
        Task<CustomerJobsSummaryDto> GetCustomerJobsSummaryAsync(Guid customerPartyId, CancellationToken ct = default);
        Task<CustomerPerformanceDto> GetCustomerPerformanceTrendAsync(CustomerPerformanceFilterDto filter);
    }
}
