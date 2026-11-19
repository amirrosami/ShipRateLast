using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.Interfaces;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.BillOfLading;
using ShipRate.Domain.Entities.ContainerOperation;
using ShipRate.Infrastructure.Data;

namespace ShipRate.Application.Services
{
    public class ContainerUseCycleService : IContainerUseCycleService
    {
        private readonly ApplicationDbContext _db;
        public ContainerUseCycleService(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<PagedResultDto<ContainerUseCycleRowDto>> GetCyclesAsync(ContainerUseCycleQueryDto q, int companyId)
        {
            q.PageNumber = q.PageNumber <= 0 ? 1 : q.PageNumber;
            q.PageSize = q.PageSize <= 0 ? 15 : q.PageSize;

            var query = _db.ContainerUses.AsNoTracking().Where(x => x.CompanyId == companyId);

            // Filter: Closed
            if (q.Closed.HasValue) query = query.Where(x => x.CycleClosed == q.Closed.Value);

            // Search (ContainerNumber / Seal / EIR)
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim();
                query = query.Where(x =>
                    x.ContainerNumber.Contains(s) ||
                    (x.SealNumber != null && x.SealNumber.Contains(s)) ||
                    (x.EIRNumber != null && x.EIRNumber.Contains(s))
                );
            }

            // TotalCount
            var totalCount = await query.CountAsync();

            // Sorting (whitelist)
            query = ApplySorting(query, q.SortColumn, q.SortDirection);
            query = query.OrderByDescending(x => x.CycleId);

            // Page
            var items = await query
                .Skip((q.PageNumber - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(x => new ContainerUseCycleRowDto
                {
                    Id = x.Id,
                    ContainerNumber = x.ContainerNumber,
                    SealNumber = x.SealNumber,
                    EIRNumber = x.EIRNumber,
                    CycleStartDate = x.CycleStartDate,
                    CycleClosed = x.CycleClosed,
                    CycleCloseDate = x.CycleCloseDate,
                    FullEmpty = x.FullMT != null ? x.FullMT.FullMTName : "", // اگر Title ندارید اصلاح کنید
                    SizeType = x.ContainerSizeType != null ? x.ContainerSizeType.ShortName : "",
                    RTSDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 1).FirstOrDefault().ContainerActionDateTime,
                    RBSDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 2).FirstOrDefault().ContainerActionDateTime,
                    LOBDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 3).FirstOrDefault().ContainerActionDateTime,
                    DISDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 4).FirstOrDefault().ContainerActionDateTime,
                    DTCDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 5).FirstOrDefault().ContainerActionDateTime,
                    RBCDate = _db.ContainerActions.Where(a => a.ContainerUseId == x.Id && a.CntActionTypeId == 6).FirstOrDefault().ContainerActionDateTime,
                    CycleId = x.CycleId,
                    cntOwnTypeName = x.CntOwnType.CntOwnTypeName
                })
                .ToListAsync();

            return new PagedResultDto<ContainerUseCycleRowDto>(items, totalCount, q.PageNumber, q.PageSize)
            {
                SearchTerm = q.Search,
                SortColumn = q.SortColumn,
                SortDirection = q.SortDirection
            };
        }
        private static IQueryable<ContainerUse> ApplySorting(IQueryable<ContainerUse> q, string? sortColumn, string? sortDir)
        {
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortColumn ?? "").ToLowerInvariant() switch
            {
                "containernumber" => desc ? q.OrderByDescending(x => x.ContainerNumber) : q.OrderBy(x => x.ContainerNumber),
                "cycleclosed" => desc ? q.OrderByDescending(x => x.CycleClosed) : q.OrderBy(x => x.CycleClosed),
                "cyclestartdate" => desc ? q.OrderByDescending(x => x.CycleStartDate) : q.OrderBy(x => x.CycleStartDate),
                _ => q.OrderByDescending(x => x.CycleStartDate),
            };
        }
        public async Task<List<ContainerMovementDto>> GetMovementsByCycleIdAsync(Guid cycleId, int companyId)
        {
            return await _db.ContainerActions
                .AsNoTracking()
                .Where(x => x.ContainerUseId == cycleId && x.CompanyId == companyId)
                .OrderBy(x => x.ContainerActionDateTime)
                .Select(x => new ContainerMovementDto
                {
                    Id = x.Id,
                    ActionDateTime = x.ContainerActionDateTime,
                    ActionTypeName = x.CntActionType != null ? x.CntActionType.CntActionTypeName : "", // یا Name بر اساس دیتابیس شما
                    LocationName = x.Location != null ? x.Location.LocationName : null,
                    SubLocationName = x.SubLocation != null ? x.SubLocation.LocationName : null,
                    VoyageInfo = x.Voyage != null ? x.Voyage.VoyageNo : null,
                    FullMTName = x.FullMT != null ? x.FullMT.FullMTName : null,
                    SourceReference = x.ContainerActionSourceReference,
                    BookingNumber = x.Booking != null ? x.Booking.BookingNo : null,
                    ContainerCondition = x.ContainerCondition != null ? x.ContainerCondition.ContainerConditionStatusName : null
                })
                .ToListAsync();
        }
        public async Task<List<ContainerCycleBLDto>> GetBLsByCycleIdAsync(Guid cycleId, int companyId)
        {
            return await _db.Set<BLContainer>()
                .AsNoTracking()
                .Where(bc => bc.ContainerUseId == cycleId && bc.BL.CompanyId == companyId)
                .Select(bc => new ContainerCycleBLDto
                {
                    BLId = bc.BLId,
                    BLNo = bc.BL.BLNo,
                    MasterBLNo = bc.BL.MasterBLNo,
                    BLIssueDate = bc.BL.BLIssueDate,
                    POR = bc.BL.POR != null ? bc.BL.POR.LocationName : bc.BL.PORText,
                    POL = bc.BL.POL != null ? bc.BL.POL.LocationName : bc.BL.POLText,
                    POD = bc.BL.POD != null ? bc.BL.POD.LocationName : bc.BL.PODText,
                    PODel = bc.BL.PODel != null ? bc.BL.PODel.LocationName : bc.BL.PODelText,
                    CarrierName = bc.BL.Carrier != null ? bc.BL.Carrier.Person.PersonName : null,
                    SealNumber = bc.SealNumber,
                    GrossWeightKg = bc.TotalGrossWeight ?? bc.BL.GrossWeightKg,
                    TotalPackages = bc.TotalPackageCount,
                    BLStatusName = bc.BL.BLStatus != null ? bc.BL.BLStatus.BLStatusName : null
                })
                .Distinct()
                .ToListAsync();
        }
        //-------------Card---------------

        public async Task<GraphicCycleHeaderDto?> GetContainerAction(Guid id)
        {
            var queryBlParty = _db.BLParties.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            // 1. دریافت جزئیات کارت‌ها
            var actions = await _db.ContainerActions
                .AsNoTracking()
                .Where(ca => ca.ContainerUseId == id && !ca.IsDeleted)
                .OrderBy(o => o.ContainerActionDateTime)
                .Select(ca => new CardDetailsDto
                {
                    ActionDate = ca.ContainerActionDateTime,
                    LastFullMtInCycle = ca.FullMT.FullMTName,
                    CntActionTypeName = ca.CntActionType.CntActionTypeName,
                    LocationName = ca.Location.LocationName,
                    CompanyName = ca.Company.Person.PersonName,
                    CreateDate = ca.CreatedAt,
                    EditDate = ca.UpdatedAt,
                    CreateUserName = ca.CreateUser.FullName,
                    EditUserName = ca.EditUser.FullName,
                    Refference = ca.ContainerActionSourceReference,
                    VoyageNo = ca.Voyage.VoyageNo,
                    EventActionSource = ca.EventActionSource.EventActionSourceName,
                    VesselName = ca.Voyage.Vessel.VesselName
                })
                .ToListAsync();

            if (!actions.Any())
                return null;

            // 2. دریافت مستقیم اطلاعات هدر
            var headerInfo = await _db.ContainerUses
                .AsNoTracking()
                .Where(cu => cu.Id == id)
                .Select(cu => new
                {
                    ContainerNo = cu.ContainerNumber,
                    ContainerSize = cu.ContainerSizeType != null ? cu.ContainerSizeType.ShortName : null,
                    CntOwnTypeName = cu.CntOwnType != null ? cu.CntOwnType.CntOwnTypeName : null
                })
                .FirstOrDefaultAsync();

            // 3. لیست بارنامه‌های کانتینر (اصلاح شده و با مپ مستقیم به DTO)
            // نام کلاس زیر (مثلاً ContainerBLDto) را با نام کلاس داخل پراپرتی ContainerList ست کنید
            var containerBLs = await _db.BLs
                .AsNoTracking()
                .Where(b => b.BLContainers.Any(bc => bc.ContainerUseId == id))
                .Select(b => new CardContainerListDto
                {
                    BLNo = b.BLNo,
                    MasterBLNo = b.MasterBLNo,
                    GrossWeight = b.GrossWeightKg,
                    PackageCount = b.TotalPackages,
                    NetWeight = b.NetWeightKg,
                    VolumeCBM = b.VolumeCBM,
                    ShipperName = queryBlParty.Where(bp => bp.BLId == b.Id && bp.RoleTypeId == 51).FirstOrDefault().Party.PersonName,
                    ConsigneeName = queryBlParty.Where(bp => bp.BLId == b.Id && bp.RoleTypeId == 52).FirstOrDefault().Party.PersonName,
                    NotifyName = queryBlParty.Where(bp => bp.BLId == b.Id && bp.RoleTypeId == 53).FirstOrDefault().Party.PersonName,
                    PORName = b.POR.LocationName,
                    PODelName = b.PODel.LocationName,
                    PODName = b.POD.LocationName,
                    POLName = b.POL.LocationName
                })
                .Distinct()
                .ToListAsync();

            // 4. سرهم‌بندی خروجی
            var result = new GraphicCycleHeaderDto
            {
                ContainerNo = headerInfo?.ContainerNo,
                ContainerSize = headerInfo?.ContainerSize,
                CycleCount = actions.Count,
                StartCycleDate = actions.First().ActionDate,
                LastAction = actions.Last().CntActionTypeName,
                CntOwnTypeName = headerInfo?.CntOwnTypeName,
                CardDetails = actions,
                ContainerList = containerBLs
            };

            return result;
        }



        //public async Task<GraphicCycleHeaderDto?> GetContainerAction(Guid id)
        //{
        //    // 1. دریافت جزئیات کارت‌ها
        //    var actions = await _db.ContainerActions
        //        .AsNoTracking()
        //        .Where(ca => ca.ContainerUseId == id && !ca.IsDeleted)
        //        .OrderBy(o => o.ContainerActionDateTime)
        //        .Select(ca => new CardDetailsDto
        //        {
        //            ActionDate = ca.ContainerActionDateTime,
        //            LastFullMtInCycle = ca.FullMT.FullMTName,
        //            CntActionTypeName = ca.CntActionType.CntActionTypeName,
        //            LocationName = ca.Location.LocationName,
        //            CompanyName = ca.Company.Person.PersonName,
        //            //
        //            CreateDate = ca.CreatedAt,
        //            EditDate = ca.UpdatedAt,
        //            CreateUserName = ca.CreateUser.FullName,
        //            EditUserName = ca.EditUser.FullName,
        //            Refference = ca.ContainerActionSourceReference,
        //            VoyageNo = ca.Voyage.VoyageNo,
        //            EventActionSource = ca.EventActionSource.EventActionSourceName,
        //            VesselName = ca.Voyage.Vessel.VesselName

        //        })
        //        .ToListAsync();

        //    if (!actions.Any())
        //        return null;

        //    // 2. دریافت مستقیم اطلاعات هدر با Select (برای جلوگیری از Null بودن Navigation ها)
        //    var headerInfo = await _db.ContainerUses
        //        .AsNoTracking()
        //        .Where(cu => cu.Id == id)
        //        .Select(cu => new
        //        {
        //            ContainerNo = cu.ContainerNumber, // اگر فیلد مستقیم خود ContainerUse است، وگرنه cu.Container.ContainerNumber
        //            ContainerSize = cu.ContainerSizeType != null ? cu.ContainerSizeType.ShortName : null,
        //            CntOwnTypeName = cu.CntOwnType != null ? cu.CntOwnType.CntOwnTypeName : null
        //        })
        //        .FirstOrDefaultAsync();

        //    //3.لیست بارنامه های کانتینر
        //    var containerBLs = await _db.BLs.Where(b => b.BLContainers.
        //        Any(bc => _db.ContainerActions.Any(ca => ca.ContainerUseId == bc.ContainerUseId && ca.ContainerUseId == id)))
        //        .Select(b => new
        //        {
        //            b.BLNo,
        //            b.MasterBLNo,
        //            b.GrossWeightKg,
        //            b.TotalPackages,
        //            b.NetWeightKg,
        //            b.VolumeCBM
        //        }).Distinct().ToListAsync();

        //    // 3. سرهم‌بندی خروجی
        //    var result = new GraphicCycleHeaderDto
        //    {
        //        ContainerNo = headerInfo?.ContainerNo,
        //        ContainerSize = headerInfo?.ContainerSize, // به صورت ایمن مقداردهی می‌شود
        //        CycleCount = actions.Count,
        //        StartCycleDate = actions.First().ActionDate,
        //        LastAction = actions.Last().CntActionTypeName,
        //        CntOwnTypeName = headerInfo.CntOwnTypeName,
        //        CardDetails = actions,
        //        ContainerList = containerBLs
        //    };

        //    return result;
        //}

    }
}