using ShipRate.Application.DTOs;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.ContainerOperation;

namespace ShipRate.Application.Interfaces
{
    public interface IContainerUseCycleService
    {
        Task<PagedResultDto<ContainerUseCycleRowDto>> GetCyclesAsync(ContainerUseCycleQueryDto q, int companyId);
        Task<List<ContainerMovementDto>> GetMovementsByCycleIdAsync(Guid cycleId, int companyId);
        Task<List<ContainerCycleBLDto>> GetBLsByCycleIdAsync(Guid cycleId, int companyId);
        Task<GraphicCycleHeaderDto?> GetContainerAction(Guid id);
    }
}