using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.BillOfLadings;
using ShipRate.Application.Utilities;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.BaseInfo.BLSpec;
using ShipRate.Domain.Entities.BillOfLading;
using ShipRate.Domain.Entities.DetentionCalculation;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Domain.Entities.Operations;
using ShipRate.Infrastructure.Data;
using System.Data;

namespace ShipRate.Application.Interfaces.BillOfLadings
{
    public interface IBLManagementService
    {
        Task<BLManagementPageViewModel> GetManagementPageDataAsync(BLManagementSearchRequest request);
        Task<bool> DeleteBLAsync(Guid blId, Guid userId);
        Task<MasterConsoleBLSummaryVm?> GetBLSummaryAsync(Guid blId, CancellationToken cancellationToken = default);
        Task<MasterConsoleVm> GetMasterConsoleAsync(Guid id);
        Task<List<InvoiceRowVm>> GetInvoicesByBlIdAsync(Guid blId, string filterType);
        Task<MasterConsoleDetentionsVm?> GetDetentionsByBlAsync(Guid blId, CancellationToken cancellationToken);
        Task<List<DoPricePreviewItemDto>> GetDoPricePreviewAsync(Guid blId, int clearanceTypeId, decimal taxRate);
        Task<BLManagementPageViewModel_Report> GetManagementPageData_ReportAsync(BLManagementSearchRequest_Report request);
        Task<BLManagementPageViewModel_ContainerReport> GetManagementPageData_ContainerReportAsync(BLManagementSearchRequest_ContainerReport request);
        Task<BLsReportCompleteDto> GetTotalStimulBLsReport(Guid id);

        Task<DOProcessModalVm> GetDOProcessModalAsync(Guid blId, Guid userId, CancellationToken cancellationToken = default);
        Task SaveDOProcessDraftAsync(DOProcessDraftInputVm input, Guid userId, CancellationToken cancellationToken = default);
        Task ApproveDOProcessStageAsync(DOApprovalInputVm input, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> CanApproveAsync(Guid userId, DOApprovalStage stage, CancellationToken cancellationToken = default);
        Task<Guid> GetBLIdByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default);
        Task ApproveDOSettlementExceptionAsync(Guid processId, string reason, Guid userId, CancellationToken cancellationToken = default);

    }
    public class BLManagementService : IBLManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _authorizationService;

        public BLManagementService(ApplicationDbContext context, IPermissionService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<BLManagementPageViewModel> GetManagementPageDataAsync(BLManagementSearchRequest request)
        {
            request ??= new BLManagementSearchRequest();
            var query = _context.BLs
                .Include(x => x.BLStatus).Include(x => x.BLType).Include(x => x.Company).Include(x => x.Job).AsNoTracking().AsQueryable();

            // فیلتر عدم نمایش موارد کاملاً حذف شده فیزیکی (در صورت وجود منطق سافت دلیت)
            query = query.Where(x => x.BLDeletedAt == null);

            // ۱. فیلتر جستجوی عمومی (Global Search)
            if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
            {
                var term = request.GlobalSearch.Trim();
                query = query.Where(x =>
                    x.BLNo.Contains(term) ||
                    x.Job.JobNumber.Contains(term) ||
                    x.Job.JobFileNumber.Contains(term) ||
                    x.PORText.Contains(term) ||
                    x.POLText.Contains(term) ||
                    x.PODText.Contains(term) ||
                    x.PODelText.Contains(term) ||
                    x.BLParties.Any(p => p.PartyTextInBL.Contains(term)) ||
                    x.BLContainers.Any(c => c.ContainerNumber.Contains(term)) ||
                    x.BLFlights.Any(f => f.FlightNo != null && f.FlightNo.Contains(term)) ||
                    x.BLTrips.Any(t => t.TruckNo != null && t.TruckNo.Contains(term)) ||
                    x.BLRailJourneys.Any(r => r.TrainNo != null && r.TrainNo.Contains(term) || r.WagonNo != null && r.WagonNo.Contains(term))
                );
            }

            // ۲. اعمال فیلترهای اختصاصی
            if (request.TransportTypeId.HasValue)
                query = query.Where(x => x.TransportTypeId == request.TransportTypeId.Value);

            if (request.BLTypeId.HasValue)
                query = query.Where(x => x.BLTypeId == request.BLTypeId.Value);

            if (request.BLStatusId.HasValue)
                query = query.Where(x => x.BLStatusId == request.BLStatusId.Value);

            if (!string.IsNullOrWhiteSpace(request.BLNo))
                query = query.Where(x => x.BLNo.Contains(request.BLNo.Trim()));

            if (!string.IsNullOrWhiteSpace(request.JobNo))
                query = query.Where(x => x.Job.JobNumber.Contains(request.JobNo.Trim()));

            //if (!string.IsNullOrWhiteSpace(request.CustomerName))
            //    query = query.Where(x => x.BLParties.Any(p => p.RoleTypeId == BLPartyRole.Customer && p.PartyTextInBL.Contains(request.CustomerName.Trim())));

            if (!string.IsNullOrWhiteSpace(request.ShipperName))
                query = query.Where(x => x.BLParties.Any(p => p.RoleTypeId == (int)BLPartyRole.Shipper && p.PartyTextInBL.Contains(request.ShipperName.Trim())));

            if (!string.IsNullOrWhiteSpace(request.ConsigneeName))
                query = query.Where(x => x.BLParties.Any(p => p.RoleTypeId == (int)BLPartyRole.Consignee && p.PartyTextInBL.Contains(request.ConsigneeName.Trim())));

            if (!string.IsNullOrWhiteSpace(request.Origin))
                query = query.Where(x => x.PORText.Contains(request.Origin.Trim()) || x.POLText.Contains(request.Origin.Trim()));

            if (!string.IsNullOrWhiteSpace(request.Destination))
                query = query.Where(x => x.PODText.Contains(request.Destination.Trim()) || x.PODelText.Contains(request.Destination.Trim()));

            if (request.IssueDateFrom.HasValue)
                query = query.Where(x => x.BLIssueDate >= request.IssueDateFrom.Value);

            if (request.IssueDateTo.HasValue)
                query = query.Where(x => x.BLIssueDate <= request.IssueDateTo.Value);

            if (!string.IsNullOrWhiteSpace(request.ContainerNo))
                query = query.Where(x => x.BLContainers.Any(c => c.ContainerNumber.Contains(request.ContainerNo.Trim())));

            if (!string.IsNullOrWhiteSpace(request.VesselVoyage))
                query = query.Where(x => x.BLVoyages.Any(v => v.VesselName.Contains(request.VesselVoyage.Trim()) || v.VoyageNo.Contains(request.VesselVoyage.Trim())));

            if (!string.IsNullOrWhiteSpace(request.FlightNo))
                query = query.Where(x => x.BLFlights.Any(f => f.FlightNo != null && f.FlightNo.Contains(request.FlightNo.Trim())));

            if (!string.IsNullOrWhiteSpace(request.TruckNo))
                query = query.Where(x => x.BLTrips.Any(t => t.TruckNo != null && t.TruckNo.Contains(request.TruckNo.Trim())));

            if (!string.IsNullOrWhiteSpace(request.TrainNo))
                query = query.Where(x => x.BLRailJourneys.Any(r => r.TrainNo != null && r.TrainNo.Contains(request.TrainNo.Trim())));

            if (!string.IsNullOrWhiteSpace(request.WagonNo))
                query = query.Where(x => x.BLRailJourneys.Any(r => r.WagonNo != null && r.WagonNo.Contains(request.WagonNo.Trim())));

            // ۳. محاسبه مقادیر آماری (Summary Card) پیش از اعمال Paging
            var summary = new BLManagementSummaryDto
            {
                TotalBills = await query.CountAsync(),
                InProgressCount = await query.CountAsync(x => x.BLStatusId == 2), // فرض: ۲ وضعیت در جریان است
                IncompleteCount = await query.CountAsync(x => string.IsNullOrEmpty(x.BLNo) || x.BLIssueDate == null),
                MissingCarrierReferenceCount = await query.CountAsync(x => x.CarrierId == null)
            };

            // ۴. پیاده‌سازی Paging
            var totalCount = summary.TotalBills;
            var skip = (request.Page - 1) * request.PageSize;

            var rawItems = await query
                .OrderByDescending(x => x.CreatedAt) // مرتب‌سازی بر اساس زمان ایجاد
                .Skip(skip)
                .Take(request.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.BLNo,
                    JobNo = x.Job != null ? x.Job.JobNumber : string.Empty,
                    x.TransportTypeId,
                    BLTypeTitle = x.BLType != null ? x.BLType.BLTypeName : string.Empty,
                    DocumentStatusTitle = x.BLStatus != null ? x.BLStatus.BLStatusName : string.Empty,
                    ShipperName = x.BLParties.Where(p => p.RoleTypeId == (int)BLPartyRole.Shipper).Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    ConsigneeName = x.BLParties.Where(p => p.RoleTypeId == (int)BLPartyRole.Consignee).Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    CarrierName = x.Carrier != null ? x.Carrier.Person.PersonName : string.Empty,
                    x.PORText,
                    x.POLText,
                    x.PODText,
                    x.PODelText,
                    x.BLIssueDate,
                    x.BLDeletedAt,
                    x.BLReleaseTypeId,
                    x.JobId,
                    x.Job.JobFileNumber,
                    x.TradeType.TradeTypeName,
                    x.TradeTypeId,
                    x.GrossWeightKg,
                    x.BLType.BLTypeCode,
                    SalesUserName = x.Job.SalesPerson.UserName,
                    // جزئیات ترانسپورت جهت فرمت‌دهی
                    FirstContainer = x.BLContainers.Select(c => c.ContainerNumber).FirstOrDefault(),
                    FirstVoyage = x.BLVoyages.Select(v => new { v.VesselName, v.VoyageNo }).FirstOrDefault(),
                    FirstFlight = x.BLFlights.Select(f => f.FlightNo).FirstOrDefault(),
                    FirstTruck = x.BLTrips.Select(t => t.TruckNo).FirstOrDefault(),
                    FirstTrain = x.BLRailJourneys.Select(r => new { r.TrainNo, r.WagonNo }).FirstOrDefault(),

                    // فلگ‌های منطقی مربوط به MultiModal
                    HasSea = x.BLVoyages.Any(),
                    HasRoad = x.BLTrips.Any(),
                    HasRail = x.BLRailJourneys.Any(),
                    HasAir = x.BLFlights.Any()
                })
                .ToListAsync();

            var rows = rawItems.Select(x => new BLManagementRowDto
            {
                BLId = x.Id,
                BLNo = x.BLNo,
                JobNo = x.JobNo,
                TransportTypeId = x.TransportTypeId,
                TransportTypeTitle = ((TransportTypeEnum)x.TransportTypeId).ToString(),
                BLTypeTitle = x.BLTypeCode,
                DocumentStatusTitle = x.DocumentStatusTitle,
                ShipperName = x.ShipperName,
                ConsigneeName = x.ConsigneeName,
                CarrierName = x.CarrierName,
                RouteText = BuildRouteText(x.PORText, x.POLText, x.PODText, x.PODelText),
                IssueDate = x.BLIssueDate,
                BLDeletedAt = x.BLDeletedAt,
                JobId = x.JobId,
                JobFileNumber = x.JobFileNumber,
                TradeTypeId = x.TradeTypeId,
                TradeTypeName = x.TradeTypeName,
                GrossWeightKg = x.GrossWeightKg != null ? x.GrossWeightKg : null,
                SalesUserName = x.SalesUserName,
                // مقداردهی هوشمند ستون‌های Mode Detail
                ModeDetailTitle = FormatModeDetailTitle(x.TransportTypeId, x.FirstContainer, x.FirstTruck, x.FirstTrain?.TrainNo, x.FirstFlight, x.HasSea, x.HasRoad, x.HasRail, x.HasAir),
                ModeDetailSubTitle = FormatModeDetailSubTitle(x.TransportTypeId, x.FirstVoyage?.VesselName, x.FirstVoyage?.VoyageNo, x.FirstTruck, x.FirstTrain?.WagonNo, x.FirstFlight, x.HasSea, x.HasRoad, x.HasRail, x.HasAir),
                // قوانین اکشن‌ها
                CanEdit = !x.BLDeletedAt.HasValue && x.BLReleaseTypeId == null,
                CanPrint = true,
                CanRelease = !x.BLDeletedAt.HasValue && x.BLReleaseTypeId == null,
                CanCancel = !x.BLDeletedAt.HasValue,
                CanDelete = !x.BLDeletedAt.HasValue
            }).ToList();

            return new BLManagementPageViewModel
            {
                Search = request,
                Summary = summary,
                PagedResult = new PagedResultDto<BLManagementRowDto>
                {
                    Items = rows,
                    TotalCount = totalCount,
                    PageNumber = request.Page,
                    PageSize = request.PageSize
                }
            };
        }
        public async Task<bool> DeleteBLAsync(Guid blId, Guid userId)
        {
            var bl = await _context.BLs.FindAsync(blId);
            if (bl == null) return false;

            bl.BLDeletedAt = DateTime.Now;
            bl.BLDeletedByUserId = userId;
            await _context.SaveChangesAsync();
            return true;
        }
        private static string BuildRouteText(string por, string pol, string pod, string podel)
        {
            var segments = new[] { por, pol, pod, podel }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(" → ", segments);
        }
        private static string FormatModeDetailTitle(int typeId, string container, string truck, string train, string flight, bool hasSea, bool hasRoad, bool hasRail, bool hasAir)
        {
            return typeId switch
            {
                //1 => !string.IsNullOrEmpty(container) ? $"CNTR: {container}" : "Ocean Freight",
                1 => "Ocean Freight",
                2 => !string.IsNullOrEmpty(truck) ? $"Truck: {truck}" : "Road Transport",
                3 => !string.IsNullOrEmpty(train) ? $"Train: {train}" : "Rail Freight",
                4 => !string.IsNullOrEmpty(flight) ? $"Flight: {flight}" : "Air Freight",
                5 => string.Join(" + ", new List<string> { hasSea ? "Sea" : "", hasRoad ? "Road" : "", hasRail ? "Rail" : "", hasAir ? "Air" : "" }.Where(s => !string.IsNullOrEmpty(s))),
                _ => "Unknown"
            };
        }
        private static string FormatModeDetailSubTitle(int typeId, string vessel, string voyage, string truck, string wagon, string flight, bool hasSea, bool hasRoad, bool hasRail, bool hasAir)
        {
            return typeId switch
            {
                1 => !string.IsNullOrEmpty(vessel) ? $"{vessel} / {voyage}" : "-",
                2 => "Interstate Delivery",
                3 => !string.IsNullOrEmpty(wagon) ? $"Wagon: {wagon}" : "-",
                4 => "Direct Air Service",
                5 => $"Combined Leg Operations",
                _ => "-"
            };
        }
        public async Task<MasterConsoleBLSummaryVm?> GetBLSummaryAsync(Guid blId, CancellationToken cancellationToken = default)
        {
            var vm = await _context.BLs
                .Include(x => x.BLContainers)
                .AsNoTracking()
                .Where(x => x.Id == blId && !x.IsDeleted)
                .Select(x => new MasterConsoleBLSummaryVm
                {
                    BLId = x.Id,
                    BLNo = x.BLNo,
                    JobId = x.JobId,
                    JobFileNo = x.Job.JobFileNumber,
                    SalesUserName = x.Job.SalesPerson != null ? x.Job.SalesPerson.UserName : "",
                    //ReferenceNo = x.ReferenceNo,
                    //BookingNo = x.BookingNo,
                    CarrierName = x.Carrier != null ? x.Carrier.Person.PersonName : string.Empty,
                    //VesselName = x.VesselName,
                    //VoyageNumber = x.VoyageNumber,
                    POL = x.POL != null ? x.POL.LocationName : string.Empty,
                    POD = x.POD != null ? x.POD.LocationName : string.Empty,
                    IssueDate = x.BLIssueDate,
                    GrossWeightKg = x.GrossWeightKg,
                    PackageCount = x.TotalPackages,
                    ContainerCount = x.BLContainers.Count(),
                    Containers = x.BLContainers
                                .OrderBy(x => x.ContainerNumber)
                                .Select(x => new MasterConsoleContainerVm
                                {
                                    ContainerNo = x.ContainerNumber,
                                    ContainerType = x.ContainerSizeType != null ? x.ContainerSizeType.ShortName : null,
                                    SealNo = x.SealNumber,
                                    GrossWeightKg = x.TotalGrossWeight,
                                    PackageCount = x.TotalPackageCount,
                                    ContainerUseId = x.ContainerUseId
                                })
                                .ToList(),
                    ConsigneeText = x.BLParties.Where(x => x.RoleTypeId == 52).Any() ? x.BLParties.Where(x => x.RoleTypeId == 52).FirstOrDefault().PartyTextInBL : "",
                    NotifyText = x.BLParties.Where(x => x.RoleTypeId == 53).Any() ? x.BLParties.Where(x => x.RoleTypeId == 53).FirstOrDefault().PartyTextInBL : "",
                    BookerClientName = x.Job.Parties.Where(x => x.RoleTypeId == 31).Any() ? x.Job.Parties.Where(x => x.RoleTypeId == 31).FirstOrDefault().Party.PersonName : "",
                    Cargos = x.CargoDetails
                            .Select(x => new MasterConsoleCargoVm
                            {
                                PackageTypeName = x.PackageType != null ? x.PackageType.PackageTypeName : null,
                                PackageCount = x.PackageCount,
                                GrossWeight = x.GrossWeight,
                                NetWeight = x.NetWeight,
                                Measurement = x.WeightUnit.MeasurementUnitName,
                                MarksAndNumbers = x.MarksAndNumbers,
                                DescriptionOfGoods = x.CargoDescription,
                                HSCode = x.HSCodeToSave,
                                Volume = x.Volume
                            }).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (vm.Containers != null && vm.Containers.Any())
            {
                foreach (var item in vm.Containers)
                {
                    var allContainerActions = await _context.ContainerActions.Where(x => x.ContainerUseId == item.ContainerUseId).ToListAsync();
                    if (allContainerActions != null && allContainerActions.Any())
                    {

                        item.RTSDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 1)
                            .OrderByDescending(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();

                        item.RBSDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 2)
                            .OrderByDescending(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();


                        item.FirstLOBDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 3)
                            .OrderBy(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();

                        item.LastDischargeDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 4)
                            .OrderByDescending(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();

                        item.DTCDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 5)
                            .OrderByDescending(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();

                        item.RBCDate = allContainerActions
                            .Where(a => a.CntActionTypeId == 6)
                            .OrderByDescending(a => a.ContainerActionDateTime)
                            .Select(a => (DateTime?)a.ContainerActionDateTime)
                            .FirstOrDefault();
                    }
                }
            }
            return vm;
        }
        public async Task<MasterConsoleVm> GetMasterConsoleAsync(Guid id)
        {
            var vm = new MasterConsoleVm();
            vm.BLId = id;
            vm.Summary = await GetBLSummaryAsync(id, default);
            vm.JobId = vm.Summary.JobId;
            return vm;
        }
        public async Task<List<InvoiceRowVm>> GetInvoicesByBlIdAsync(Guid blId, string filterType)
        {
            var query = _context.Invoices
                .AsNoTracking()
                .Where(i => i.BLId == blId && i.InvoiceStatusId != 8);
            switch (filterType)
            {
                case "BuyF":
                    query = query.Where(x => x.FinanceDirectionId == 2 && x.InvoiceFNFId == 1 && x.InvoiceModelId == 1);
                    break;
                case "BuyNF":
                    query = query.Where(x => x.FinanceDirectionId == 2 && x.InvoiceFNFId == 2 && x.InvoiceModelId == 1);
                    break;
                case "SellF":
                    query = query.Where(x => x.FinanceDirectionId == 1 && x.InvoiceFNFId == 1 && x.InvoiceModelId == 1);
                    break;
                case "SellNF":
                    query = query.Where(x => x.FinanceDirectionId == 1 && x.InvoiceFNFId == 2 && x.InvoiceModelId == 1);
                    break;
                case "PreInvoices":
                    query = query.Where(x => x.InvoiceModelId == 2);
                    break;
                default:
                    break;
            }

            return await query
                .OrderBy(x => x.InvoiceModelId).ThenByDescending(x => x.FinanceDirectionId).ThenByDescending(x => x.InvoiceFNFId)
                .Select(i => new InvoiceRowVm
                {
                    InvoiceId = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.InvoiceDate.Value,
                    PartyName = i.Party != null ? (i.Party.PersonName) : "Unknown",
                    TypeName = i.InvoiceType.InvoiceTypeName,
                    TotalAmount = i.TotalAmount,
                    CurrencyCode = i.Currency.CurrencyCode,
                    StatusName = i.InvoiceStatus.InvoiceStatusName,
                    StatusId = i.InvoiceStatusId,
                    IsPosted = i.IsPostedToAcc,
                    FNFName = i.InvoiceFNF.FNFName,
                    InvocieModelName = i.InvoiceModel.InvoiceModelName,
                    CompanyId = i.CompanyId,
                    CompanyName = i.Company.CompanyAbb,
                    DirectionName = i.FinanceDirection.InvoiceContextName,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    CreateUserName = i.CreateUser.UserName,
                    UpdateUserName = i.UpdateUser.UserName,
                    SettledAmount = i.SettledAmount,
                    RemainingAmount = i.RemainingAmount,
                    SubjectName = i.InvoiceSubject.InvoiceSubjectName,
                    Serial = i.Serial,
                    SerialPrefix = i.SerialPrefix,
                    DirectionId = i.FinanceDirectionId,
                    InvoiceModelId = i.InvoiceModelId
                })
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }
        public async Task<MasterConsoleDetentionsVm?> GetDetentionsByBlAsync(Guid blId, CancellationToken cancellationToken)
        {
            var items = await _context.Detentions
                .AsNoTracking()
                .Include(x => x.Company)
                .Include(x => x.MainCurrency)
                .Include(x => x.PaymentCurrency)
                .Include(x => x.Company)
                .Include(x => x.FinanceDirection)
                .Include(x => x.Party)
                .Include(x => x.DetentionStatus)
                .Include(x => x.CreateUser)
                .Include(x => x.EditUser)
                .Where(x => x.BLId == blId)
                .OrderByDescending(x => x.DetentionToDate)
                .Select(x => new DetentionRowVm
                {
                    Id = x.Id,
                    DetentionNo = x.DetentionNo,
                    DirectionName = x.FinanceDirection.InvoiceContextName,
                    DirectionId = x.FinanceDirectionId,
                    CompanyName = x.Company.CompanyAbb,
                    IssueDate = x.CreatedAt,
                    StatusId = x.DetentionStatusId,
                    PartyName = x.Party != null ? x.Party.PersonName : null,
                    StatusTitle = x.DetentionStatus != null ? x.DetentionStatus.DetentionStatusName : null,
                    GrossAmount = x.GrossAmount,
                    VATAmount = x.VATAmount,
                    Amount = x.Amount,
                    DetentionToDate = x.DetentionToDate,
                    IsFinal = x.IsFinal,
                    Remarks = x.Remarks,
                    TariffTitle = x.TariffBook.Name,
                    FreeDays = x.Items.Max(x => x.DetentionItemFreeDays),
                    CreateUserName = x.CreateUser.FullName,
                    IssueDatePersian = PersianDateHelper.ToShamsi(x.CreatedAt),
                    FinalPayableAmount = x.FinalPayableAmount,
                    FinalCurrencyName = x.PaymentCurrency.CurrencyCode,
                    MainCurrencyName = x.MainCurrency.CurrencyCode,
                    Settled = x.SettledAmount,
                    Remain = x.RemainingAmount
                })
                .ToListAsync(cancellationToken);
            return new MasterConsoleDetentionsVm
            {
                BLId = blId,
                BLNo = "-",
                Items = items
            };
        }
        public async Task<List<DoPricePreviewItemDto>> GetDoPricePreviewAsync(Guid blId, int clearanceTypeId, decimal taxRate)
        {
            var sql = @"
                        SELECT 
                            Seq, ChargeTypeId, Description, Qty, UnitPrice, 
                            AmountBeforeDiscount, Discount, AmountAfterDiscount, 
                            TaxRatePercent, TaxAmount, TotalAmount
                        FROM [dbo].[fnGetIssueDOInvoiceItems](@blId, @clearanceTypeId, @taxRate)
                        ORDER BY Seq";
            using var connection = new SqlConnection(_context.Database.GetConnectionString());
            var items = await connection.QueryAsync<DoPricePreviewItemDto>(sql, new { blId, clearanceTypeId, taxRate });
            return items.ToList();
        }

        //-----------------BLs Report-----------------------
        public async Task<BLManagementPageViewModel_Report> GetManagementPageData_ReportAsync(BLManagementSearchRequest_Report request)
        {
            request ??= new BLManagementSearchRequest_Report();
            var query = _context.BLs.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            var queryBlParty = _context.BLParties.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            var queryBlContainer = _context.BLContainers.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();


            //Search System
            if (!string.IsNullOrWhiteSpace(request.BLNo)) query = query.Where(x => x.BLNo.Contains(request.BLNo));
            if (!string.IsNullOrWhiteSpace(request.MasterBLNo)) query = query.Where(x => x.MasterBLNo.Contains(request.MasterBLNo));
            if (request.TradeTypeId.HasValue) query = query.Where(x => x.TradeTypeId == request.TradeTypeId.Value);
            if (request.TotalContainerFrom > 0) query = query.Where(x => x.TotalContainers >= request.TotalContainerFrom.Value);
            if (request.TotalContainerTo > 0) query = query.Where(x => x.TotalContainers <= request.TotalContainerTo.Value);
            if (request.TransportTypeId.HasValue) query = query.Where(x => x.TransportTypeId == request.TransportTypeId.Value);
            if (request.SheeperId.HasValue) query = query.Where(x => x.BLParties.Any(p => p.PartyId == request.SheeperId.Value && p.RoleTypeId == 51));
            if (!string.IsNullOrWhiteSpace(request.SheeperText)) query = query.Where(x => x.BLParties.Any(p => p.PartyTextInBL.Contains(request.SheeperText) && p.RoleTypeId == 51));
            if (request.ConsigneeId.HasValue) query = query.Where(x => x.BLParties.Any(p => p.PartyId == request.ConsigneeId.Value && p.RoleTypeId == 52));
            if (!string.IsNullOrWhiteSpace(request.ConsigneeText)) query = query.Where(x => x.BLParties.Any(p => p.PartyTextInBL.Contains(request.ConsigneeText) && p.RoleTypeId == 52));
            if (request.NotifyId.HasValue) query = query.Where(x => x.BLParties.Any(p => p.PartyId == request.NotifyId.Value && p.RoleTypeId == 53));
            if (!string.IsNullOrWhiteSpace(request.NotifyText)) query = query.Where(x => x.BLParties.Any(p => p.PartyTextInBL.Contains(request.NotifyText) && p.RoleTypeId == 53));
            if (request.CarrierId.HasValue) query = query.Where(x => x.CarrierId == request.CarrierId.Value);
            if (request.CompanyId.HasValue) query = query.Where(x => x.CompanyId == request.CompanyId.Value);
            if (!string.IsNullOrWhiteSpace(request.BookingNo)) query = query.Where(x => x.Booking.BookingNo.Contains(request.BookingNo));
            if (!string.IsNullOrWhiteSpace(request.JobNo)) query = query.Where(x => x.Job.JobNumber.Contains(request.JobNo));
            if (!string.IsNullOrWhiteSpace(request.JobFileNumber)) query = query.Where(x => x.Job.JobFileNumber.Contains(request.JobFileNumber));
            if (!string.IsNullOrWhiteSpace(request.SalesUserName)) query = query.Where(x => x.Job.SalesPerson.FullName.Contains(request.SalesUserName));
            if (request.BLTypeId.HasValue) query = query.Where(x => x.BLTypeId == request.BLTypeId.Value);
            if (request.BLLevelId.HasValue) query = query.Where(x => x.BLLevelId == request.BLLevelId.Value);
            if (request.BLIssueDateFrom.HasValue) query = query.Where(x => x.BLIssueDate >= request.BLIssueDateFrom.Value);
            if (request.BLIssueDateTo.HasValue) query = query.Where(x => x.BLIssueDate <= request.BLIssueDateTo.Value);
            if (!string.IsNullOrWhiteSpace(request.PlaceOfIssue)) query = query.Where(x => x.PlaceOfIssue.Contains(request.PlaceOfIssue));
            if (request.IssuerPartyId.HasValue) query = query.Where(x => x.IssuerPartyId == request.IssuerPartyId.Value);
            if (request.IssuerByUs.HasValue)
            {
                bool adv = request.IssuerByUs.Value == 1;
                query = query.Where(x => x.IssuedByUs == adv);
            }
            if (request.BLReleasedTypeId.HasValue) query = query.Where(x => x.BLReleaseTypeId == request.BLReleasedTypeId.Value);
            if (request.ReleaseDateFrom.HasValue) query = query.Where(x => x.ReleaseDate >= request.ReleaseDateFrom.Value);
            if (request.ReleaseDateTo.HasValue) query = query.Where(x => x.ReleaseDate <= request.ReleaseDateTo.Value);
            if (request.LoadTypeId.HasValue) query = query.Where(x => x.LoadTypeId == request.LoadTypeId.Value);
            if (request.MovementTypeId.HasValue) query = query.Where(x => x.MovementTypeId == request.MovementTypeId.Value);
            if (!string.IsNullOrWhiteSpace(request.ExportReference)) query = query.Where(x => x.ExportReference.Contains(request.ExportReference));
            if (!string.IsNullOrWhiteSpace(request.SerialNo)) query = query.Where(x => x.SerialNo.Contains(request.SerialNo));
            if (request.IsSwitched.HasValue)
            {
                bool adv = request.IsSwitched.Value == 1;
                query = query.Where(x => x.IsSwitched == adv);
            }
            if (request.PORId.HasValue) query = query.Where(x => x.PORId == request.PORId.Value);
            if (!string.IsNullOrWhiteSpace(request.PORText)) query = query.Where(x => x.PORText.Contains(request.PORText));
            if (request.POLId.HasValue) query = query.Where(x => x.POLId == request.POLId.Value);
            if (!string.IsNullOrWhiteSpace(request.POLText)) query = query.Where(x => x.POLText.Contains(request.POLText));
            if (request.PODId.HasValue) query = query.Where(x => x.PODId == request.PODId.Value);
            if (!string.IsNullOrWhiteSpace(request.PODText)) query = query.Where(x => x.PODText.Contains(request.PODText));
            if (request.PODelId.HasValue) query = query.Where(x => x.PODelId == request.PODelId.Value);
            if (!string.IsNullOrWhiteSpace(request.PODelText)) query = query.Where(x => x.PODelText.Contains(request.PODelText));
            if (!string.IsNullOrWhiteSpace(request.FinalDestinationText)) query = query.Where(x => x.FinalDestinationText.Contains(request.FinalDestinationText));
            if (!string.IsNullOrWhiteSpace(request.GoodsDescription)) query = query.Where(x => x.GoodsDescription.Contains(request.GoodsDescription));
            if (!string.IsNullOrWhiteSpace(request.MarksAndNumbers)) query = query.Where(x => x.MarksAndNumbers.Contains(request.MarksAndNumbers));
            if (request.FreightPaymentAtId.HasValue) query = query.Where(x => x.FreightPaymentAtId == request.FreightPaymentAtId.Value);
            if (!string.IsNullOrWhiteSpace(request.FreightPaymentAtText)) query = query.Where(x => x.FreightPaymentAtText.Contains(request.FreightPaymentAtText));
            if (!string.IsNullOrWhiteSpace(request.RemarksInBL)) query = query.Where(x => x.RemarksInBL.Contains(request.RemarksInBL));
            if (!string.IsNullOrWhiteSpace(request.RemarksInManifest)) query = query.Where(x => x.RemarksInManifest.Contains(request.RemarksInManifest));
            if (request.PackageTypeId.HasValue) query = query.Where(x => x.PackageTypeId == request.PackageTypeId.Value);
            if (request.BLStatusId.HasValue) query = query.Where(x => x.BLStatusId == request.BLStatusId.Value);
            if (request.SalesPersonId.HasValue) query = query.Where(x => x.SalesPersonId == request.SalesPersonId.Value);
            if (request.PaymentTypeId.HasValue) query = query.Where(x => x.PaymentTypeId == request.PaymentTypeId.Value);
            if (request.TranshipmentPortId.HasValue) query = query.Where(x => x.TranshipmentPortId == request.TranshipmentPortId.Value);
            if (request.CreatedAtFrom.HasValue) query = query.Where(x => x.CreatedAt <= request.CreatedAtFrom.Value);
            if (request.CreatedAtTo.HasValue) query = query.Where(x => x.CreatedAt >= request.CreatedAtTo.Value);
            if (!string.IsNullOrWhiteSpace(request.CreateUserName)) query = query.Where(x => x.CreateUser.FullName.Contains(request.CreateUserName));
            if (request.UpdatedAtFrom.HasValue) query = query.Where(x => x.UpdatedAt <= request.UpdatedAtFrom.Value);
            if (request.UpdatedAtTo.HasValue) query = query.Where(x => x.UpdatedAt >= request.UpdatedAtTo.Value);
            if (!string.IsNullOrWhiteSpace(request.EditUserName)) query = query.Where(x => x.EditUser.FullName.Contains(request.EditUserName));
            if (request.IncotermId.HasValue) query = query.Where(x => x.IncotermId == request.IncotermId.Value);
            if (request.MasterBLReleaseTypeId.HasValue) query = query.Where(x => x.MasterBLReleaseTypeId == request.MasterBLReleaseTypeId.Value);
            if (request.MasterPaymentTypeId.HasValue) query = query.Where(x => x.MasterPaymentTypeId == request.MasterPaymentTypeId.Value);
            if (!string.IsNullOrWhiteSpace(request.MasterPlaceOfIssue)) query = query.Where(x => x.MasterPlaceOfIssue.Contains(request.MasterPlaceOfIssue));
            if (!string.IsNullOrWhiteSpace(request.ContainerNo)) query = query.Where(x => x.BLContainers.Any(c => c.ContainerNumber.Contains(request.ContainerNo) && !c.IsDeleted));


            // ۳. محاسبه مقادیر آماری (Summary Card) پیش از اعمال Paging
            var summary = new BLManagementSummaryDto_Report
            {
                TotalBills = await query.CountAsync(),
                InProgressCount = await query.CountAsync(x => x.BLStatusId == 2), // فرض: ۲ وضعیت در جریان است
                IncompleteCount = await query.CountAsync(x => string.IsNullOrEmpty(x.BLNo) || x.BLIssueDate == null),
                MissingCarrierReferenceCount = await query.CountAsync(x => x.CarrierId == null)
            };

            // ۴. پیاده‌سازی Paging
            var totalCount = summary.TotalBills;
            var skip = (request.Page - 1) * request.PageSize;

            var Items = await query.OrderByDescending(x => x.CreatedAt).Skip(skip).Take(request.PageSize).Select(x => new BLManagementRowDto_Report
            {
                BLId = x.Id,
                BLNo = x.BLNo,
                MasterBLNo = x.MasterBLNo,
                CompanyName = x.Company.Person.PersonName,
                TradeTypeName = x.TradeType.TradeTypeName,
                BookingNo = x.Booking.BookingNo,
                JobFileNumber = x.Job.JobFileNumber,
                //ContainerSize
                F20 = queryBlContainer.Where(bc => bc.BLId == x.Id && bc.ContainerSizeType.ContainerSizeId == 20).Count(),
                F40 = queryBlContainer.Where(bc => bc.BLId == x.Id && bc.ContainerSizeType.ContainerSizeId == 40).Count(),
                //
                JobNo = x.Job.JobNumber,
                SalesUserName = x.Job.SalesPerson.FullName,
                BLTypeTitle = x.BLType.BLTypeName,
                BLLevelTitle = x.BLLevel.BLLevelName,
                BLIssueDate = x.BLIssueDate,
                PlaceOfIssue = x.PlaceOfIssue,
                CarrierName = x.Carrier.Person.PersonName,
                IssuerPartyName = x.IssuerParty.PersonName,
                IssuerByUs = x.IssuedByUs,
                TransportTypeTitle = x.TransportType.TransportTypeName,
                BLReleasedTypeName = x.BLReleaseType.BLReleaseTypeName,
                ReleaseDate = x.ReleaseDate,
                LoadTypeName = x.LoadType.LoadTypeCode,
                MovementTypeName = x.MovementType.MovementTypeCode,
                ExportReference = x.ExportReference,
                SerialNo = x.SerialNo,
                IsSwitched = x.IsSwitched,
                PORIdName = x.POR.LocationName,
                PORText = x.PORText,
                POLIdName = x.POL.LocationName,
                POLText = x.POLText,
                PODIdName = x.POD.LocationName,
                PODText = x.PODText,
                PODelIdName = x.PODel.LocationName,
                PODelText = x.PODelText,
                FinalDestinationText = x.FinalDestinationText,
                GoodsDescription = x.GoodsDescription,
                MarksAndNumbers = x.MarksAndNumbers,
                FreightPaymentAtIdName = x.FreightPaymentAt.LocationName,
                FreightPaymentAtText = x.FreightPaymentAtText,
                RemarksInBL = x.RemarksInBL,
                RemarksInManifest = x.RemarksInManifest,
                ChargeableWeight = x.ChargeableWeight,
                GrossWeightKg = x.GrossWeightKg,
                NetWeightKg = x.NetWeightKg,
                VolumeCBM = x.VolumeCBM,
                TotalContainers = x.TotalContainers,
                TotalPackages = x.TotalPackages,
                PackageTypeName = x.PackageType.PackageTypeName,
                PackageTypeText = x.PackageTypeText,
                BLStatusName = x.BLStatus.BLStatusName,
                SalesPersonName = x.SalesPerson.FullName,
                PaymentTypeName = x.PaymentType.PaymentTypeName,
                TranshipmentPortName = x.TranshipmentPort.LocationName,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreateUserName = x.CreateUser.FullName,
                EditUserName = x.EditUser.FullName,
                IncotermName = x.Incoterm.IncoTermTitle,
                MasterBLIssueDate = x.MasterBLIssueDate,
                MasterBLReleaseTypeName = x.MasterBLReleaseType.BLReleaseTypeName,
                MasterPaymentTypeName = x.MasterPaymentType.PaymentTypeName,
                MasterPlaceOfIssue = x.MasterPlaceOfIssue,
                MasterReleaseDate = x.MasterReleaseDate,
                PODDetentionTariffName = x.PODDetentionTariff.Name,
                POLDetentionTariffName = x.POLDetentionTariff.Name,
                MasterPODDetentionTariffName = x.MasterPODDetentionTariff.Name,
                MasterPOLDetentionTariffName = x.MasterPOLDetentionTariff.Name,
                //BlParty
                ShipperName = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 51).FirstOrDefault().Party.PersonName,
                ShipperText = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 51).FirstOrDefault().PartyTextInBL,
                ConsigneeName = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 52).FirstOrDefault().Party.PersonName,
                ConsigneeText = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 52).FirstOrDefault().PartyTextInBL,
                NotifyName = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 53).FirstOrDefault().Party.PersonName,
                NotifyText = queryBlParty.Where(bp => bp.BLId == x.Id && bp.RoleTypeId == 53).FirstOrDefault().PartyTextInBL,
                //

            }).ToListAsync();

            return new BLManagementPageViewModel_Report
            {
                Search = request,
                Summary = summary,
                PagedResult = new PagedResultDto<BLManagementRowDto_Report>
                {
                    Items = Items,
                    TotalCount = totalCount,
                    PageNumber = request.Page,
                    PageSize = request.PageSize
                }
            };
        }
        //------------Containers BL Report-------------------
        public async Task<BLManagementPageViewModel_ContainerReport> GetManagementPageData_ContainerReportAsync(BLManagementSearchRequest_ContainerReport request)
        {
            request ??= new BLManagementSearchRequest_ContainerReport();
            var query = _context.BLContainers.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            var queryBL = _context.BLs.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            var queryBlParty = _context.BLParties.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();
            //var queryBlContainer = _context.BLContainers.Where(x => !x.IsDeleted).AsNoTracking().AsQueryable();


            //Search System
            //1
            if (!string.IsNullOrWhiteSpace(request.BLNo)) query = query.Where(x => x.BL.BLNo.Contains(request.BLNo));
            //2
            if (!string.IsNullOrWhiteSpace(request.MasterBLNo)) query = query.Where(x => x.BL.MasterBLNo.Contains(request.MasterBLNo));
            //3
            if (request.TradeTypeId.HasValue) query = query.Where(x => x.BL.TradeTypeId == request.TradeTypeId.Value);
            ////4
            //if (request.TotalContainerFrom > 0) query = query.Where(x => x.BL.TotalContainers >= request.TotalContainerFrom.Value);
            ////5
            //if (request.TotalContainerTo > 0) query = query.Where(x => x.BL.TotalContainers <= request.TotalContainerTo.Value);
            //6
            if (request.TransportTypeId.HasValue) query = query.Where(x => x.BL.TransportTypeId == request.TransportTypeId.Value);
            //7
            if (request.SheeperId.HasValue) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyId == request.SheeperId.Value && p.RoleTypeId == 51));
            //8
            if (!string.IsNullOrWhiteSpace(request.SheeperText)) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyTextInBL.Contains(request.SheeperText) && p.RoleTypeId == 51));
            //9
            if (request.ConsigneeId.HasValue) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyId == request.ConsigneeId.Value && p.RoleTypeId == 52));
            //10
            if (!string.IsNullOrWhiteSpace(request.ConsigneeText)) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyTextInBL.Contains(request.ConsigneeText) && p.RoleTypeId == 52));
            //11
            if (request.NotifyId.HasValue) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyId == request.NotifyId.Value && p.RoleTypeId == 53));
            //12
            if (!string.IsNullOrWhiteSpace(request.NotifyText)) query = query.Where(x => x.BL.BLParties.Any(p => p.PartyTextInBL.Contains(request.NotifyText) && p.RoleTypeId == 53));
            //13
            if (request.CarrierId.HasValue) query = query.Where(x => x.BL.CarrierId == request.CarrierId.Value);
            //14
            if (request.CompanyId.HasValue) query = query.Where(x => x.BL.CompanyId == request.CompanyId.Value);
            //15
            if (!string.IsNullOrWhiteSpace(request.BookingNo)) query = query.Where(x => x.BL.Booking.BookingNo.Contains(request.BookingNo));
            //16
            if (!string.IsNullOrWhiteSpace(request.JobNo)) query = query.Where(x => x.BL.Job.JobNumber.Contains(request.JobNo));
            //17
            if (!string.IsNullOrWhiteSpace(request.JobFileNumber)) query = query.Where(x => x.BL.Job.JobFileNumber.Contains(request.JobFileNumber));
            //18
            if (!string.IsNullOrWhiteSpace(request.SalesUserName)) query = query.Where(x => x.BL.Job.SalesPerson.FullName.Contains(request.SalesUserName));
            //19
            if (request.BLTypeId.HasValue) query = query.Where(x => x.BL.BLTypeId == request.BLTypeId.Value);
            //20
            if (request.BLLevelId.HasValue) query = query.Where(x => x.BL.BLLevelId == request.BLLevelId.Value);
            //21
            if (request.BLIssueDateFrom.HasValue) query = query.Where(x => x.BL.BLIssueDate >= request.BLIssueDateFrom.Value);
            //22
            if (request.BLIssueDateTo.HasValue) query = query.Where(x => x.BL.BLIssueDate <= request.BLIssueDateTo.Value);
            //23
            if (!string.IsNullOrWhiteSpace(request.PlaceOfIssue)) query = query.Where(x => x.BL.PlaceOfIssue.Contains(request.PlaceOfIssue));
            //24
            if (request.IssuerPartyId.HasValue) query = query.Where(x => x.BL.IssuerPartyId == request.IssuerPartyId.Value);
            //25
            if (request.IssuerByUs.HasValue)
            {
                bool adv = request.IssuerByUs.Value == 1;
                query = query.Where(x => x.BL.IssuedByUs == adv);
            }
            //26
            if (request.BLReleasedTypeId.HasValue) query = query.Where(x => x.BL.BLReleaseTypeId == request.BLReleasedTypeId.Value);
            //27
            if (request.ReleaseDateFrom.HasValue) query = query.Where(x => x.BL.ReleaseDate >= request.ReleaseDateFrom.Value);
            //28
            if (request.ReleaseDateTo.HasValue) query = query.Where(x => x.BL.ReleaseDate <= request.ReleaseDateTo.Value);
            //29
            if (request.LoadTypeId.HasValue) query = query.Where(x => x.BL.LoadTypeId == request.LoadTypeId.Value);
            //30
            if (request.MovementTypeId.HasValue) query = query.Where(x => x.BL.MovementTypeId == request.MovementTypeId.Value);
            //31
            if (!string.IsNullOrWhiteSpace(request.ExportReference)) query = query.Where(x => x.BL.ExportReference.Contains(request.ExportReference));
            //32
            if (!string.IsNullOrWhiteSpace(request.SerialNo)) query = query.Where(x => x.BL.SerialNo.Contains(request.SerialNo));
            //33
            if (request.IsSwitched.HasValue)
            {
                bool adv = request.IsSwitched.Value == 1;
                query = query.Where(x => x.BL.IsSwitched == adv);
            }
            //34
            if (request.PORId.HasValue) query = query.Where(x => x.BL.PORId == request.PORId.Value);
            //35
            if (!string.IsNullOrWhiteSpace(request.PORText)) query = query.Where(x => x.BL.PORText.Contains(request.PORText));
            //36
            if (request.POLId.HasValue) query = query.Where(x => x.BL.POLId == request.POLId.Value);
            //37
            if (!string.IsNullOrWhiteSpace(request.POLText)) query = query.Where(x => x.BL.POLText.Contains(request.POLText));
            //38
            if (request.PODId.HasValue) query = query.Where(x => x.BL.PODId == request.PODId.Value);
            //39
            if (!string.IsNullOrWhiteSpace(request.PODText)) query = query.Where(x => x.BL.PODText.Contains(request.PODText));
            //40
            if (request.PODelId.HasValue) query = query.Where(x => x.BL.PODelId == request.PODelId.Value);
            //41
            if (!string.IsNullOrWhiteSpace(request.PODelText)) query = query.Where(x => x.BL.PODelText.Contains(request.PODelText));
            //42
            if (!string.IsNullOrWhiteSpace(request.FinalDestinationText)) query = query.Where(x => x.BL.FinalDestinationText.Contains(request.FinalDestinationText));
            //43
            if (!string.IsNullOrWhiteSpace(request.GoodsDescription)) query = query.Where(x => x.BL.GoodsDescription.Contains(request.GoodsDescription));
            //44
            if (!string.IsNullOrWhiteSpace(request.MarksAndNumbers)) query = query.Where(x => x.BL.MarksAndNumbers.Contains(request.MarksAndNumbers));
            //45
            if (request.FreightPaymentAtId.HasValue) query = query.Where(x => x.BL.FreightPaymentAtId == request.FreightPaymentAtId.Value);
            //46
            if (!string.IsNullOrWhiteSpace(request.FreightPaymentAtText)) query = query.Where(x => x.BL.FreightPaymentAtText.Contains(request.FreightPaymentAtText));
            //47
            if (!string.IsNullOrWhiteSpace(request.RemarksInBL)) query = query.Where(x => x.BL.RemarksInBL.Contains(request.RemarksInBL));
            //48
            if (!string.IsNullOrWhiteSpace(request.RemarksInManifest)) query = query.Where(x => x.BL.RemarksInManifest.Contains(request.RemarksInManifest));
            //49
            if (request.PackageTypeId.HasValue) query = query.Where(x => x.BL.PackageTypeId == request.PackageTypeId.Value);
            //50
            if (request.BLStatusId.HasValue) query = query.Where(x => x.BL.BLStatusId == request.BLStatusId.Value);
            //51
            if (request.SalesPersonId.HasValue) query = query.Where(x => x.BL.SalesPersonId == request.SalesPersonId.Value);
            //52
            if (request.PaymentTypeId.HasValue) query = query.Where(x => x.BL.PaymentTypeId == request.PaymentTypeId.Value);
            //53
            if (request.TranshipmentPortId.HasValue) query = query.Where(x => x.BL.TranshipmentPortId == request.TranshipmentPortId.Value);
            //54
            if (request.CreatedAtFrom.HasValue) query = query.Where(x => x.BL.CreatedAt >= request.CreatedAtFrom.Value);
            //55
            if (request.CreatedAtTo.HasValue) query = query.Where(x => x.BL.CreatedAt <= request.CreatedAtTo.Value);
            //56
            if (!string.IsNullOrWhiteSpace(request.CreateUserName)) query = query.Where(x => x.BL.CreateUser.FullName.Contains(request.CreateUserName));
            //57
            if (request.UpdatedAtFrom.HasValue) query = query.Where(x => x.BL.UpdatedAt >= request.UpdatedAtFrom.Value);
            //58
            if (request.UpdatedAtTo.HasValue) query = query.Where(x => x.BL.UpdatedAt <= request.UpdatedAtTo.Value);
            //59
            if (!string.IsNullOrWhiteSpace(request.EditUserName)) query = query.Where(x => x.BL.EditUser.FullName.Contains(request.EditUserName));
            //60
            if (request.IncotermId.HasValue) query = query.Where(x => x.BL.IncotermId == request.IncotermId.Value);
            //61
            if (request.MasterBLReleaseTypeId.HasValue) query = query.Where(x => x.BL.MasterBLReleaseTypeId == request.MasterBLReleaseTypeId.Value);
            //62
            if (request.MasterPaymentTypeId.HasValue) query = query.Where(x => x.BL.MasterPaymentTypeId == request.MasterPaymentTypeId.Value);
            //63
            if (!string.IsNullOrWhiteSpace(request.MasterPlaceOfIssue)) query = query.Where(x => x.BL.MasterPlaceOfIssue.Contains(request.MasterPlaceOfIssue));
            //Containers Bl Filter
            if (!string.IsNullOrWhiteSpace(request.ContainerNo)) query = query.Where(x => x.ContainerNumber.Contains(request.ContainerNo));
            if (request.ContainerSizeTypeId.HasValue) query = query.Where(x => x.ContainerSizeTypeId == request.ContainerSizeTypeId.Value);
            if (!string.IsNullOrWhiteSpace(request.SealNumber)) query = query.Where(x => x.SealNumber.Contains(request.SealNumber));
            if (request.CntOwnTypeId.HasValue) query = query.Where(x => x.CntOwnTypeId == request.CntOwnTypeId.Value);
            if (request.FullMTId.HasValue) query = query.Where(x => x.FullMTId == request.FullMTId.Value);
            if (request.ContainerLoadTypeId.HasValue) query = query.Where(x => x.LoadTypeId == request.ContainerLoadTypeId.Value);

            // ۴. پیاده‌سازی Paging
            //var totalCount = summary.TotalBills;
            var skip = (request.Page - 1) * request.PageSize;

            var Items = await query.OrderByDescending(x => x.BL.CreatedAt).ThenBy(x => x.Seq).Skip(skip).Take(request.PageSize).Select(x => new BLManagementRowDto_ContainerReport
            {
                BLId = x.Id,
                BLNo = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLNo,
                MasterBLNo = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterBLNo,
                CompanyName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Company.Person.PersonName,
                TradeTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().TradeType.TradeTypeName,
                BookingNo = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Booking.BookingNo,
                JobFileNumber = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Job.JobFileNumber,
                JobNo = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Job.JobNumber,
                SalesUserName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Job.SalesPerson.FullName,
                BLTypeTitle = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLType.BLTypeName,
                BLLevelTitle = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLLevel.BLLevelName,
                BLIssueDate = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLIssueDate,
                PlaceOfIssue = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PlaceOfIssue,
                CarrierName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Carrier.Person.PersonName,
                IssuerPartyName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().IssuerParty.PersonName,
                IssuerByUs = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().IssuedByUs,
                TransportTypeTitle = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().TransportType.TransportTypeName,
                BLReleasedTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLReleaseType.BLReleaseTypeName,
                ReleaseDate = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().ReleaseDate,
                LoadTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().LoadType.LoadTypeCode,
                MovementTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MovementType.MovementTypeCode,
                ExportReference = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().ExportReference,
                SerialNo = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().SerialNo,
                IsSwitched = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().IsSwitched,
                PORIdName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().POR.LocationName,
                PORText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PORText,
                POLIdName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().POL.LocationName,
                POLText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().POLText,
                PODIdName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().POD.LocationName,
                PODText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PODText,
                PODelIdName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PODel.LocationName,
                PODelText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PODelText,
                FinalDestinationText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().FinalDestinationText,
                GoodsDescription = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().GoodsDescription,
                MarksAndNumbers = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MarksAndNumbers,
                FreightPaymentAtIdName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().FreightPaymentAt.LocationName,
                FreightPaymentAtText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().FreightPaymentAtText,
                RemarksInBL = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().RemarksInBL,
                RemarksInManifest = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().RemarksInManifest,
                ChargeableWeight = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().ChargeableWeight,
                GrossWeightKg = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().GrossWeightKg,
                NetWeightKg = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().NetWeightKg,
                VolumeCBM = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().VolumeCBM,
                //TotalContainers = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().TotalContainers,
                TotalPackages = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().TotalPackages,
                PackageTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PackageType.PackageTypeName,
                PackageTypeText = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PackageTypeText,
                BLStatusName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().BLStatus.BLStatusName,
                SalesPersonName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().SalesPerson.FullName,
                PaymentTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PaymentType.PaymentTypeName,
                TranshipmentPortName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().TranshipmentPort.LocationName,
                CreatedAt = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().CreatedAt,
                UpdatedAt = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().UpdatedAt,
                CreateUserName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().CreateUser.FullName,
                EditUserName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().EditUser.FullName,
                IncotermName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().Incoterm.IncoTermTitle,
                MasterBLIssueDate = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterBLIssueDate,
                MasterBLReleaseTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterBLReleaseType.BLReleaseTypeName,
                MasterPaymentTypeName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterPaymentType.PaymentTypeName,
                MasterPlaceOfIssue = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterPlaceOfIssue,
                MasterReleaseDate = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterReleaseDate,
                PODDetentionTariffName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().PODDetentionTariff.Name,
                POLDetentionTariffName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().POLDetentionTariff.Name,
                MasterPODDetentionTariffName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterPODDetentionTariff.Name,
                MasterPOLDetentionTariffName = queryBL.Where(b => b.Id == x.BLId && !b.IsDeleted).FirstOrDefault().MasterPOLDetentionTariff.Name,
                ////BlParty
                ShipperName = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 51).FirstOrDefault().Party.PersonName,
                ShipperText = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 51).FirstOrDefault().PartyTextInBL,
                ConsigneeName = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 52).FirstOrDefault().Party.PersonName,
                ConsigneeText = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 52).FirstOrDefault().PartyTextInBL,
                NotifyName = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 53).FirstOrDefault().Party.PersonName,
                NotifyText = queryBlParty.Where(bp => bp.BLId == x.BLId && bp.RoleTypeId == 53).FirstOrDefault().PartyTextInBL,
                ////ContainersBL
                ContainerNumber = x.ContainerNumber,
                ContainerSizeTypeName = x.ContainerSizeType.ShortName,
                SealNumber = x.SealNumber,
                TotalPackageCount = x.TotalPackageCount,
                TotalVolumeCBM = x.TotalVolumeCBM,
                TotalGrossWeight = x.TotalGrossWeight,
                TotalNetWeight = x.TotalNetWeight,
                BLContainerPercent = x.BLContainerPercent,
                OverHeight = x.OverHeight,
                OverWidth = x.OverWidth,
                OverLength = x.OverLength,
                DimensionUnitName = x.DimensionUnit.MeasurementUnitName,
                CntOwnTypeName = x.CntOwnType.CntOwnTypeName,
                ExportPermissionNo = x.ExportPermissionNo,
                FullMTName = x.FullMT.FullMTName,
                LoadTypeNameContainer = x.LoadType.LoadTypeCode,
                TareWeight = x.TareWeight,
                Seq = x.Seq

            }).ToListAsync();

            return new BLManagementPageViewModel_ContainerReport
            {
                Search = request,
                //Summary = summary,
                PagedResult = new PagedResultDto<BLManagementRowDto_ContainerReport>
                {
                    Items = Items,
                    TotalCount = query.Count(),
                    PageNumber = request.Page,
                    PageSize = request.PageSize
                }
            };
        }
        //--------------BLs Stimul Report--------------------

        public async Task<BLsReportCompleteDto> GetTotalStimulBLsReport(Guid id)
        {
            var BLquery = _context.BLs.Where(x => !x.IsDeleted && x.Id == id).AsNoTracking();
            var BLpartyquery = _context.BLParties.Where(x => !x.IsDeleted && x.BLId == id);
            var BLContainersquery = _context.BLContainers.Where(x => !x.IsDeleted && x.BLId == id);
            var BLVoyagesquery = _context.BLVoyages.Where(x => !x.IsDeleted && x.BLId == id);

            // ۱. استخراج HSCode
            var hsCodesList = await _context.CargoDetails
                .Where(cd => !cd.IsDeleted && cd.BLId == id && !string.IsNullOrEmpty(cd.HSCodeToSave))
                .Select(cd => cd.HSCodeToSave.Trim())
                .Distinct()
                .ToListAsync();

            var hsCodes = string.Join(", ", hsCodesList);

            // ۲. استخراج ArrivalDate بر اساس VoyageTypeId = 2 و PODId بارنامه
            var blInfo = await BLquery.Select(b => new { b.PODId }).FirstOrDefaultAsync();
            var voyage2 = await BLVoyagesquery
                .Where(v => v.VoyageTypeId == 2)
                .OrderByDescending(v => v.Id)
                .Select(v => new { v.VoyageId })
                .FirstOrDefaultAsync();

            DateTime? arrivalDate = null;
            if (blInfo?.PODId != null && voyage2?.VoyageId != null)
            {
                arrivalDate = await (from vp in _context.VoyagePorts
                                     join vpe in _context.VoyagePortEvents on vp.Id equals vpe.VoyagePortId
                                     where vp.InboundVoyageId == voyage2.VoyageId
                                        && vp.PortId == blInfo.PODId
                                        && vpe.EventTypeId == 105
                                     orderby vpe.VoyagePortEventDateTime descending
                                     select (DateTime?)vpe.VoyagePortEventDateTime)
                                     .FirstOrDefaultAsync();
            }

            // ۳. کوئری اصلی
            var item = await BLquery.Select(x => new BLsReportCompleteDto
            {
                CompanyId = x.CompanyId,
                Id = x.Id,
                BLNo = x.BLNo.Trim(),
                JobFileNo = x.Job.JobFileNumber.Trim(),
                JobNo = x.Job.JobNumber.Trim(),
                PORText = x.PORText.Trim(),
                POLText = x.POLText.Trim(),
                PODText = x.PODText.Trim(),
                PODelText = x.PODelText.Trim(),
                FinalDestinationText = x.FinalDestinationText.Trim(),
                GoodsDescription = x.GoodsDescription.Trim(),
                CarriresAgentAtDischargePort = "",
                BLIssueDate = x.BLIssueDate,
                PackageTypeText = x.PackageTypeText,
                MasterBLIssueDate = x.MasterBLIssueDate,
                IssuerParty = x.IssuerParty.PersonName.Trim(),
                RemarksInManifest = x.RemarksInManifest,
                PaymentTypeText = x.PaymentType.PaymentTypeName,
                //
                HSCode = hsCodes,
                ArrivalDate = arrivalDate, // <-- اضافه شد
                                           //
                ShipperText = BLpartyquery.Where(bp => bp.RoleTypeId == 51).Select(bp => bp.PartyTextInBL).FirstOrDefault().Trim(),
                ConsigneeText = BLpartyquery.Where(bp => bp.RoleTypeId == 52).Select(bp => bp.PartyTextInBL).FirstOrDefault().Trim(),
                NotifyText = BLpartyquery.Where(bp => bp.RoleTypeId == 53).Select(bp => bp.PartyTextInBL).FirstOrDefault().Trim(),
                //
                VesseleName2 = BLVoyagesquery.Where(v => v.VoyageTypeId == 2).Select(v => v.VesselName).FirstOrDefault().Trim(),
                VoyageNo2 = BLVoyagesquery.Where(v => v.VoyageTypeId == 2).Select(v => v.VoyageNo).FirstOrDefault().Trim(),
                VesseleName1 = BLVoyagesquery.Where(v => v.VoyageTypeId == 1).Select(v => v.VesselName).FirstOrDefault().Trim(),
                VoyageNo1 = BLVoyagesquery.Where(v => v.VoyageTypeId == 1).Select(v => v.VoyageNo).FirstOrDefault().Trim(),
                //
                TradeType = x.TradeType.TradeTypeName,
                BLContainersDetails = BLContainersquery.Select(c => new BLContainersReportDto
                {
                    ContainerNumber = c.ContainerNumber.Trim(),
                    ShortName = c.ContainerSizeType.ShortName.Trim(),
                    SealNumber = c.SealNumber.Trim(),
                    LoadTypeCode = c.LoadType.LoadTypeCode.Trim(),
                    TotalPackageCount = c.TotalPackageCount,
                    TotalGrossWeight = c.TotalGrossWeight,
                    TotalNetWeight = c.TotalNetWeight,
                    CntOwnTypeName = c.CntOwnType.CntOwnTypeName.Trim(),
                    SOC = c.CntOwnType.CntOwnTypeName,
                    FMT = c.FullMT.FullMTName,
                    Movement = c.LoadType.LoadTypeCode,
                    TareWeight = c.TareWeight,

                }).ToList()

            }).SingleOrDefaultAsync();

            return item;
        }



        public async Task<DOProcessModalVm> GetDOProcessModalAsync(
            Guid blId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var bl = await _context.Set<BL>()
                .AsNoTracking()
                .Where(x => x.Id == blId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.JobId,
                    x.BLNo,
                    x.BLReleaseTypeId
                })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("BL not found.");

            var process = await _context.Set<DOProcess>()
                .AsNoTracking()
                .Include(x => x.ChecklistItems)
                    .ThenInclude(x => x.CheckedByUser)
                .SingleOrDefaultAsync(
                    x => x.BLId == blId && !x.IsDeleted,
                    cancellationToken);

            var jobParties = await _context.Set<JobParty>()
                .AsNoTracking()
                .Where(x => x.JobId == bl.JobId && !x.IsDeleted)
                .OrderBy(x => x.SequenceNo)
                .Select(x => new
                {
                    x.PartyId,
                    PartyName = x.Party.PersonName,
                    x.RoleTypeId
                })
                .Distinct()
                .ToListAsync(cancellationToken);


            var releaseTypes = await _context.Set<BLReleaseType>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    Text = x.BLReleaseTypeName,
                })
                .ToListAsync(cancellationToken);




            var definitions = await _context.Set<DOChecklistDefinition>()
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .Select(x => new DOChecklistDisplayVm
                {
                    Id = x.Id,
                    Name = x.CheckListLocalName,
                    IsMandatory = x.IsMandatory
                })
                .ToListAsync(cancellationToken);

            var savedItems = process?.ChecklistItems
                .Where(x => !x.IsDeleted)
                .ToDictionary(x => x.ChecklistDefinitionId)
                ?? new Dictionary<int, DOProcessChecklistItem>();




            var hasProcess = process is not null;
            var approvalStages = new List<DOApprovalStageVm>
            {
                new()
                {
                    Stage = DOApprovalStage.CS,
                    Title = "Customer Service",
                    Status = process?.CSControllerStatus
                             ?? DOControlStatus.Pending,
                    ControllerId = process?.CSControllerId,
                    Date = process?.CSControllerDate,
                    Comments = process?.CSControllerComments,

                    // مقدار واقعی را با authorization service مشخص می‌کنیم.
                    CanApprove = hasProcess &&
                                 process!.CSControllerStatus ==
                                 DOControlStatus.Pending
                },

                new()
                {
                    Stage = DOApprovalStage.Finance,
                    Title = "Finance",
                    Status = process?.FinanceControllerStatus
                             ?? DOControlStatus.Pending,
                    ControllerId = process?.FinanceControllerId,
                    Date = process?.FinanceControllerDate,
                    Comments = process?.FinanceControllerComments,

                    CanApprove = hasProcess &&
                                 process!.CSControllerStatus ==
                                 DOControlStatus.Approved &&
                                 process.FinanceControllerStatus ==
                                 DOControlStatus.Pending
                },

                new()
                {
                    Stage = DOApprovalStage.Final,
                    Title = "Final Control",
                    Status = process?.FinalControllerStatus
                             ?? DOControlStatus.Pending,
                    ControllerId = process?.FinalControllerId,
                    Date = process?.FinalControllerDate,
                    Comments = process?.FinalControllerComments,

                    CanApprove = hasProcess &&
                                 process!.FinanceControllerStatus ==
                                 DOControlStatus.Approved &&
                                 process.FinalControllerStatus ==
                                 DOControlStatus.Pending
                }
            };

            foreach (var stage in approvalStages)
            {
                stage.CanApprove =
                    process is not null &&
                    IsStageReady(process, stage.Stage) &&
                    await CanApproveAsync(userId, stage.Stage, cancellationToken);
            }

            var customerDebts = new List<DOCustomerDebtRowVm>();

            if (process is not null)
            {
                // ۱. ابتدا بررسی کنیم آیا اسنپ‌شات ذخیره‌شده در دیتابیس داریم؟
                var snapshots = await _context.Set<DOProcessCustomerDebtSnapshot>()
                    .AsNoTracking()
                    .Where(x => x.DOProcessId == process.Id && !x.IsDeleted)
                    .ToListAsync(cancellationToken);

                if (snapshots.Count > 0)
                {
                    // خواندن از اسنپ‌شات ذخیره‌شده
                    customerDebts = snapshots.Select(x => new DOCustomerDebtRowVm
                    {
                        Source = x.InvoiceId.HasValue ? "Invoice" :
                                 x.DetentionId.HasValue ? "Detention" : "OnlineDetention",
                        Description = x.Description,
                        IssueDate = x.IssueDate,
                        Amount = x.Amount,
                        CurrencyId = x.CurrencyId.Value,
                        InvoiceId = x.InvoiceId,
                        DetentionId = x.DetentionId,
                        OnlineDetentionId = x.OnlineDetentionId
                    }).ToList();
                }
                else if (process.CustomerId.HasValue)
                {
                    // ۲. اگر هنوز تصمیمی گرفته نشده و اسنپ‌شاتی نیست، بدهی زنده را می‌خوانیم
                    customerDebts = await LoadCustomerDebtsAsync(process.CustomerId.Value, cancellationToken);
                }
            }
            var hasDebts = customerDebts.Count > 0;

            var canApproveSettlementException =
                process is not null &&
                hasDebts &&
                !process.SettlementExceptionApprovedAt.HasValue &&
                process.CSControllerStatus == DOControlStatus.Approved &&
                process.FinanceControllerStatus == DOControlStatus.Pending &&
                await CanApproveSettlementExceptionAsync(
                    userId,
                    cancellationToken);


            return new DOProcessModalVm
            {
                BLNo = bl.BLNo,
                BLReleaseTypeId =
                    process?.BLReleaseTypeId ?? bl.BLReleaseTypeId,

                IsLocked = process is not null &&
                (
                    process.CSControllerStatus != DOControlStatus.Pending ||
                    process.FinanceControllerStatus != DOControlStatus.Pending ||
                    process.FinalControllerStatus != DOControlStatus.Pending
                ),
                DOProcessId = process is null ? null : process.Id,
                ApprovalStages = approvalStages,
                CustomerDebts = customerDebts,


                SettlementExceptionApprovedAt = process?.SettlementExceptionApprovedAt,
                SettlementExceptionApprovedById = process?.SettlementExceptionApprovedById,
                SettlementExceptionReason = process?.SettlementExceptionReason,
                CanApproveSettlementException = canApproveSettlementException,


                //public bool IsLocked =>
                //    Process is not null &&
                //    (
                //        Process.CSControllerStatus != DOControlStatus.Pending ||
                //        Process.FinanceControllerStatus != DOControlStatus.Pending ||
                //        Process.FinalControllerStatus != DOControlStatus.Pending
                //    );




                Parties = jobParties.Select(x => new DOJobPartyOptionVm
                {
                    PartyId = x.PartyId,
                    Text = $"{x.PartyName}"
                }).Distinct().ToList(),
                BLReleaseTypes = releaseTypes.Select(x => new DOReleaseTypeOptionVm
                {
                    Id = x.Id,
                    Text = $"{x.Text}"
                }).ToList(),



                Definitions = definitions,

                Draft = new DOProcessDraftInputVm
                {
                    BLId = bl.Id,
                    CustomerId = process?.CustomerId,
                    LawyerId = process?.LawyerId,
                    CustomerRequestDate = process?.CustomerRequestDate,
                    LineDODate = process?.LineDODate,
                    Remarks = process?.Remarks,

                    Checklist = definitions.Select(d =>
                    {
                        savedItems.TryGetValue(d.Id, out var item);

                        return new DOChecklistInputVm
                        {
                            ChecklistDefinitionId = d.Id,
                            IsChecked = item?.IsChecked ?? false,
                            Comments = item?.Comments,
                            CheckedByUserId = item?.CheckedByUserId,
                            CheckedByUserName = item?.CheckedByUser?.FullName
                        };
                    }).ToList()
                }
            };
        }

        private async Task<bool> CanApproveSettlementExceptionAsync(Guid userId, CancellationToken cancellationToken)
        {
            //موقت
            return true;
            return await _authorizationService.HasPermissionAsync(userId, "DO.SettlementException.Approve");
        }

        public async Task SaveDOProcessDraftAsync(
            DOProcessDraftInputVm input,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (input.BLId == Guid.Empty)
                throw new InvalidOperationException("BL is required.");

            await using var transaction = await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var bl = await _context.Set<BL>()
                .Where(x => x.Id == input.BLId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.JobId,
                    x.BLReleaseTypeId
                })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("BL not found.");

            var allowedPartyIds = await _context.Set<JobParty>()
                .Where(x => x.JobId == bl.JobId && !x.IsDeleted)
                .Select(x => x.PartyId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var allowed = allowedPartyIds.ToHashSet();

            if (input.CustomerId is Guid customer &&
                (customer == Guid.Empty || !allowed.Contains(customer)))
                throw new InvalidOperationException(
                    "Customer must belong to this BL's JobParty.");

            if (input.LawyerId is Guid lawyer &&
                (lawyer == Guid.Empty || !allowed.Contains(lawyer)))
                throw new InvalidOperationException(
                    "Lawyer must belong to this BL's JobParty.");

            var definitions = await _context.Set<DOChecklistDefinition>()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            var postedIds = input.Checklist
                .Select(x => x.ChecklistDefinitionId)
                .ToList();

            // از ثبت آیتم ناشناس، تکراری یا فرم قدیمی جلوگیری می‌کند.
            if (postedIds.Count != definitions.Count ||
                postedIds.Distinct().Count() != postedIds.Count ||
                !postedIds.ToHashSet().SetEquals(definitions))
            {
                throw new InvalidOperationException(
                    "Checklist has changed. Reload the form.");
            }

            var process = await _context.Set<DOProcess>()
                .Include(x => x.ChecklistItems)
                .SingleOrDefaultAsync(
                    x => x.BLId == input.BLId && !x.IsDeleted,
                    cancellationToken);

            if (process is null)
            {
                process = new DOProcess
                {
                    BLId = bl.Id,
                    BLReleaseTypeId = bl.BLReleaseTypeId,
                    CSControllerStatus = DOControlStatus.Pending,
                    FinanceControllerStatus = DOControlStatus.Pending,
                    FinalControllerStatus = DOControlStatus.Pending,
                    CreatedAt = DateTime.Now,
                    CreateUserId = userId
                };

                _context.Set<DOProcess>().Add(process);
            }
            else if (process.CSControllerStatus == DOControlStatus.Approved || process.FinanceControllerStatus == DOControlStatus.Approved || process.FinalControllerStatus == DOControlStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Draft cannot be changed after the first approval.");
            }
            else
            {
                process.UpdatedAt = DateTime.Now;
                process.EditUserId = userId;
            }

            process.CustomerId = input.CustomerId;
            process.LawyerId = input.LawyerId;
            process.CustomerRequestDate = input.CustomerRequestDate;
            process.LineDODate = input.LineDODate;
            process.Remarks = input.Remarks;

            var existingItems = process.ChecklistItems
                .Where(x => !x.IsDeleted)
                .ToDictionary(x => x.ChecklistDefinitionId);

            foreach (var posted in input.Checklist)
            {
                if (!existingItems.TryGetValue(
                        posted.ChecklistDefinitionId, out var item))
                {
                    item = new DOProcessChecklistItem
                    {
                        ChecklistDefinitionId = posted.ChecklistDefinitionId
                    };

                    process.ChecklistItems.Add(item);
                }

                if (item.IsChecked != posted.IsChecked)
                {
                    item.CheckedByUserId =
                        posted.IsChecked ? userId : null;
                    item.CheckedAt =
                        posted.IsChecked ? DateTime.Now : null;
                }
                item.IsChecked = posted.IsChecked;
                item.Comments = posted.Comments?.Trim();
            }
            var hasFirstDecision =
                process.CSControllerStatus != DOControlStatus.Pending ||
                process.FinanceControllerStatus != DOControlStatus.Pending ||
                process.FinalControllerStatus != DOControlStatus.Pending;

            if (hasFirstDecision)
            {
                throw new InvalidOperationException(
                    "Draft cannot be changed after the first decision.");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        public async Task ApproveDOProcessStageAsync(
            DOApprovalInputVm input,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (input.DOProcessId == Guid.Empty)
                throw new InvalidOperationException(
                    "DO process is required.");

            if (userId == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "Authenticated user is required.");

            if (input.Decision is not
                (DOControlStatus.Approved or DOControlStatus.Rejected))
            {
                throw new InvalidOperationException(
                    "Only Approved or Rejected decisions are allowed.");
            }

            var canApprove = await CanApproveAsync(
                    userId,
                    input.Stage,
                    cancellationToken);

            if (!canApprove)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to approve this stage.");
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var process = await _context.Set<DOProcess>()
                .SingleOrDefaultAsync(
                    x => x.Id == input.DOProcessId &&
                         !x.IsDeleted,
                    cancellationToken);

            if (process is null)
                throw new KeyNotFoundException(
                    "DO process not found.");

            if (process.FinalControllerStatus ==
                DOControlStatus.Approved)
            {
                throw new InvalidOperationException(
                    "The DO process is already finalized.");
            }

            switch (input.Stage)
            {
                case DOApprovalStage.CS:
                    ApproveCsStage(process, input, userId);
                    var hasSnapshot = await _context.Set<DOProcessCustomerDebtSnapshot>()
                        .AnyAsync(x => x.DOProcessId == process.Id && !x.IsDeleted, cancellationToken);
                    if (!hasSnapshot)
                    {
                        await CreateCustomerDebtSnapshotsAsync(process.Id, userId, cancellationToken);
                    }

                    break;

                case DOApprovalStage.Finance:
                    await EnsureFinanceStageCanStartAsync(process, default);
                    ApproveFinanceStage(process, input, userId);
                    break;

                case DOApprovalStage.Final:
                    EnsureFinalStageCanStart(process);
                    ApproveFinalStage(process, input, userId);
                    break;

                default:
                    throw new InvalidOperationException(
                        "Invalid approval stage.");
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<bool> CanApproveAsync(Guid userId, DOApprovalStage stage, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                return false;

            //موقت
            return true;

            return stage switch
            {
                DOApprovalStage.CS => await _authorizationService.HasPermissionAsync(userId, "DO.CS.Approve"),
                DOApprovalStage.Finance => await _authorizationService.HasPermissionAsync(userId, "DO.Finance.Approve"),
                DOApprovalStage.Final => await _authorizationService.HasPermissionAsync(userId, "DO.Final.Approve"),
                _ => false
            };
        }

        private static void ApproveCsStage(
            DOProcess process,
            DOApprovalInputVm input,
            Guid userId)
        {
            if (process.CSControllerStatus !=
                DOControlStatus.Pending)
            {
                throw new InvalidOperationException(
                    "CS stage has already been decided.");
            }

            process.CSControllerId = userId;
            process.CSControllerDate = DateTime.Now;
            process.CSControllerComments =
                input.Comments?.Trim();
            process.CSControllerStatus =
                input.Decision;
        }

        //private static void EnsureFinanceStageCanStart(
        //    DOProcess process)
        //{
        //    if (process.CSControllerStatus !=
        //        DOControlStatus.Approved)
        //    {
        //        throw new InvalidOperationException(
        //            "Finance approval requires approved CS stage.");
        //    }

        //    if (process.FinanceControllerStatus !=
        //        DOControlStatus.Pending)
        //    {
        //        throw new InvalidOperationException(
        //            "Finance stage has already been decided.");
        //    }
        //}

        private static void ApproveFinanceStage(
            DOProcess process,
            DOApprovalInputVm input,
            Guid userId)
        {
            process.FinanceControllerId = userId;
            process.FinanceControllerDate = DateTime.Now;
            process.FinanceControllerComments =
                input.Comments?.Trim();
            process.FinanceControllerStatus =
                input.Decision;
        }

        private static void EnsureFinalStageCanStart(
            DOProcess process)
        {
            if (process.FinanceControllerStatus !=
                DOControlStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Final approval requires approved Finance stage.");
            }

            if (process.FinalControllerStatus !=
                DOControlStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Final stage has already been decided.");
            }
        }

        private static void ApproveFinalStage(
            DOProcess process,
            DOApprovalInputVm input,
            Guid userId)
        {
            process.FinalControllerId = userId;
            process.FinalControllerDate = DateTime.Now;
            process.FinalControllerComments =
                input.Comments?.Trim();
            process.FinalControllerStatus =
                input.Decision;
        }

        private static bool IsStageReady(
            DOProcess process,
            DOApprovalStage stage)
        {
            return stage switch
            {
                DOApprovalStage.CS =>
                    process.CSControllerStatus ==
                    DOControlStatus.Pending,

                DOApprovalStage.Finance =>
                    process.CSControllerStatus ==
                    DOControlStatus.Approved &&
                    process.FinanceControllerStatus ==
                    DOControlStatus.Pending,

                DOApprovalStage.Final =>
                    process.FinanceControllerStatus ==
                    DOControlStatus.Approved &&
                    process.FinalControllerStatus ==
                    DOControlStatus.Pending,

                _ => false
            };
        }
        public async Task<Guid> GetBLIdByProcessIdAsync(
            Guid processId,
            CancellationToken cancellationToken = default)
        {
            if (processId == Guid.Empty)
                throw new ArgumentException(
                    "DO process id is required.",
                    nameof(processId));

            var blId = await _context.Set<DOProcess>()
                .AsNoTracking()
                .Where(x =>
                    x.Id == processId &&
                    !x.IsDeleted)
                .Select(x => (Guid?)x.BLId)
                .SingleOrDefaultAsync(cancellationToken);

            if (!blId.HasValue || blId.Value == Guid.Empty)
                throw new KeyNotFoundException(
                    "DO process or related BL was not found.");

            return blId.Value;
        }
        private async Task<List<DOCustomerDebtRowVm>> LoadCustomerDebtsAsync(
            Guid customerId,
            CancellationToken cancellationToken)
        {
            var result = new List<DOCustomerDebtRowVm>();

            var invoices = await _context.Set<Invoice>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.PartyId == customerId &&
                    x.TotalAmount - x.SettledAmount > 0)
                .Select(x => new DOCustomerDebtRowVm
                {
                    Source = "Invoice",
                    Description = x.Remarks,
                    IssueDate = x.InvoiceDate,
                    Amount = x.TotalAmount - x.SettledAmount,
                    CurrencyId = x.CurrencyId,
                    InvoiceId = x.Id,
                    SubjectName = x.InvoiceSubject.InvoiceSubjectLocalName,
                    CurrencyCode = x.Currency.CurrencyCode
                })
                .ToListAsync(cancellationToken);

            result.AddRange(invoices);
            var detentions = await _context.Set<Detention>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.PartyId == customerId &&
                    x.FinalPayableAmount - x.SettledAmount > 0)
                .Select(x => new DOCustomerDebtRowVm
                {
                    Source = "Detention",
                    Description = x.Remarks,
                    IssueDate = x.CreatedAt,
                    Amount = x.FinalPayableAmount - x.SettledAmount,
                    CurrencyId = x.FinalCurrencyId,
                    DetentionId = x.Id,
                    SubjectName = "Detention",
                    CurrencyCode = x.PaymentCurrency.CurrencyCode
                })
                .ToListAsync(cancellationToken);

            result.AddRange(detentions);

            var onlineDetentions = await _context.Set<OnlineDetention>()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.IssuedToPartyId == customerId &&
                    x.OnlineDetentionNet > 0)
                .Select(x => new DOCustomerDebtRowVm
                {
                    Source = "OnlineDetention",
                    Description = "آنلاین",
                    IssueDate = x.UpdatedAt,
                    Amount = x.OnlineDetentionNet,
                    CurrencyId = x.CurrencyId,
                    OnlineDetentionId = x.Id
                })
                .ToListAsync(cancellationToken);

            result.AddRange(onlineDetentions);

            return result;
        }

        public async Task ApproveDOSettlementExceptionAsync(
            Guid processId,
            string reason,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (processId == Guid.Empty)
                throw new InvalidOperationException(
                    "DO process is required.");

            if (userId == Guid.Empty)
                throw new UnauthorizedAccessException(
                    "Authenticated user is required.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException(
                    "Settlement exception reason is required.");

            if (!await CanApproveSettlementExceptionAsync(
                    userId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to approve settlement exception.");
            }

            await using var transaction = await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            var process = await _context.Set<DOProcess>()
                .SingleOrDefaultAsync(
                    x => x.Id == processId && !x.IsDeleted,
                    cancellationToken);

            if (process is null)
                throw new KeyNotFoundException(
                    "DO process not found.");

            if (process.SettlementExceptionApprovedAt.HasValue)
                throw new InvalidOperationException(
                    "Settlement exception has already been approved.");

            if (process.CSControllerStatus != DOControlStatus.Approved)
                throw new InvalidOperationException(
                    "Settlement exception requires approved CS stage.");

            if (process.FinanceControllerStatus != DOControlStatus.Pending)
                throw new InvalidOperationException(
                    "Settlement exception must be approved before Finance decision.");

            if (!process.CustomerId.HasValue)
                throw new InvalidOperationException(
                    "Customer is not selected.");

            var debts = await LoadCustomerDebtsAsync(
                process.CustomerId.Value,
                cancellationToken);

            if (debts.Count == 0)
                throw new InvalidOperationException(
                    "No open customer debt exists.");

            process.SettlementExceptionApprovedById = userId;
            process.SettlementExceptionApprovedAt = DateTime.Now;
            process.SettlementExceptionReason = reason.Trim();

            var hasSnapshot = await _context.Set<DOProcessCustomerDebtSnapshot>()
                .AnyAsync(x => x.DOProcessId == process.Id && !x.IsDeleted, cancellationToken);
            if (!hasSnapshot)
            {
                await CreateCustomerDebtSnapshotsAsync(process.Id, userId, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        private async Task EnsureFinanceStageCanStartAsync(
            DOProcess process,
            CancellationToken cancellationToken)
        {
            if (process.CSControllerStatus != DOControlStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Finance approval requires approved CS stage.");
            }

            if (process.FinanceControllerStatus != DOControlStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Finance stage has already been decided.");
            }

            if (process.CustomerId.HasValue)
            {
                var debts = await LoadCustomerDebtsAsync(
                    process.CustomerId.Value,
                    cancellationToken);

                if (debts.Count > 0 &&
                    !process.SettlementExceptionApprovedAt.HasValue)
                {
                    throw new InvalidOperationException(
                        "Settlement exception must be approved before Finance approval.");
                }
            }
        }

        public async Task CreateCustomerDebtSnapshotsAsync(Guid doProcessId, Guid userId, CancellationToken cancellationToken)
        {
            var process = await _context.DOProcesses.FindAsync(doProcessId);
            var customerId = process.CustomerId.Value;

            // ۱. خواندن بدهی‌های زنده از سه منبع
            var currentDebts = await LoadCustomerDebtsAsync(customerId, cancellationToken);

            if (currentDebts.Count == 0)
                return;

            var now = DateTime.Now;

            // ۲. تبدیل به موجودیت DOProcessCustomerDebtSnapshot
            // فیلد اضافی نیست؛ دقیقاً طبق ساختار ۴ موجودیت اصلی
            foreach (var debt in currentDebts)
            {
                var snapshot = new DOProcessCustomerDebtSnapshot
                {
                    DOProcessId = process.Id,
                    CustomerId = customerId,
                    InvoiceId = debt.InvoiceId,
                    DetentionId = debt.DetentionId,
                    OnlineDetentionId = debt.OnlineDetentionId,
                    Amount = debt.Amount,
                    CurrencyId = debt.CurrencyId,
                    IssueDate = debt.IssueDate,
                    Description = debt.Description,
                    SnapshotAt = now,
                    SnapshotByUserId = userId
                };
                _context.Set<DOProcessCustomerDebtSnapshot>().Add(snapshot);
            }
        }
    }
}