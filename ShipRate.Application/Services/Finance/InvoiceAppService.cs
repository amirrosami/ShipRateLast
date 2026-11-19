using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs.Finance;
using ShipRate.Application.DTOs.Reports;
using ShipRate.Application.Interfaces;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Application.Utilities;
using ShipRate.Application.ViewModels;
using ShipRate.Common.Utilities;
using ShipRate.Domain.Entities.BaseInfo.Finance;
using ShipRate.Domain.Entities.BaseInfo.Persons;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Domain.Entities.Settings;
using ShipRate.Infrastructure.Data;
using ShipRate.Infrastructure.Repositories.Queries;
using System.Data;
using System.Globalization;
using System.Text;
using System.Xml;

namespace ShipRate.Application.Services.Finance
{
    public class InvoiceAppService : IInvoiceAppService
    {
        private readonly ApplicationDbContext _db;
        private readonly IRequestContext _requestContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly FinanceQueries financeQueries;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDetentionService _detentionService;
        public InvoiceAppService(ApplicationDbContext db, IRequestContext requestContext, ICurrentUserService currentUserService, IWebHostEnvironment webHostEnvironment, FinanceQueries financeQueries, IDetentionService detentionService)
        {
            _db = db;
            _requestContext = requestContext;
            _currentUserService = currentUserService;
            this.financeQueries = financeQueries;
            _webHostEnvironment = webHostEnvironment;
            _detentionService = detentionService;
        }
        public async Task<InvoiceListPageVm> GetListPageAsync(InvoiceListFilterVm filter, CancellationToken cancellationToken = default)
        {
            filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var invoices = _db.Invoices.AsNoTracking()
                //.Where(x => !x.IsDeleted && x.CompanyId == _requestContext.CompanyId);
                .Where(x => !x.IsDeleted);

            var isAgent = _currentUserService.IsInRole("External Party");
            if (isAgent)
            {
                var agentPersonId = await _currentUserService.GetPersonIdAsync();
                invoices = invoices.Where(x => x.CreateUserId == _currentUserService.UserId && x.PartyId == agentPersonId);
                //این هم میشه گذاشت 
            }
            if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
                invoices = invoices.Where(x => x.InvoiceNo.Contains(filter.InvoiceNo));

            if (filter.InvoiceTypeId.HasValue)
                invoices = invoices.Where(x => x.InvoiceTypeId == filter.InvoiceTypeId.Value);

            if (filter.JobId.HasValue)
                invoices = invoices.Where(x => x.JobId == filter.JobId.Value);

            if (filter.InvoiceStatusId.HasValue)
                invoices = invoices.Where(x => x.InvoiceStatusId == filter.InvoiceStatusId.Value);

            if (filter.FinanceDirectionId.HasValue)
                invoices = invoices.Where(x => x.FinanceDirectionId == filter.FinanceDirectionId.Value);

            if (filter.CurrencyId.HasValue)
                invoices = invoices.Where(x => x.CurrencyId == filter.CurrencyId.Value);

            if (filter.InvoiceDateFrom.HasValue)
                invoices = invoices.Where(x => x.InvoiceDate >= filter.InvoiceDateFrom.Value);

            if (filter.InvoiceDateTo.HasValue)
            {
                var toDate = filter.InvoiceDateTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.InvoiceDate < toDate);
            }

            var query =
                from i in invoices
                join p in _db.Persons.AsNoTracking().Where(x => !x.IsDeleted) on i.PartyId equals p.Id into partyJoin
                from p in partyJoin.DefaultIfEmpty()
                join t in _db.InvoiceTypes.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceTypeId equals t.Id
                join fnf in _db.InvoiceFNFs.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceFNFId equals fnf.Id
                join imodel in _db.InvoiceModels.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceModelId equals imodel.Id
                join s in _db.InvoiceStatuses.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceStatusId equals s.Id
                join subjects in _db.InvoiceSubjects.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceSubjectId equals subjects.Id
                join f in _db.FinanceDirections.AsNoTracking().Where(x => !x.IsDeleted) on i.FinanceDirectionId equals f.Id
                join c in _db.Currencies.AsNoTracking().Where(x => !x.IsDeleted) on i.CurrencyId equals c.Id
                join u in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.CreateUserId equals u.Id
                join uu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.EditUserId equals uu.Id into updateUser
                from uu in updateUser.DefaultIfEmpty()
                select new InvoiceListItemVm
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    PartyName = p != null ? p.PersonName : "",
                    InvoiceTypeName = t.InvoiceTypeName,
                    InvoiceStatusName = s.InvoiceStatusName,
                    FinanceDirectionName = f.InvoiceContextName,
                    CurrencyCode = c.CurrencyCode,
                    TotalAmount = i.TotalAmount,
                    FNFName = i.InvoiceFNF.FNFName,
                    CreateDateTime = i.CreatedAt,
                    CreateUserName = u.UserName,
                    UpdateDateTime = i.UpdatedAt,
                    UpdateUserName = uu.UserName,
                    InvoiceSubjectId = i.InvoiceSubjectId,
                    InvoiceSubjectName = subjects.InvoiceSubjectName,
                    InvoiceSubjectTitle = i.InvoiceSubjectTitle,
                    InvoiceModelId = i.InvoiceModelId,
                    InvoiceModelName = i.InvoiceModel.InvoiceModelName,
                    TotalPaid = i.SettledAmount,
                    RemainPayment = i.TotalAmount - i.SettledAmount,
                    PartyId = i.PartyId.Value,
                    SettledAmount = i.SettledAmount,
                    RemainingAmount = i.RemainingAmount,
                    IsFullySettled = i.IsFullySettled,
                    BLNo = i.BL.BLNo,
                    JobFileNo = i.Job.JobFileNumber,
                    BookingNo = i.Booking.BookingNo,
                    Serial = i.Serial,
                    SerialPrefix = i.SerialPrefix,
                    CompanyName = i.Company.CompanyAbb,
                    DirectionId = i.FinanceDirectionId,
                    ExchangeRate = i.ExchangeRate.Value
                };
            if (!string.IsNullOrWhiteSpace(filter.PartyName))
                query = query.Where(x => x.PartyName.Contains(filter.PartyName));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.InvoiceNo)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);
            return new InvoiceListPageVm
            {
                Filter = filter,
                Result = new PagedResultVm<InvoiceListItemVm>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                },
                InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken),
                InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken),
                FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken),
                Currencies = await GetCurrenciesAsync(cancellationToken)
            };
        }

        public async Task<InvoiceEditVm> GetCreateVmAsync(Guid? jobId, Guid? blId, Guid? bookingId, CancellationToken cancellationToken = default, string FilterType = "All")
        {
            var vm = new InvoiceEditVm
            {
                InvoiceDate = DateTime.Today,
                InvoicePDate = DateTime.Today,
                DueDate = DateTime.Today,
                ExchangeRate = 1,
                Remarks = string.Empty,
                Notes = string.Empty,
                InvoicePNo = string.Empty,
                InvoiceNo = string.Empty,
                FirstTotal = 0,
                Discount = 0,
                SubTotal = 0,
                TaxAmount = 0,
                TotalAmount = 0,
                JobId = jobId,
                BLId = blId,
                BookingId = bookingId
            };
            switch (FilterType)
            {
                case "BuyF":
                    vm.FNFId = 1;
                    vm.FinanceDirectionId = 2;
                    vm.InvoiceModelId = 1;
                    break;
                case "BuyNF":
                    vm.FNFId = 2;
                    vm.FinanceDirectionId = 2;
                    vm.InvoiceModelId = 1;
                    break;
                case "SellF":
                    vm.FNFId = 1;
                    vm.FinanceDirectionId = 1;
                    vm.InvoiceModelId = 1;
                    break;
                case "SellNF":
                    vm.FNFId = 2;
                    vm.FinanceDirectionId = 1;
                    vm.InvoiceModelId = 1;
                    break;
                case "PreInvoice":
                    vm.FNFId = 2;
                    vm.FinanceDirectionId = 1;
                    vm.InvoiceModelId = 2;
                    break;
                default:
                    break;
            }
            await FillHeaderLookupsAsync(vm, cancellationToken);
            return vm;
        }

        public async Task<InvoiceEditVm?> GetEditVmAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices.AsNoTracking()
                //.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.CompanyId == _requestContext.CompanyId, cancellationToken);
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                return null;
            string partyName = string.Empty;
            if (invoice.PartyId.HasValue)
            {
                partyName = await _db.Persons.AsNoTracking()
                    .Where(x => x.Id == invoice.PartyId.Value && !x.IsDeleted)
                    .Select(x => x.NationalID != null
                        ? "(" + x.NationalID + ")-" + x.PersonName + "-" + x.PersonPersianName
                        : x.PersonName)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            }

            var vm = new InvoiceEditVm
            {
                Id = invoice.Id,
                CompanyId = invoice.CompanyId,
                BranchId = invoice.BranchId,
                FinanceDirectionId = invoice.FinanceDirectionId,
                InvoiceTypeId = invoice.InvoiceTypeId,
                InvoiceStatusId = invoice.InvoiceStatusId,
                InvoiceNo = invoice.InvoiceNo,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceSubjectId = invoice.InvoiceSubjectId,
                DueDate = invoice.DueDate,
                JobId = invoice.JobId,
                BLId = invoice.BLId,
                ParentInvoiceId = invoice.ParentInvoiceId,
                DetentionId = invoice.DetentionId,
                PartyId = invoice.PartyId,
                PartyName = partyName,
                CurrencyId = invoice.CurrencyId,
                ExchangeRate = invoice.ExchangeRate,
                Remarks = invoice.Remarks,
                Notes = invoice.Notes,
                FirstTotal = invoice.FirstTotal,
                Discount = invoice.Discount,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                FNFId = invoice.InvoiceFNFId,
                DefaultTaxPercent = invoice.DefaultTaxPercent,
                InvoiceModelId = invoice.InvoiceModelId,
                InvoiceSubjectTitle = invoice.InvoiceSubjectTitle,
                IsReturn = invoice.IsReturn.Value,
                SettledAmount = invoice.SettledAmount
            };
            await FillHeaderLookupsAsync(vm, cancellationToken);
            return vm;
        }

        public async Task<Guid> SaveHeaderAsync(InvoiceEditVm vm, CancellationToken cancellationToken = default)
        {
            if (vm.Id.HasValue)
            {
                var invoice = await _db.Invoices
                    //.FirstOrDefaultAsync(x => x.Id == vm.Id.Value && !x.IsDeleted && x.CompanyId == _requestContext.CompanyId, cancellationToken);
                    .FirstOrDefaultAsync(x => x.Id == vm.Id.Value && !x.IsDeleted, cancellationToken);

                if (invoice == null)
                    throw new InvalidOperationException("Invoice not found.");

                invoice.FinanceDirectionId = vm.FinanceDirectionId;
                invoice.InvoiceTypeId = vm.FinanceDirectionId;
                invoice.InvoiceNo = vm.InvoiceNo.Trim();
                invoice.InvoiceModelId = vm.InvoiceModelId;
                invoice.InvoiceDate = vm.InvoiceDate;
                invoice.DueDate = vm.DueDate;
                invoice.PartyId = vm.PartyId;
                invoice.CurrencyId = vm.CurrencyId;
                invoice.ExchangeRate = vm.ExchangeRate;
                invoice.Remarks = vm.Remarks.Trim();
                invoice.Notes = vm.Notes?.Trim() ?? string.Empty;
                invoice.InvoiceFNFId = vm.FNFId;
                invoice.CompanyId = vm.CompanyId.Value;
                invoice.BranchId = vm.BranchId.Value;
                invoice.IsReturn = vm.IsReturn;
                invoice.UpdatedAt = DateTime.Now;
                invoice.EditUserId = _requestContext.UserId;
                invoice.DefaultTaxPercent = vm.DefaultTaxPercent;
                invoice.InvoiceModelId = vm.InvoiceModelId;
                invoice.InvoiceSubjectId = vm.InvoiceSubjectId;
                invoice.InvoiceSubjectTitle = vm.InvoiceSubjectTitle;
                invoice.BLId = vm.BLId;
                invoice.BookingId = vm.BookingId;
                await RecalculateInvoiceTotalsInternalAsync(invoice.Id, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await RecalculateInvoiceTotalsInternalAsync(invoice.Id, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return invoice.Id;
            }
            else
            {

                await SetInvoiceSerialAsync(vm);
                var invoice = new Invoice
                {
                    Id = Guid.CreateVersion7(),
                    BranchId = vm.BranchId,
                    InvoiceFNFId = vm.FNFId,
                    CompanyId = vm.CompanyId.Value,
                    FinanceDirectionId = vm.FinanceDirectionId,
                    InvoiceTypeId = vm.FinanceDirectionId,
                    InvoiceNo = vm.InvoiceNo.Trim(),
                    InvoiceDate = vm.InvoiceDate,
                    DueDate = vm.DueDate,
                    JobId = vm.JobId,
                    BLId = vm.BLId,
                    BookingId = vm.BookingId,
                    ParentInvoiceId = vm.ParentInvoiceId,
                    DetentionId = vm.DetentionId,
                    PartyId = vm.PartyId,
                    CurrencyId = vm.CurrencyId,
                    InvoiceModelId = vm.InvoiceModelId,
                    ExchangeRate = vm.ExchangeRate,
                    FirstTotal = 0,
                    Discount = 0,
                    SubTotal = 0,
                    TaxAmount = 0,
                    TotalAmount = 0,
                    Remarks = vm.Remarks?.Trim() ?? string.Empty,
                    Notes = vm.Notes?.Trim() ?? string.Empty,
                    InvoiceStatusId = -1,
                    ApprovedById = null,
                    ApprovedDate = null,
                    IsPostedToAcc = false,
                    //IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null,
                    CreateUserId = _requestContext.UserId,
                    EditUserId = null,
                    InvoiceSubjectId = vm.InvoiceSubjectId,
                    InvoiceSubjectTitle = vm.InvoiceSubjectTitle,
                    DefaultTaxPercent = vm.DefaultTaxPercent,
                    Serial = vm.Serial,
                    SerialPrefix = vm.SerialPrefix,
                    IsReturn = vm.IsReturn
                };
                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync(cancellationToken);
                return invoice.Id;
            }
        }
        public async Task<int> SaveInvoiceDOAsync(InvoiceEditVm vm, CancellationToken cancellationToken = default)
        {
            if (vm.Id is null || vm.Id == Guid.Empty)
            {
                vm.Id = Guid.CreateVersion7();
                vm.Id2 = Guid.CreateVersion7();
                vm.Id3 = Guid.CreateVersion7();
            }
            var parameters = new[]
                {
                    new SqlParameter("@BLId", SqlDbType.UniqueIdentifier) { Value = vm.BLId },
                    new SqlParameter("@InvoiceId", SqlDbType.UniqueIdentifier) { Value = vm.Id },
                    new SqlParameter("@InvoiceId2", SqlDbType.UniqueIdentifier) { Value = vm.Id2 },
                    new SqlParameter("@InvoiceId3", SqlDbType.UniqueIdentifier) { Value = vm.Id3 },
                    new SqlParameter("@CompanyId", SqlDbType.Int) { Value = vm.CompanyId },
                    new SqlParameter("@JobId", SqlDbType.UniqueIdentifier) { Value = vm.JobId },
                    new SqlParameter("@PartyId", SqlDbType.UniqueIdentifier) { Value = vm.PartyId },
                    new SqlParameter("@CurrencyId", SqlDbType.Int) { Value = 2 },  //Rials
                    new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = _currentUserService.UserId },
                    new SqlParameter("@DefaultTaxPercent", SqlDbType.Int) { Value = vm.DefaultTaxPercent },
                    new SqlParameter("@BranchId", SqlDbType.UniqueIdentifier) { Value = vm.BranchId },
                    new SqlParameter("@ClearanceTypeId", SqlDbType.Int) { Value = vm.ClearanceTypeId },
                    new SqlParameter("@Remarks", SqlDbType.NVarChar) { Value = vm.Remarks},
                };

            var query = "EXEC [dbo].[sp_IssueDoInvocie] @BLId, @InvoiceId, @CompanyId, @JobId, @PartyId, @CurrencyId, @UserId, @DefaultTaxPercent, @BranchId, @ClearanceTypeId,@Remarks,@InvoiceId2,@InvoiceId3";

            switch (vm.CalculateDOInvoiceMode)
            {
                case 1:
                    query = "EXEC [dbo].[sp_IssueDoInvocie_From_NOA] @BLId, @InvoiceId, @CompanyId, @JobId, @PartyId, @CurrencyId, @UserId, @DefaultTaxPercent, @BranchId, @ClearanceTypeId,@Remarks,@InvoiceId2,@InvoiceId3";
                    break;
                case 2:
                    query = "EXEC [dbo].[sp_IssueDoInvocie] @BLId, @InvoiceId, @CompanyId, @JobId, @PartyId, @CurrencyId, @UserId, @DefaultTaxPercent, @BranchId, @ClearanceTypeId,@Remarks,@InvoiceId2,@InvoiceId3";
                    break;
                default:
                    break;
            }
            var rowsAffected = await _db.Database.ExecuteSqlRawAsync(query, parameters);
            return rowsAffected;
        }

        public async Task<(bool canBeDeleted, string message)> InvoiceCanBeDeleted(Guid id, CancellationToken cancellationToken = default)
        {
            var items = await _db.InvoiceItems.Where(x => x.InvoiceId == id && !x.IsDeleted).ToListAsync();
            if (items.Any())
            {
                return (false, "Invoice has some items and can not be deleted");
            }
            var settleitems = await _db.SettlementItems.Where(x => x.InvoiceId == id && !x.IsDeleted).ToListAsync();
            if (settleitems.Any())
            {
                return (false, "Invoice has some settlements and can not be deleted");
            }
            return (true, string.Empty);
        }


        public async Task<bool> SoftDeleteInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                return false;
            var allowStatus = await InvoiceCanBeDeleted(invoice.Id, cancellationToken);
            if (!allowStatus.canBeDeleted) return false;

            invoice.IsDeleted = true;
            //invoice.IsActive = false;
            invoice.DeletedAt = DateTime.Now;
            invoice.DeleteUserId = _requestContext.UserId;

            var items = await _db.InvoiceItems
                .Where(x => x.InvoiceId == id && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.IsDeleted = true;
                //item.IsActive = false;
                invoice.DeletedAt = DateTime.Now;
                invoice.DeleteUserId = _requestContext.UserId;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<InvoiceItemRowVm>> GetItemsAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var items =
                from ii in _db.InvoiceItems.AsNoTracking()
                where ii.InvoiceId == invoiceId && !ii.IsDeleted
                join ct in _db.ChargeTypes.AsNoTracking().Where(x => !x.IsDeleted) on ii.ChargeTypeId equals ct.Id
                join c in _db.Currencies.AsNoTracking().Where(x => !x.IsDeleted) on ii.CurrencyId equals c.Id
                join ch in _db.Charges.AsNoTracking().Where(x => !x.IsDeleted) on ii.ChargeId equals ch.Id into chargeJoin
                from ch in chargeJoin.DefaultIfEmpty()
                join u in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on ii.CreateUserId equals u.Id
                join uu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on ii.EditUserId equals uu.Id into UpdateUserJoin
                from uu in UpdateUserJoin.DefaultIfEmpty()
                orderby ii.SequenceNumber

                select new InvoiceItemRowVm
                {
                    Id = ii.Id,
                    InvoiceId = ii.InvoiceId,
                    SequenceNumber = ii.SequenceNumber,
                    ChargeTypeName = ct.ChargeTypeName,
                    ChargeName = ch != null ? ch.ChargeDescription : "",
                    Description = ii.Description,
                    QTY = ii.QTY,
                    UnitPrice = ii.UnitPrice,
                    Discount = ii.Discount,
                    TaxPercent = ii.TaxPercent,
                    CurrencyCode = c.CurrencyCode,
                    AmountBeforeDiscount = ii.AmountBeforeDiscount,
                    AmountAfterDiscount = ii.AmountAfterDiscount,
                    TaxAmount = ii.TaxAmount,
                    TotalAmount = ii.TotalAmount,
                    CreateDateTime = ii.CreatedAt,
                    CreateUserName = u.UserName,
                    UpdateDateTime = ii.UpdatedAt,
                    UpdateUserName = uu.UserName
                };

            return await items.ToListAsync(cancellationToken);
        }

        public async Task<InvoiceItemEditVm> GetNewItemVmAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found.");

            var nextSequence = await _db.InvoiceItems.AsNoTracking()
                .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
                .Select(x => (int?)x.SequenceNumber)
                .MaxAsync(cancellationToken) ?? 0;

            var vm = new InvoiceItemEditVm
            {
                InvoiceId = invoiceId,
                SequenceNumber = nextSequence + 1,
                CurrencyId = invoice.CurrencyId,
                QTY = 1,
                UnitPrice = 0,
                Discount = 0,
                TaxPercent = invoice.DefaultTaxPercent ?? 0,
                Description = "-",
                ChargeTypeSupplementRemarks = string.Empty
            };
            CalculateItem(vm);
            await FillItemLookupsAsync(vm, invoice.FinanceDirectionId, cancellationToken);
            return vm;
        }

        public async Task<InvoiceItemEditVm?> GetEditItemVmAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = await _db.InvoiceItems.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (item == null)
                return null;

            var invoice = await _db.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == item.InvoiceId && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                return null;

            var vm = new InvoiceItemEditVm
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                SequenceNumber = item.SequenceNumber,
                ChargeTypeId = item.ChargeTypeId,
                ChargeId = item.ChargeId,
                Description = item.Description,
                QTY = item.QTY,
                UnitPrice = item.UnitPrice,
                Discount = item.Discount,
                TaxPercent = item.TaxPercent,
                CurrencyId = item.CurrencyId,
                ChargeTypeSupplementRemarks = item.ChargeTypeSupplementRemarks,
                AmountBeforeDiscount = item.AmountBeforeDiscount,
                AmountAfterDiscount = item.AmountAfterDiscount,
                TaxAmount = item.TaxAmount,
                TotalAmount = item.TotalAmount
            };

            await FillItemLookupsAsync(vm, invoice.FinanceDirectionId, cancellationToken);
            return vm;
        }

        public async Task<Guid> SaveItemAsync(InvoiceItemEditVm vm, CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(x => x.Id == vm.InvoiceId && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found.");

            CalculateItem(vm);

            if (vm.Id.HasValue)
            {
                var item = await _db.InvoiceItems
                    .FirstOrDefaultAsync(x => x.Id == vm.Id.Value && !x.IsDeleted, cancellationToken);

                if (item == null)
                    throw new InvalidOperationException("Invoice item not found.");

                item.ChargeTypeId = vm.ChargeTypeId;
                item.ChargeId = vm.ChargeId;
                item.Description = vm.Description.Trim();
                item.QTY = vm.QTY;
                item.UnitPrice = vm.UnitPrice;
                item.AmountBeforeDiscount = vm.AmountBeforeDiscount;
                item.Discount = vm.Discount;
                item.AmountAfterDiscount = vm.AmountAfterDiscount;
                item.TaxPercent = vm.TaxPercent;
                item.TaxAmount = vm.TaxAmount;
                item.TotalAmount = vm.TotalAmount;
                item.CurrencyId = invoice.CurrencyId;
                item.ChargeTypeSupplementRemarks = vm.ChargeTypeSupplementRemarks.Trim();
                item.UpdatedAt = DateTime.Now;
                item.EditUserId = _requestContext.UserId;
            }
            else
            {
                var item = new InvoiceItem
                {
                    Id = Guid.CreateVersion7(),
                    InvoiceId = vm.InvoiceId,
                    SequenceNumber = vm.SequenceNumber > 0 ? vm.SequenceNumber : await GetNextSequenceNumberAsync(vm.InvoiceId, cancellationToken),
                    ChargeTypeId = vm.ChargeTypeId,
                    ChargeId = vm.ChargeId,
                    Description = vm.Description.Trim(),
                    QTY = vm.QTY,
                    UnitPrice = vm.UnitPrice,
                    AmountBeforeDiscount = vm.AmountBeforeDiscount,
                    Discount = vm.Discount,
                    AmountAfterDiscount = vm.AmountAfterDiscount,
                    TaxPercent = vm.TaxPercent,
                    TaxAmount = vm.TaxAmount,
                    TotalAmount = vm.TotalAmount,
                    CurrencyId = invoice.CurrencyId,
                    ChargeTypeSupplementRemarks = vm.ChargeTypeSupplementRemarks.Trim(),
                    //IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null,
                    CreateUserId = _requestContext.UserId,
                    EditUserId = null

                };

                _db.InvoiceItems.Add(item);
                vm.Id = item.Id;
            }

            await RecalculateInvoiceTotalsInternalAsync(vm.InvoiceId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateInvoiceTotalsInternalAsync(vm.InvoiceId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return vm.Id!.Value;
        }

        public async Task SoftDeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var item = await _db.InvoiceItems
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (item == null)
                return;

            var invoice = await _db.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == item.InvoiceId && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                return;

            item.IsDeleted = true;
            //item.IsActive = false;
            item.UpdatedAt = DateTime.Now;
            item.EditUserId = _requestContext.UserId;

            await RecalculateInvoiceTotalsInternalAsync(item.InvoiceId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await RecalculateInvoiceTotalsInternalAsync(item.InvoiceId, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<InvoiceTotalsVm> GetTotalsAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _db.Invoices.AsNoTracking()
                //.FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted && x.CompanyId == _requestContext.CompanyId, cancellationToken);
                .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken);


            if (invoice == null)
                throw new InvalidOperationException("Invoice not found.");

            return new InvoiceTotalsVm
            {
                InvoiceId = invoice.Id,
                FirstTotal = invoice.FirstTotal,
                Discount = invoice.Discount,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount
            };
        }

        public async Task<PagedResultVm<PartyLookupRowVm>> GetPartyLookupAsync(PartyLookupFilterVm filter, Guid? jobId, CancellationToken cancellationToken = default)
        {
            filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;





            var query = _db.Persons.AsNoTracking()
                .Where(x => !x.IsDeleted);


            // فیلتر بر اساس JobId در صورت وجود مقدار
            if (jobId.HasValue && jobId.Value != Guid.Empty)
            {
                query = query.Where(p => _db.JobParties.Any(jp =>
                    !jp.IsDeleted &&
                    jp.JobId == jobId.Value &&
                    jp.PartyId == p.Id));
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(x => x.PersonName.Contains(filter.Search));

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.PersonName)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new PartyLookupRowVm
                {
                    Id = x.Id,
                    PersonName = x.NationalID != null ? "(" + x.NationalID + ")-" + x.PersonName + "-" + x.PersonPersianName : x.PersonName
                })
                .ToListAsync(cancellationToken);

            return new PagedResultVm<PartyLookupRowVm>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<List<LookupItemVm>> GetChargesForItemAsync(int? currencyId, int? chargeTypeId, int? financeDirectionId, CancellationToken cancellationToken = default)
        {
            var query = _db.Charges.AsNoTracking()
                //.Where(x => !x.IsDeleted && x.CompanyId == _requestContext.CompanyId);
                .Where(x => !x.IsDeleted);


            if (currencyId.HasValue)
                query = query.Where(x => x.CurrencyId == currencyId.Value);

            if (chargeTypeId.HasValue)
                query = query.Where(x => x.ChargeTypeId == chargeTypeId.Value);

            if (financeDirectionId.HasValue)
                query = query.Where(x => x.DirectionId == financeDirectionId.Value);

            return await query
                .OrderBy(x => x.ChargeDescription)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.ChargeDescription
                })
                .ToListAsync(cancellationToken);
        }
        private async Task FillHeaderLookupsAsync(InvoiceEditVm vm, CancellationToken cancellationToken)
        {
            vm.InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken);
            vm.InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken);
            vm.FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken);
            vm.Currencies = await GetCurrenciesAsync(cancellationToken);
            vm.FNFS = await GetFNFSAsync(cancellationToken);
            vm.Companies = await GetCompaniesAsync(cancellationToken);
            vm.InvoiceSubjects = await GetInvoiceSubjectsAsync(cancellationToken);
            vm.InvoiceModels = await GetInvoiceModelsAsync(cancellationToken);
            vm.Branches = await GetBranchesAsync(cancellationToken);
            vm.ClearanceTypes = await GetClearanceTypesAsync(cancellationToken);
            if (vm.JobId != null && vm.JobId.HasValue && vm.JobId != Guid.Empty)
            {
                vm.BLs = await GetBLsAsync(vm.JobId.Value, cancellationToken);
                vm.Bookings = await GetBookingsAsync(vm.JobId.Value, cancellationToken);

                //if (vm.BLId != null && vm.BLId.HasValue && vm.BLId != Guid.Empty)
                //{
                //    vm.BLId=
                //}
            }
        }
        private async Task FillItemLookupsAsync(InvoiceItemEditVm vm, int financeDirectionId, CancellationToken cancellationToken)
        {
            vm.ChargeTypes = await _db.ChargeTypes.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.ChargeTypeName)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.ChargeTypeName}-{x.ChargeTypeLocalName}"
                })
                .ToListAsync(cancellationToken);

            vm.Currencies = await GetCurrenciesAsync(cancellationToken);

            vm.Charges = await GetChargesForItemAsync(
                vm.CurrencyId > 0 ? vm.CurrencyId : null,
                vm.ChargeTypeId > 0 ? vm.ChargeTypeId : null,
                financeDirectionId,
                cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetInvoiceTypesAsync(CancellationToken cancellationToken)
        {
            return await _db.InvoiceTypes.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.InvoiceTypeName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetInvoiceStatusesAsync(CancellationToken cancellationToken)
        {
            return await _db.InvoiceStatuses.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.InvoiceStatusName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetFinanceDirectionsAsync(CancellationToken cancellationToken)
        {
            return await _db.FinanceDirections.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted && x.Id != 3)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.InvoiceContextName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetCurrenciesAsync(CancellationToken cancellationToken)
        {
            return await _db.Currencies.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .ThenBy(x => x.CurrencyCode)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.CurrencyCode + " - " + x.CurrencyName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetCompaniesAsync(CancellationToken cancellationToken)
        {
            return await _db.Companies
                .Include(x => x.Person)
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.Person.PersonName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetInvoiceSubjectsAsync(CancellationToken cancellationToken)
        {
            return await _db.InvoiceSubjects
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.InvoiceSubjectName + " - " + x.InvoiceSubjectLocalName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetInvoiceModelsAsync(CancellationToken cancellationToken)
        {
            return await _db.InvoiceModels
                .AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.InvoiceModelName + " - " + x.InvoiceModelLocalName
                })
                .ToListAsync(cancellationToken);
        }


        private async Task<List<LookupItemVm>> GetClearanceTypesAsync(CancellationToken cancellationToken)
        {
            return await _db.ClearanceTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.ClearanceTypeName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetChargeTypesAsync(CancellationToken cancellationToken)
        {
            return await _db.ChargeTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.ChargeTypeName)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.ChargeTypeName
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetBranchesAsync(CancellationToken cancellationToken)
        {
            return await _db.Branches
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.BranchName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetBLsAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return await _db.BLs
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.JobId == jobId)
                .OrderBy(x => x.BLNo)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.BLNo
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<List<LookupItemVm>> GetBookingsAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return await _db.Bookings
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.JobId == jobId)
                .OrderBy(x => x.BookingNo)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.BookingNo
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<LookupItemVm>> GetFNFSAsync(CancellationToken cancellationToken)
        {
            return await _db.InvoiceFNFs.AsNoTracking()
                .Where(x => x.IsActive && !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new LookupItemVm
                {
                    Value = x.Id.ToString(),
                    Text = x.FNFName
                })
                .ToListAsync(cancellationToken);
        }
        private async Task<int> GetNextSequenceNumberAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            var max = await _db.InvoiceItems
                .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
                .Select(x => (int?)x.SequenceNumber)
                .MaxAsync(cancellationToken);

            return (max ?? 0) + 1;
        }

        private static void CalculateItem(InvoiceItemEditVm vm)
        {
            vm.AmountBeforeDiscount = Math.Round(vm.QTY * vm.UnitPrice, 2, MidpointRounding.AwayFromZero);
            vm.AmountAfterDiscount = Math.Round(vm.AmountBeforeDiscount - vm.Discount, 2, MidpointRounding.AwayFromZero);

            if (vm.AmountAfterDiscount < 0)
                vm.AmountAfterDiscount = 0;

            vm.TaxAmount = Math.Round(vm.AmountAfterDiscount * vm.TaxPercent / 100m, 2, MidpointRounding.AwayFromZero);
            vm.TotalAmount = Math.Round(vm.AmountAfterDiscount + vm.TaxAmount, 2, MidpointRounding.AwayFromZero);
        }

        private async Task RecalculateInvoiceTotalsInternalAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            var invoice = await _db.Invoices
                .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, cancellationToken);

            if (invoice == null)
                throw new InvalidOperationException("Invoice not found.");

            var items = await _db.InvoiceItems
                .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            invoice.FirstTotal = items.Sum(x => x.AmountBeforeDiscount);
            invoice.Discount = items.Sum(x => x.Discount);
            invoice.SubTotal = items.Sum(x => x.AmountAfterDiscount);
            invoice.TaxAmount = items.Sum(x => x.TaxAmount);
            invoice.TotalAmount = items.Sum(x => x.TotalAmount);
            invoice.UpdatedAt = DateTime.Now;
            invoice.EditUserId = _requestContext.UserId;
        }
        public async Task<string> GetInvoiceNoAsync(Guid invoiceId)
        {
            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice == null) return "";
            return invoice.InvoiceNo;
        }
        public async Task<int> GetInvoiceCurrencyIdAsync(Guid invoiceId)
        {
            var inv = await _db.Invoices.FindAsync(invoiceId);
            return inv.CurrencyId;
        }

        public async Task<ServiceResult<CreateOnlinePaymentVm>> GetNewOnlinePayment(List<Guid> invoiceIds)
        {
            var currentPerson = await _currentUserService.GetPersonIdAsync();
            var invoices = await _db.Invoices.Where(x => invoiceIds.Contains(x.Id) && x.PartyId == currentPerson).ToListAsync();
            if (invoiceIds.Count != invoices.Count)
            {
                return ServiceResult<CreateOnlinePaymentVm>.Failed("invoice Ids are invalid");
            }

            // check remain payment for each invoice:

            var remainPaymentInvoices = await financeQueries.GetRemainInvoicePayment()
                .Where(x => invoiceIds.Contains(x.InvoiceId))
                .ToListAsync();

            if (remainPaymentInvoices.Any(x => x.RemainPayment <= 0))
            {
                return ServiceResult<CreateOnlinePaymentVm>.Failed("Some Invoices paid fully before !!");
            }

            var items = from i in invoices
                        join remainPayment in remainPaymentInvoices
                 on i.Id equals remainPayment.InvoiceId
                        select new CreateOnlinePaymentItemVm
                        {
                            InvoiceId = i.Id,
                            InvoiceNo = i.InvoiceNo,
                            RemainAmount = (long?)remainPayment.RemainPayment ?? 0,
                            PaymentAmount = (long?)remainPayment.RemainPayment ?? 0,
                            Total = (long)i.TotalAmount
                        };

            var createvm = new CreateOnlinePaymentVm(null, currentPerson.Value, items.ToList());
            return ServiceResult<CreateOnlinePaymentVm>.Success(createvm);
        }

        public async Task<bool> ExistsSaleDoInvoiceForBLAsync(Guid? bLId)
        {
            return await _db.Invoices.Where(x => x.BLId == bLId && x.FinanceDirectionId == 1 && x.InvoiceStatusId != 8 && x.IsDeleted == false && x.InvoiceSubjectId == 4).AnyAsync();
        }

        public async Task<byte[]> GenerateOfficialTaxInvoicePdfAsync(InvoicePrintDto invoiceDto, CancellationToken cancellationToken = default)
        {

            var logoPath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "logos",
                        "logo-1.png");

            byte[] logoBytes = Array.Empty<byte>();

            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(
                    logoPath,
                    cancellationToken);
            }



            using var report = new Report();

            // 1. ساخت DataTable برای هدر فاکتور
            var headerTable = new DataTable("InvoiceHeader");
            headerTable.Columns.Add("InvoiceNo", typeof(string));
            headerTable.Columns.Add("InvoiceDatePersian", typeof(string));
            headerTable.Columns.Add("InvoiceModelName", typeof(string));
            headerTable.Columns.Add("CurrencyName", typeof(string));
            headerTable.Columns.Add("SellerName", typeof(string));
            headerTable.Columns.Add("SellerEconomicCode", typeof(string));
            headerTable.Columns.Add("SellerNationalId", typeof(string));
            headerTable.Columns.Add("SellerAddress", typeof(string));
            headerTable.Columns.Add("SellerPhone", typeof(string));
            headerTable.Columns.Add("BuyerName", typeof(string));
            headerTable.Columns.Add("BuyerEconomicCode", typeof(string));
            headerTable.Columns.Add("BuyerNationalId", typeof(string));
            headerTable.Columns.Add("BuyerAddress", typeof(string));
            headerTable.Columns.Add("BuyerPhone", typeof(string));
            headerTable.Columns.Add("TotalFirst", typeof(decimal));
            headerTable.Columns.Add("TotalDiscount", typeof(decimal));
            headerTable.Columns.Add("TotalTax", typeof(decimal));
            headerTable.Columns.Add("FinalAmount", typeof(decimal));
            headerTable.Columns.Add("CompanyLogo", typeof(byte[]));

            var headerRow = headerTable.NewRow();
            headerRow["InvoiceNo"] = invoiceDto.InvoiceNo ?? "";
            headerRow["InvoiceDatePersian"] = invoiceDto.InvoiceDatePersian ?? "";
            headerRow["InvoiceModelName"] = invoiceDto.InvoiceModelName ?? "";
            headerRow["CurrencyName"] = invoiceDto.CurrencyName;
            headerRow["SellerName"] = invoiceDto.SellerName ?? "";
            headerRow["SellerEconomicCode"] = invoiceDto.SellerEconomicCode ?? "";
            headerRow["SellerNationalId"] = invoiceDto.SellerNationalId ?? "";
            headerRow["SellerAddress"] = invoiceDto.SellerAddress ?? "";
            headerRow["SellerPhone"] = invoiceDto.SellerPhone ?? "";
            headerRow["BuyerName"] = invoiceDto.BuyerName ?? "";
            headerRow["BuyerEconomicCode"] = invoiceDto.BuyerEconomicCode ?? "";
            headerRow["BuyerNationalId"] = invoiceDto.BuyerNationalId ?? "";
            headerRow["BuyerAddress"] = invoiceDto.BuyerAddress ?? "";
            headerRow["BuyerPhone"] = invoiceDto.BuyerPhone ?? "";
            headerRow["TotalFirst"] = invoiceDto.TotalFirst;
            headerRow["TotalDiscount"] = invoiceDto.TotalDiscount;
            headerRow["TotalTax"] = invoiceDto.TotalTax;
            headerRow["FinalAmount"] = invoiceDto.FinalAmount;
            headerRow["CompanyLogo"] = logoBytes;
            headerTable.Rows.Add(headerRow);

            // 2. ساخت DataTable برای اقلام فاکتور (Items)
            var itemsTable = new DataTable("InvoiceItems");
            itemsTable.Columns.Add("Row", typeof(int));
            itemsTable.Columns.Add("ChargeTitle", typeof(string));
            itemsTable.Columns.Add("Qty", typeof(decimal));
            itemsTable.Columns.Add("UnitPrice", typeof(decimal));
            itemsTable.Columns.Add("AmountBeforeDiscount", typeof(decimal));
            itemsTable.Columns.Add("Discount", typeof(decimal));
            itemsTable.Columns.Add("TaxAmount", typeof(decimal));
            itemsTable.Columns.Add("Total", typeof(decimal));

            int index = 1;
            foreach (var item in invoiceDto.Items)
            {
                var row = itemsTable.NewRow();
                row["Row"] = item.Row > 0 ? item.Row : index++;
                row["ChargeTitle"] = item.ChargeTitle ?? "";
                row["Qty"] = item.Qty;
                row["UnitPrice"] = item.UnitPrice;
                row["AmountBeforeDiscount"] = item.AmountBeforeDiscount;
                row["Discount"] = item.Discount;
                row["TaxAmount"] = item.TaxAmount;
                row["Total"] = item.Total;
                itemsTable.Rows.Add(row);
            }

            var dataSet = new DataSet("TaxInvoiceData");
            dataSet.Tables.Add(headerTable);
            dataSet.Tables.Add(itemsTable);

            // 3. ثبت دیتاست در FastReport و فعال‌سازی TableDataSource
            report.RegisterData(dataSet, "TaxInvoiceData");
            report.GetDataSource("InvoiceHeader").Enabled = true;
            report.GetDataSource("InvoiceItems").Enabled = true;

            // 4. بارگذاری تمپلیت فایل FastReport (.frx) یا طراحی آن
            string reportPath = "";
            switch (invoiceDto.InvoiceModelId)
            {
                case 1:
                    switch (invoiceDto.FNFId)
                    {
                        case 1:
                            reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "OfficialTaxInvoice.frx");
                            break;
                        case 2:
                            reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "NFInvoice.frx");
                            break;
                    }
                    break;
                case 2:
                    reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "PreInvoice.frx");
                    break;
            }
            if (File.Exists(reportPath))
            {
                report.Load(reportPath);
            }
            else
            {
                // ساخت خودکار تمپلیت در حافظه در صورتی که فایل frx هنوز تعریف نشده باشد
                BuildDynamicReportTemplate(report);
            }
            report.Prepare();
            using var pdfExport = new PDFSimpleExport();
            using var stream = new MemoryStream();
            pdfExport.Export(report, stream);

            return stream.ToArray();
        }

        public async Task<byte[]> GenerateBuyRequestPdfAsync(InvoicePrintDto invoiceDto, CancellationToken cancellationToken = default)
        {

            var logoPath = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "logos",
                        "logo-1.png");

            byte[] logoBytes = Array.Empty<byte>();

            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(
                    logoPath,
                    cancellationToken);
            }



            using var report = new Report();

            // 1. ساخت DataTable برای هدر فاکتور
            var headerTable = new DataTable("InvoiceHeader");
            headerTable.Columns.Add("InvoiceNo", typeof(string));
            headerTable.Columns.Add("InvoiceDatePersian", typeof(string));
            headerTable.Columns.Add("InvoiceModelName", typeof(string));
            headerTable.Columns.Add("CurrencyName", typeof(string));
            headerTable.Columns.Add("SellerName", typeof(string));
            headerTable.Columns.Add("SellerEconomicCode", typeof(string));
            headerTable.Columns.Add("SellerNationalId", typeof(string));
            headerTable.Columns.Add("SellerAddress", typeof(string));
            headerTable.Columns.Add("SellerPhone", typeof(string));
            headerTable.Columns.Add("BuyerName", typeof(string));
            headerTable.Columns.Add("BuyerEconomicCode", typeof(string));
            headerTable.Columns.Add("BuyerNationalId", typeof(string));
            headerTable.Columns.Add("BuyerAddress", typeof(string));
            headerTable.Columns.Add("BuyerPhone", typeof(string));
            headerTable.Columns.Add("TotalFirst", typeof(decimal));
            headerTable.Columns.Add("TotalDiscount", typeof(decimal));
            headerTable.Columns.Add("TotalTax", typeof(decimal));
            headerTable.Columns.Add("FinalAmount", typeof(decimal));
            headerTable.Columns.Add("CompanyLogo", typeof(byte[]));

            var headerRow = headerTable.NewRow();
            headerRow["InvoiceNo"] = invoiceDto.InvoiceNo ?? "";
            headerRow["InvoiceDatePersian"] = invoiceDto.InvoiceDatePersian ?? "";
            headerRow["InvoiceModelName"] = invoiceDto.InvoiceModelName ?? "";
            headerRow["CurrencyName"] = invoiceDto.CurrencyName;
            headerRow["SellerName"] = invoiceDto.SellerName ?? "";
            headerRow["SellerEconomicCode"] = invoiceDto.SellerEconomicCode ?? "";
            headerRow["SellerNationalId"] = invoiceDto.SellerNationalId ?? "";
            headerRow["SellerAddress"] = invoiceDto.SellerAddress ?? "";
            headerRow["SellerPhone"] = invoiceDto.SellerPhone ?? "";
            headerRow["BuyerName"] = invoiceDto.BuyerName ?? "";
            headerRow["BuyerEconomicCode"] = invoiceDto.BuyerEconomicCode ?? "";
            headerRow["BuyerNationalId"] = invoiceDto.BuyerNationalId ?? "";
            headerRow["BuyerAddress"] = invoiceDto.BuyerAddress ?? "";
            headerRow["BuyerPhone"] = invoiceDto.BuyerPhone ?? "";
            headerRow["TotalFirst"] = invoiceDto.TotalFirst;
            headerRow["TotalDiscount"] = invoiceDto.TotalDiscount;
            headerRow["TotalTax"] = invoiceDto.TotalTax;
            headerRow["FinalAmount"] = invoiceDto.FinalAmount;
            headerRow["CompanyLogo"] = logoBytes;
            headerTable.Rows.Add(headerRow);

            // 2. ساخت DataTable برای اقلام فاکتور (Items)
            var itemsTable = new DataTable("InvoiceItems");
            itemsTable.Columns.Add("Row", typeof(int));
            itemsTable.Columns.Add("ChargeTitle", typeof(string));
            itemsTable.Columns.Add("Qty", typeof(decimal));
            itemsTable.Columns.Add("UnitPrice", typeof(decimal));
            itemsTable.Columns.Add("AmountBeforeDiscount", typeof(decimal));
            itemsTable.Columns.Add("Discount", typeof(decimal));
            itemsTable.Columns.Add("TaxAmount", typeof(decimal));
            itemsTable.Columns.Add("Total", typeof(decimal));

            int index = 1;
            foreach (var item in invoiceDto.Items)
            {
                var row = itemsTable.NewRow();
                row["Row"] = item.Row > 0 ? item.Row : index++;
                row["ChargeTitle"] = item.ChargeTitle ?? "";
                row["Qty"] = item.Qty;
                row["UnitPrice"] = item.UnitPrice;
                row["AmountBeforeDiscount"] = item.AmountBeforeDiscount;
                row["Discount"] = item.Discount;
                row["TaxAmount"] = item.TaxAmount;
                row["Total"] = item.Total;
                itemsTable.Rows.Add(row);
            }

            var dataSet = new DataSet("TaxInvoiceData");
            dataSet.Tables.Add(headerTable);
            dataSet.Tables.Add(itemsTable);

            // 3. ثبت دیتاست در FastReport و فعال‌سازی TableDataSource
            report.RegisterData(dataSet, "TaxInvoiceData");
            report.GetDataSource("InvoiceHeader").Enabled = true;
            report.GetDataSource("InvoiceItems").Enabled = true;

            // 4. بارگذاری تمپلیت فایل FastReport (.frx) یا طراحی آن
            string reportPath = "";
            //switch (invoiceDto.InvoiceModelId)
            //{
            //    case 1:
            //        switch (invoiceDto.FNFId)
            //        {
            //            case 1:
            //                reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "OfficialTaxInvoice.frx");
            //                break;
            //            case 2:
            //                reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "NFInvoice.frx");
            //                break;
            //        }
            //        break;
            //    case 2:
            //        reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "PreInvoice.frx");
            //        break;
            //}
            reportPath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", "Finance", "BuyRequest.frx");
            if (File.Exists(reportPath))
            {
                report.Load(reportPath);
            }
            else
            {
                // ساخت خودکار تمپلیت در حافظه در صورتی که فایل frx هنوز تعریف نشده باشد
                BuildDynamicReportTemplate(report);
            }
            report.Prepare();
            using var pdfExport = new PDFSimpleExport();
            using var stream = new MemoryStream();
            pdfExport.Export(report, stream);

            return stream.ToArray();
        }

        private void BuildDynamicReportTemplate(Report report)
        {
            var page = new ReportPage();
            page.Name = "Page1";
            page.Landscape = false;
            page.PaperWidth = 210; // A4
            page.PaperHeight = 297;
            page.LeftMargin = 8;
            page.RightMargin = 8;
            page.TopMargin = 8;
            page.BottomMargin = 8;
            report.Pages.Add(page);
            // تمپلیت در لایه frx اختصاصی لود می‌شود
        }

        public async Task<InvoicePrintDto> GetInvoicePrintDtoAsync(Guid invoiceId)
        {
            var invoice = await _db.Invoices
                .Include(x => x.Company)
                    .ThenInclude(x => x.Person)
                .Include(x => x.Party)
                .Include(x => x.InvoiceModel)
                .Include(x => x.Currency)
                .Include(x => x.InvoiceItems).ThenInclude(i => i.ChargeType)
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice == null) return null;

            var dto = new InvoicePrintDto
            {
                FinanceDirectionId = invoice.FinanceDirectionId,
                InvoiceModelId = invoice.InvoiceModelId,
                InvoiceNo = invoice.InvoiceNo ?? "-",
                InvoiceDatePersian = DateHelper.getSolarDate10(invoice.InvoiceDate.Value),
                InvoiceModelName = invoice.InvoiceModel?.InvoiceModelName ?? "",
                CurrencyName = invoice.Currency?.LocalName ?? "",
                TotalFirst = invoice.FirstTotal,
                TotalDiscount = invoice.Discount,
                TotalTax = invoice.TaxAmount,
                FinalAmount = invoice.TotalAmount,
                FNFId = invoice.InvoiceFNFId,
                Items = invoice.InvoiceItems.OrderBy(x => x.SequenceNumber).Select(x => new InvoiceItemPrintDto
                {
                    Row = x.SequenceNumber,
                    ChargeTitle = x.ChargeType?.ChargeTypeLocalName + " " + x.ChargeTypeSupplementRemarks,
                    Qty = x.QTY,
                    UnitPrice = x.UnitPrice,
                    AmountBeforeDiscount = x.AmountBeforeDiscount,
                    Discount = x.Discount,
                    TaxAmount = x.TaxAmount,
                    Total = x.TotalAmount
                }).ToList()
            };

            // منطق Direction: 1 = فروش (Company فروشنده)، 2 = خرید (Company خریدار)
            if (invoice.FinanceDirectionId == 1)
            {
                FillSellerFromCompany(dto, invoice.Company);
                FillBuyerFromParty(dto, invoice.Party);
            }
            else
            {
                FillSellerFromParty(dto, invoice.Party);
                FillBuyerFromCompany(dto, invoice.Company);
            }
            return dto;
        }

        private void FillSellerFromCompany(InvoicePrintDto dto, Company company)
        {
            if (company == null) return;

            dto.SellerName = company.Person.PersonPersianName;
            dto.SellerEconomicCode = company.Person.EcoCode ?? "-";
            dto.SellerNationalId = company.Person.NationalID ?? "-"; // یا هر فیلدی که شناسه ملی است
            dto.SellerAddress = company.Person.PersianAddress ?? "-";
            dto.SellerPhone = company.Person.Phone ?? "-";
        }

        private void FillBuyerFromParty(InvoicePrintDto dto, Person party)
        {
            if (party == null) return;

            dto.BuyerName = party.PersonPersianName;
            dto.BuyerEconomicCode = party.EcoCode ?? "-";
            dto.BuyerNationalId = party.NationalID ?? "-";
            dto.BuyerAddress = party.PersianAddress ?? "-";
            dto.BuyerPhone = party.Phone ?? "-";
        }
        private void FillSellerFromParty(InvoicePrintDto dto, Person party)
        {
            if (party == null) return;

            dto.SellerName = party.PersonPersianName;
            dto.SellerEconomicCode = party.EcoCode ?? "-";
            dto.SellerNationalId = party.NationalID ?? "-";
            dto.SellerAddress = party.Address ?? "-";
            dto.SellerPhone = party.Phone ?? "-";
        }
        private void FillBuyerFromCompany(InvoicePrintDto dto, Company company)
        {
            if (company == null) return;

            dto.BuyerName = company.Person.PersonPersianName;
            dto.BuyerEconomicCode = company.Person.EcoCode ?? "-";
            dto.BuyerNationalId = company.Person.NationalID ?? "-";
            dto.BuyerAddress = company.Person.PersianAddress ?? "-";
            dto.BuyerPhone = company.Person.Phone ?? "-";
        }
        public async Task<int> GetNextSerialAsync(int companyId, string serialPrefix, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serialPrefix))
                throw new ArgumentException("Serial prefix cannot be empty.", nameof(serialPrefix));

            serialPrefix = serialPrefix.Trim().ToUpper();

            // گرفتن ماکسیمم شماره سریال برای پیشوند مشخص شده
            var maxSerial = await _db.Invoices
                .Where(x => x.SerialPrefix == serialPrefix && !x.IsDeleted && x.CompanyId == companyId)
                .MaxAsync(x => (int?)x.Serial, ct) ?? 0;
            return maxSerial + 1;
        }

        public async Task SetInvoiceSerialAsync(InvoiceEditVm vm)
        {
            if (vm.InvoiceModelId == 2)
            {
                vm.SerialPrefix = "P";
                vm.Serial = await GetNextSerialAsync(vm.CompanyId.Value, vm.SerialPrefix, default);
                return;
            }
            switch (vm.FinanceDirectionId)
            {
                case 1: //Sell
                    switch (vm.FNFId)
                    {
                        case 1:
                            vm.SerialPrefix = "A";
                            vm.Serial = await GetNextSerialAsync(vm.CompanyId.Value, vm.SerialPrefix, default);
                            break;
                        case 2:
                            vm.SerialPrefix = "N";
                            vm.Serial = await GetNextSerialAsync(vm.CompanyId.Value, vm.SerialPrefix, default);
                            break;
                        default:
                            break;
                    }
                    break;
                case 2: //Buy
                    vm.SerialPrefix = "Z";
                    vm.Serial = await GetNextSerialAsync(vm.CompanyId.Value, vm.SerialPrefix, default);
                    break;
                default:
                    break;
            }
        }

        public async Task<InvoiceReportDto?> GetInvoiceForReportAsync(Guid invoiceId)
        {
            var invoiceDto = await _db.Invoices
                .AsNoTracking()
                .Where(i => i.Id == invoiceId && !i.IsDeleted)
                .Select(i => new InvoiceReportDto
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceDate = i.InvoiceDate,
                    DueDate = i.DueDate,
                    // ترکیب پیش‌وند و سریال
                    FullSerial = (i.SerialPrefix ?? "") + i.Serial.ToString(),
                    // اطلاعات مشتری از جدول Persons
                    PartyName = _db.Persons
                        .Where(p => p.Id == i.PartyId)
                        .Select(p => p.PersonName) // یا فیلد عنوان شخص در جدول Persons
                        .FirstOrDefault(),
                    // کد ارز از جدول Currency
                    CurrencyCode = _db.Currencies
                        .Where(c => c.Id == i.CurrencyId)
                        .Select(c => c.CurrencyCode)
                        .FirstOrDefault(),
                    FirstTotal = i.FirstTotal,
                    Discount = i.Discount,
                    SubTotal = i.SubTotal,
                    TaxAmount = i.TaxAmount,
                    TotalAmount = i.TotalAmount,
                    Remarks = i.Remarks ?? string.Empty,
                    Notes = i.Notes ?? string.Empty,

                    // اقلام فاکتور (مرتب شده بر اساس SequenceNumber)
                    InvoiceItems = _db.InvoiceItems
                        .AsNoTracking()
                        .Where(it => it.InvoiceId == i.Id && !it.IsDeleted)
                        .OrderBy(it => it.SequenceNumber)
                        .Select(it => new InvoiceItemReportDto
                        {
                            Id = it.Id,
                            InvoiceId = it.InvoiceId,
                            SequenceNumber = it.SequenceNumber,
                            // نام نوع شارژ/هزینه از جدول ChargeTypes
                            ChargeTypeName = _db.ChargeTypes
                                .Where(ct => ct.Id == it.ChargeTypeId)
                                .Select(ct => ct.ChargeTypeName)
                                .FirstOrDefault(),
                            Description = it.Description ?? string.Empty,
                            QTY = it.QTY,
                            UnitPrice = it.UnitPrice,
                            AmountBeforeDiscount = it.AmountBeforeDiscount,
                            Discount = it.Discount,
                            TaxPercent = it.TaxPercent,
                            TaxAmount = it.TaxAmount,
                            TotalAmount = it.TotalAmount
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return invoiceDto;
        }



        public async Task<Guid> GenerateFinalDetentionInvoiceAsync(Guid finalDetentionId)
        {
            var finalDetention = await _db.Detentions.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == finalDetentionId);

            if (finalDetention == null) throw new Exception("Detention not found.");
            if (!finalDetention.IsFinal) throw new Exception("Detention is not final.");
            var companyId = finalDetention.CompanyId;


            // 4) ساخت Invoice
            var invoice = new Invoice
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                FinanceDirectionId = 1,
                InvoiceDate = DateTime.Today,
                BookingId = finalDetention.BookingId,
                BLId = finalDetention.BLId,
                DetentionId = finalDetention.Id,
                PartyId = finalDetention.PartyId,
                CurrencyId = finalDetention.FinalCurrencyId,
                TotalAmount = finalDetention.Amount,
                SubTotal = finalDetention.Amount,
                TaxAmount = finalDetention.VATAmount,
                SettledAmount = 0,
                Remarks = "Final Detention Official Invoice"
            };

            foreach (var item in finalDetention.Items)
            {
                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    Id = Guid.CreateVersion7(),
                    InvoiceId = invoice.Id,
                    SequenceNumber = 1,
                    ChargeTypeId = 160,
                    Description = "Detention (Final)",
                    QTY = 1,
                    UnitPrice = item.DetentionItemAmount,
                    AmountBeforeDiscount = item.DetentionItemAmount,
                    AmountAfterDiscount = item.DetentionItemAmount,
                    TaxPercent = finalDetention.VATPercent,
                    TaxAmount = finalDetention.VATPercent * item.DetentionItemAmount * 0.01m,
                    TotalAmount = item.DetentionItemAmount + (finalDetention.VATPercent * item.DetentionItemAmount * 0.01m),
                    CurrencyId = invoice.CurrencyId
                });
            }
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            return invoice.Id;
        }

        public async Task<Guid> GenerateAndSettleFinalDetentionInvoiceAsync(Guid finalDetentionId)
        {

            var finalDetention = await _db.Detentions
                .FirstOrDefaultAsync(x => x.Id == finalDetentionId);

            if (finalDetention == null) throw new Exception("Detention not found.");
            if (!finalDetention.IsFinal) throw new Exception("Detention is not final.");

            var companyId = finalDetention.CompanyId;

            var detentionIds = await _db.Detentions
                .Where(d => d.BLId == finalDetention.BLId && d.CompanyId == companyId)
                .Select(d => d.Id)
                .ToListAsync();

            // 2) واکشی allocationهای پرداختِ قدیمی روی دیتنشن‌ها
            var oldAllocations = await _db.SettlementItemPaymentAllocations
                .Include(a => a.SettlementItem)
                .Where(a =>
                    a.SettlementItem.DetentionId.HasValue &&
                    detentionIds.Contains(a.SettlementItem.DetentionId.Value))
                .AsNoTracking()
                .ToListAsync();

            if (!oldAllocations.Any())
                throw new Exception("No prior payments found for detention.");

            var finalTotal = oldAllocations.Sum(x => x.SettledAmountInItemCurrency);


            var vm = new InvoiceEditVm { CompanyId = companyId, InvoiceModelId = 1, FNFId = 1, FinanceDirectionId = 1 };
            await SetInvoiceSerialAsync(vm);

            // 4) ساخت Invoice
            var invoice = new Invoice
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                FinanceDirectionId = 1,
                InvoiceDate = DateTime.Today,
                BookingId = finalDetention.BookingId,
                BLId = finalDetention.BLId,
                DetentionId = finalDetention.Id,
                PartyId = finalDetention.PartyId,
                CurrencyId = finalDetention.FinalCurrencyId,
                TotalAmount = finalTotal,
                SubTotal = finalTotal, // اگر VAT دارید جدا کنید
                TaxAmount = 0,
                SettledAmount = 0,
                Remarks = "Final Detention Official Invoice",
                Serial = vm.Serial,
                SerialPrefix = vm.SerialPrefix,
                InvoiceSubjectId = 3,
                InvoiceFNFId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = _currentUserService.UserId,
                ExchangeRate = 1,
                InvoiceModelId = 1,
                InvoiceTypeId = 1,
                InvoiceSubjectTitle = "دیرکرد کانتینر",
                IsPostedToAcc = false,
                InvoiceStatusId = 5,
                IsReturn = false,
                IsSentToTaxProvider = false,
                BranchId = finalDetention.BranchId
            };
            // InvoiceItems را طبق محاسبه خودتان اضافه کنید
            invoice.InvoiceItems.Add(new InvoiceItem
            {
                Id = Guid.CreateVersion7(),
                InvoiceId = invoice.Id,
                SequenceNumber = 1,
                ChargeTypeId = 160,
                Description = "Detention (Final)",
                ChargeTypeSupplementRemarks = "",
                QTY = 1,
                UnitPrice = finalTotal,
                AmountBeforeDiscount = finalTotal,
                AmountAfterDiscount = finalTotal,
                TaxPercent = 0,
                TaxAmount = 0,
                TotalAmount = finalTotal,
                CurrencyId = invoice.CurrencyId,
                CreateUserId = _currentUserService.UserId,
                CreatedAt = DateTime.Now,
                IsDeleted = false,
            });
            _db.Invoices.Add(invoice);
            // 5) ساخت Settlement و SettlementItem برای فاکتور
            var settlement = new Settlement
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                SettlementDate = invoice.InvoiceDate ?? DateTime.Today,
                PartyId = invoice.PartyId,
                CurrencyId = invoice.CurrencyId,
                Remarks = $"Settlement for Final Detention Invoice {invoice.InvoiceNo}",
                BLId = finalDetention.BLId,
            };
            _db.Settlements.Add(settlement);

            var settlementItem = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                SettlementId = settlement.Id,
                InvoiceId = invoice.Id,
                Amount = invoice.TotalAmount,
                Remarks = "Covered by previous detention payments (transfer)"
            };
            _db.SettlementItems.Add(settlementItem);

            // 6) انتقال تسویه: پر کردن SettlementItemPreviousPayment
            foreach (var old in oldAllocations)
            {
                _db.SettlementItemPreviousPayments.Add(new SettlementItemPreviousPayment
                {
                    Id = Guid.CreateVersion7(),
                    SettlementId = settlement.Id,
                    SettlementItemId = settlementItem.Id,

                    SettlementItemPaymentAllocationId = old.Id,

                    AllocatedAmount = old.AllocatedAmount,
                    SettledAmountInItemCurrency = old.SettledAmountInItemCurrency,

                    ExchangeRateId = old.ExchangeRateId,
                    ExchangeRateBaseId = old.ExchangeRateBaseId,
                    ExchangeRateDate = old.ExchangeRateDate,
                    ExchangeRateValue = old.ExchangeRateValue,

                    AllocatedAmountInBaseCurrency = old.AllocatedAmountInBaseCurrency,
                    Remarks = "Transfer from previous detention allocation"
                });
            }
            // 7) بروزرسانی SettledAmount فاکتور از محل انتقال
            invoice.SettledAmount = oldAllocations.Sum(x => x.SettledAmountInItemCurrency);
            invoice.InvoiceNo = invoice.SerialPrefix + " " + invoice.Serial.ToString();
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var s = ex.Message;
                return invoice.Id;
            }
            return Guid.Empty;
        }



        //Invoice Reports
        public async Task InvoiceReportDropDownBindAsync(InvoiceReportSearchDropDownVm vm, CancellationToken cancellationToken)
        {
            vm.InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken);
            vm.InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken);
            vm.FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken);
            vm.Currencies = await GetCurrenciesAsync(cancellationToken);
            vm.FNFS = await GetFNFSAsync(cancellationToken);
            vm.Companies = await GetCompaniesAsync(cancellationToken);
            vm.InvoiceSubjects = await GetInvoiceSubjectsAsync(cancellationToken);
            vm.InvoiceModels = await GetInvoiceModelsAsync(cancellationToken);
            vm.Branches = await GetBranchesAsync(cancellationToken);
            vm.ClearanceTypes = await GetClearanceTypesAsync(cancellationToken);
            //if (vm.JobId != null && vm.JobId.HasValue && vm.JobId != Guid.Empty)
            //{
            //    vm.BLs = await GetBLsAsync(vm.JobId.Value, cancellationToken);
            //    vm.Bookings = await GetBookingsAsync(vm.JobId.Value, cancellationToken);
            //}
        }
        public async Task<InvoiceListReportPageVm> GetInvoiceListReportAsync(InvoiceListReportFilterVm filter, CancellationToken cancellationToken = default)
        {

            filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            var invoices = _db.Invoices.AsNoTracking()
                //.Where(x => !x.IsDeleted && x.CompanyId == _requestContext.CompanyId);
                .Where(x => !x.IsDeleted);

            var isAgent = _currentUserService.IsInRole("External Party");
            if (isAgent)
            {
                var agentPersonId = await _currentUserService.GetPersonIdAsync();
                invoices = invoices.Where(x => x.CreateUserId == _currentUserService.UserId && x.PartyId == agentPersonId);
                //این هم میشه گذاشت 
            }
            //1
            if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
                invoices = invoices.Where(x => x.InvoiceNo.Contains(filter.InvoiceNo));
            //2
            if (filter.InvoiceTypeId.HasValue)
                invoices = invoices.Where(x => x.InvoiceTypeId == filter.InvoiceTypeId.Value);
            //3
            if (filter.CompanyId.HasValue)
                invoices = invoices.Where(x => x.CompanyId == filter.CompanyId.Value);
            //4
            if (filter.FinanceDirectionId.HasValue)
                invoices = invoices.Where(x => x.FinanceDirectionId == filter.FinanceDirectionId.Value);
            //5
            if (filter.CurrencyId.HasValue)
                invoices = invoices.Where(x => x.CurrencyId == filter.CurrencyId.Value);
            //6
            if (filter.InvoiceDateFrom.HasValue)
                invoices = invoices.Where(x => x.InvoiceDate >= filter.InvoiceDateFrom.Value);
            //7
            if (filter.InvoiceDateTo.HasValue)
            {
                var toDate = filter.InvoiceDateTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.InvoiceDate < toDate);
            }
            //8
            if (filter.DueDateFrom.HasValue)
                invoices = invoices.Where(x => x.InvoiceDate >= filter.DueDateFrom.Value);
            //9
            if (filter.DueDateTo.HasValue)
            {
                var toDate = filter.DueDateTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.InvoiceDate < toDate);
            }
            //10
            if (filter.InvoiceModelId.HasValue)
                invoices = invoices.Where(x => x.InvoiceModelId == filter.InvoiceModelId.Value);
            //11
            if (filter.InvoiceSubjectId.HasValue)
                invoices = invoices.Where(x => x.InvoiceSubjectId == filter.InvoiceSubjectId.Value);
            //12
            if (filter.ExchangeRate.HasValue)
                invoices = invoices.Where(x => x.ExchangeRate == filter.ExchangeRate);
            //13
            if (filter.FNFId.HasValue)
                invoices = invoices.Where(x => x.InvoiceFNFId == filter.FNFId.Value);
            //14
            if (filter.FirstTotalFrom.HasValue)
                invoices = invoices.Where(x => x.FirstTotal >= filter.FirstTotalFrom.Value);
            //15
            if (filter.FirstTotalTo.HasValue)
                invoices = invoices.Where(x => x.FirstTotal <= filter.FirstTotalTo.Value);
            //16
            if (filter.DiscountFrom.HasValue)
                invoices = invoices.Where(x => x.Discount >= filter.DiscountFrom.Value);
            //17
            if (filter.DiscountTo.HasValue)
                invoices = invoices.Where(x => x.Discount <= filter.DiscountTo.Value);
            //18
            if (filter.SubTotalFrom.HasValue)
                invoices = invoices.Where(x => x.SubTotal >= filter.SubTotalFrom.Value);
            //19
            if (filter.SubTotalTo.HasValue)
                invoices = invoices.Where(x => x.SubTotal <= filter.SubTotalTo.Value);
            //20
            if (!string.IsNullOrWhiteSpace(filter.Remarks))
                invoices = invoices.Where(x => x.Remarks.Contains(filter.Remarks));
            //21
            if (filter.TaxAmountFrom.HasValue)
                invoices = invoices.Where(x => x.TaxAmount >= filter.TaxAmountFrom.Value);
            //22
            if (filter.TaxAmountTo.HasValue)
                invoices = invoices.Where(x => x.TaxAmount <= filter.TaxAmountTo.Value);
            //23
            if (filter.TotalAmountFrom.HasValue)
                invoices = invoices.Where(x => x.TotalAmount >= filter.TotalAmountFrom.Value);
            //24
            if (filter.TotalAmountTo.HasValue)
                invoices = invoices.Where(x => x.TotalAmount <= filter.TotalAmountTo.Value);
            //25
            if (filter.InvoiceStatusId.HasValue)
                invoices = invoices.Where(x => x.InvoiceStatusId == filter.InvoiceStatusId.Value);
            //26
            if (filter.RemainPaymentFrom.HasValue)
                invoices = invoices.Where(x => (x.TotalAmount - x.SettledAmount) >= filter.RemainPaymentFrom.Value);
            //27
            if (filter.RemainPaymentTo.HasValue)
                invoices = invoices.Where(x => (x.TotalAmount - x.SettledAmount) <= filter.RemainPaymentTo.Value);
            //28
            if (!string.IsNullOrWhiteSpace(filter.Notes))
                invoices = invoices.Where(x => x.Notes.Contains(filter.Notes));
            //29
            if (!string.IsNullOrWhiteSpace(filter.BookingNo))
                invoices = invoices.Where(x => x.Booking.BookingNo.Contains(filter.BookingNo));
            //30
            if (!string.IsNullOrWhiteSpace(filter.JobNumber))
                invoices = invoices.Where(x => x.Job.JobFileNumber.Contains(filter.JobNumber));
            //31
            if (filter.CreatedAtFrom.HasValue)
                invoices = invoices.Where(x => x.CreatedAt >= filter.CreatedAtFrom.Value);
            //32
            if (filter.CreatedAtTo.HasValue)
            {
                var toDate = filter.CreatedAtTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.CreatedAt < toDate);
            }
            //33
            if (!string.IsNullOrWhiteSpace(filter.CreateUserName))
                invoices = invoices.Where(x => x.CreateUser.FullName.Contains(filter.CreateUserName));
            //34
            if (filter.UpdateAtFrom.HasValue)
                invoices = invoices.Where(x => x.UpdatedAt >= filter.UpdateAtFrom.Value);
            //35
            if (filter.UpdateAtTo.HasValue)
            {
                var toDate = filter.UpdateAtTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.UpdatedAt < toDate);
            }
            //36
            if (!string.IsNullOrWhiteSpace(filter.UpdateUserName))
                invoices = invoices.Where(x => x.UpdateUser.FullName.Contains(filter.UpdateUserName));
            //37
            if (!string.IsNullOrWhiteSpace(filter.BLNO))
                invoices = invoices.Where(x => (x.BL.BLNo.Contains(filter.BLNO) || x.BL.MasterBLNo.Contains(filter.BLNO)));
            //38
            if (filter.IsPostedToAcc.HasValue)
            {
                bool postedAcc = filter.IsPostedToAcc.Value == 1;
                invoices = invoices.Where(x => x.IsPostedToAcc == postedAcc);
            }

            //39
            if (filter.PostedToAccFrom.HasValue)
                invoices = invoices.Where(x => x.PostedToAcc >= filter.PostedToAccFrom.Value);
            //40
            if (filter.PostedToAccTo.HasValue)
            {
                var toDate = filter.PostedToAccTo.Value.Date.AddDays(1);
                invoices = invoices.Where(x => x.PostedToAcc < toDate);
            }
            //41
            if (filter.BranchId.HasValue)
                invoices = invoices.Where(x => x.BranchId == filter.BranchId.Value);
            //42
            if (!string.IsNullOrWhiteSpace(filter.SerialPrefix))
                invoices = invoices.Where(x => x.SerialPrefix.Contains(filter.SerialPrefix));
            //43
            if (filter.SerialFrom.HasValue)
                invoices = invoices.Where(x => x.Serial >= filter.SerialFrom.Value);
            //44
            if (filter.SerialTo.HasValue)
                invoices = invoices.Where(x => x.Serial <= filter.SerialTo.Value);
            //45
            if (filter.IsReturn.HasValue)
            {
                bool IsReturnStatus = filter.IsReturn.Value == 1;
                invoices = invoices.Where(x => x.IsReturn == IsReturnStatus);
            }
            //46
            if (filter.PartyId.HasValue)
                invoices = invoices.Where(x => x.PartyId == filter.PartyId.Value);
            //47
            if (filter.POLID.HasValue)
                invoices = invoices.Where(x => x.BL.POLId == filter.POLID.Value);
            //48
            if (filter.PODID.HasValue)
                invoices = invoices.Where(x => x.BL.PODId == filter.PODID.Value);
            //49
            if (filter.PODEID.HasValue)
                invoices = invoices.Where(x => x.BL.PODelId == filter.PODEID.Value);
            //50
            if (filter.PORID.HasValue)
                invoices = invoices.Where(x => x.BL.PORId == filter.PORID.Value);
            //51
            if (!string.IsNullOrWhiteSpace(filter.FinalDestination))
                invoices = invoices.Where(x => x.BL.FinalDestinationText.Contains(filter.FinalDestination));
            //52
            if (!string.IsNullOrWhiteSpace(filter.Shipper))
            {
                var shipper = filter.Shipper.Trim();
                invoices = invoices.Where(x => x.BL.BLParties.Any(p => p.RoleTypeId == 51 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(shipper)));
            }
            //53
            if (!string.IsNullOrWhiteSpace(filter.Consignee))
            {
                var consignee = filter.Consignee.Trim();
                invoices = invoices.Where(x => x.BL.BLParties.Any(p => p.RoleTypeId == 52 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(consignee)));
            }
            //54
            if (!string.IsNullOrWhiteSpace(filter.Notify))
            {
                var notify = filter.Notify.Trim();
                invoices = invoices.Where(x => x.BL.BLParties.Any(p => p.RoleTypeId == 53 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(notify)));
            }
            ///////////////////////////////////////////////////
            try
            {
                var query =
                    from i in invoices
                    join p in _db.Persons.AsNoTracking().Where(x => !x.IsDeleted) on i.PartyId equals p.Id into partyJoin
                    from p in partyJoin.DefaultIfEmpty()
                    join t in _db.InvoiceTypes.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceTypeId equals t.Id
                    join fnf in _db.InvoiceFNFs.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceFNFId equals fnf.Id
                    join imodel in _db.InvoiceModels.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceModelId equals imodel.Id
                    join s in _db.InvoiceStatuses.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceStatusId equals s.Id
                    join subjects in _db.InvoiceSubjects.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceSubjectId equals subjects.Id
                    join f in _db.FinanceDirections.AsNoTracking().Where(x => !x.IsDeleted) on i.FinanceDirectionId equals f.Id
                    join c in _db.Currencies.AsNoTracking().Where(x => !x.IsDeleted) on i.CurrencyId equals c.Id
                    join u in _db.Users.AsNoTracking().Where(x => !x.IsDeleted && (x.UserTypeId == 1 || x.UserTypeId == 2)) on i.CreateUserId equals u.Id
                    join uu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.EditUserId equals uu.Id into updateUser
                    from uu in updateUser.DefaultIfEmpty()
                    join uuu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.DeleteUserId equals uuu.Id into deleteUser
                    from uuu in deleteUser.DefaultIfEmpty()
                    select new InvoiceListItemReportVm
                    {
                        Id = i.Id,
                        InvoiceNo = i.InvoiceNo,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        PartyName = p != null ? p.PersonName : "",
                        InvoiceTypeName = t.InvoiceTypeName,
                        InvoiceStatusName = s.InvoiceStatusName,
                        FinanceDirectionName = f.InvoiceContextName,
                        CurrencyCode = c.CurrencyCode,
                        TotalAmount = i.TotalAmount,
                        FNFName = i.InvoiceFNF.FNFName,
                        CreateDateTime = i.CreatedAt,
                        CreateUserName = u.UserName,
                        UpdateDateTime = i.UpdatedAt,
                        UpdateUserName = uu.UserName,
                        DeleteDateTime = i.DeletedAt,
                        DeleteUserName = uuu.UserName,
                        InvoiceSubjectId = i.InvoiceSubjectId,
                        InvoiceSubjectName = subjects.InvoiceSubjectName,
                        InvoiceSubjectTitle = i.InvoiceSubjectTitle,
                        InvoiceModelId = i.InvoiceModelId,
                        InvoiceModelName = i.InvoiceModel.InvoiceModelName,
                        TotalPaid = i.SettledAmount,
                        RemainPayment = i.TotalAmount - i.SettledAmount,
                        PartyId = i.PartyId.Value,
                        SettledAmount = i.SettledAmount,
                        //RemainingAmount = i.RemainingAmount,
                        IsFullySettled = i.IsFullySettled,
                        BLNo = i.BL.BLNo,
                        JobFileNo = i.Job.JobFileNumber,
                        BookingNo = i.Booking.BookingNo,
                        Serial = i.Serial,
                        SerialPrefix = i.SerialPrefix,
                        CompanyName = i.Company.CompanyAbb,
                        DirectionId = i.FinanceDirectionId,
                        //
                        ExchangeRate = i.ExchangeRate,
                        FirstTotal = i.FirstTotal,
                        Discount = i.Discount,
                        SubTotal = i.SubTotal,
                        TaxAmount = i.TaxAmount,
                        Notes = i.Notes,
                        Remarks = i.Remarks,
                        IsPostedToAcc = i.IsPostedToAcc,
                        PostedToAcc = i.PostedToAcc,
                        DefaultTaxPercent = i.DefaultTaxPercent,
                        IsReturn = i.IsReturn,
                        BranchId = i.BranchId,
                        BranchDesc = i.Branch.BranchName,
                        POD = i.BL.PODText,
                        PODID = i.BL.PODId,
                        POL = i.BL.POLText,
                        POLID = i.BL.POLId,
                        POR = i.BL.PORText,
                        PORID = i.BL.PORId,
                        PODE = i.BL.PODelText,
                        PODEID = i.BL.PODelId,
                        FinalDestination = i.BL.FinalDestinationText,
                        Shipper = i.BL.BLParties.Where(p => p.RoleTypeId == 51).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",
                        Consignee = i.BL.BLParties.Where(p => p.RoleTypeId == 52).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",
                        Notify = i.BL.BLParties.Where(p => p.RoleTypeId == 53).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",
                    };


                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.InvoiceNo)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);
                return new InvoiceListReportPageVm
                {
                    Filter = filter,
                    Result = new PagedResultVm<InvoiceListItemReportVm>
                    {
                        Items = items,
                        TotalCount = 0,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize
                    },
                    InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken),
                    InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken),
                    FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken),
                    Currencies = await GetCurrenciesAsync(cancellationToken)
                };
            }
            catch (Exception)
            {

                throw;
            }
        }
        //Invoice Item Reports
        public async Task InvoiceItemReportDropDownBindAsync(InvoiceItemReportSearchDropDownVm vm, CancellationToken cancellationToken)
        {
            vm.InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken);
            vm.InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken);
            vm.FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken);
            vm.Currencies = await GetCurrenciesAsync(cancellationToken);
            vm.FNFS = await GetFNFSAsync(cancellationToken);
            vm.Companies = await GetCompaniesAsync(cancellationToken);
            vm.InvoiceSubjects = await GetInvoiceSubjectsAsync(cancellationToken);
            vm.InvoiceModels = await GetInvoiceModelsAsync(cancellationToken);
            vm.Branches = await GetBranchesAsync(cancellationToken);
            vm.ClearanceTypes = await GetClearanceTypesAsync(cancellationToken);
            vm.ChargeTypes = await GetChargeTypesAsync(cancellationToken);
        }
        public async Task<InvoiceItemListReportPageVm> GetInvoiceItemListReportAsync(InvoiceItemListReportFilterVm filter, CancellationToken cancellationToken = default)
        {
            filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            filter.PageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

            // شروع کوئری از اقلام فاکتور
            var invoiceItems = _db.InvoiceItems.AsNoTracking()
                .Where(x => !x.IsDeleted && !x.Invoice.IsDeleted);

            var isAgent = _currentUserService.IsInRole("External Party");
            if (isAgent)
            {
                var agentPersonId = await _currentUserService.GetPersonIdAsync();
                invoiceItems = invoiceItems.Where(x => x.Invoice.CreateUserId == _currentUserService.UserId && x.Invoice.PartyId == agentPersonId);
            }

            // 1
            if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceNo.Contains(filter.InvoiceNo));
            // 2
            if (filter.InvoiceTypeId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceTypeId == filter.InvoiceTypeId.Value);
            // 3
            if (filter.CompanyId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.CompanyId == filter.CompanyId.Value);
            // 4
            if (filter.FinanceDirectionId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.FinanceDirectionId == filter.FinanceDirectionId.Value);
            // 5
            if (filter.CurrencyId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.CurrencyId == filter.CurrencyId.Value);
            // 6
            if (filter.InvoiceDateFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceDate >= filter.InvoiceDateFrom.Value);
            // 7
            if (filter.InvoiceDateTo.HasValue)
            {
                var toDate = filter.InvoiceDateTo.Value.Date.AddDays(1);
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceDate < toDate);
            }
            // 8
            if (filter.DueDateFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.DueDate >= filter.DueDateFrom.Value);
            // 9
            if (filter.DueDateTo.HasValue)
            {
                var toDate = filter.DueDateTo.Value.Date.AddDays(1);
                invoiceItems = invoiceItems.Where(x => x.Invoice.DueDate < toDate);
            }
            // 10
            if (filter.InvoiceModelId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceModelId == filter.InvoiceModelId.Value);
            // 11
            if (filter.InvoiceSubjectId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceSubjectId == filter.InvoiceSubjectId.Value);
            // 12
            if (filter.ExchangeRate.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.ExchangeRate == filter.ExchangeRate);
            // 13
            if (filter.FNFId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceFNFId == filter.FNFId.Value);
            // 14
            if (filter.FirstTotalFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.FirstTotal >= filter.FirstTotalFrom.Value);
            // 15
            if (filter.FirstTotalTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.FirstTotal <= filter.FirstTotalTo.Value);
            // 16
            if (filter.DiscountFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.Discount >= filter.DiscountFrom.Value);
            // 17
            if (filter.DiscountTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.Discount <= filter.DiscountTo.Value);
            // 18
            if (filter.SubTotalFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.SubTotal >= filter.SubTotalFrom.Value);
            // 19
            if (filter.SubTotalTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.SubTotal <= filter.SubTotalTo.Value);
            // 20
            if (!string.IsNullOrWhiteSpace(filter.Remarks))
                invoiceItems = invoiceItems.Where(x => x.Invoice.Remarks.Contains(filter.Remarks));
            // 21
            if (filter.TaxAmountFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.TaxAmount >= filter.TaxAmountFrom.Value);
            // 22
            if (filter.TaxAmountTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.TaxAmount <= filter.TaxAmountTo.Value);
            // 23
            if (filter.TotalAmountFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.TotalAmount >= filter.TotalAmountFrom.Value);
            // 24
            if (filter.TotalAmountTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.TotalAmount <= filter.TotalAmountTo.Value);
            // 25
            if (filter.InvoiceStatusId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.InvoiceStatusId == filter.InvoiceStatusId.Value);
            // 26
            if (filter.RemainPaymentFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => (x.Invoice.TotalAmount - x.Invoice.SettledAmount) >= filter.RemainPaymentFrom.Value);
            // 27
            if (filter.RemainPaymentTo.HasValue)
                invoiceItems = invoiceItems.Where(x => (x.Invoice.TotalAmount - x.Invoice.SettledAmount) <= filter.RemainPaymentTo.Value);
            // 28
            if (!string.IsNullOrWhiteSpace(filter.Notes))
                invoiceItems = invoiceItems.Where(x => x.Invoice.Notes.Contains(filter.Notes));
            // 29
            if (!string.IsNullOrWhiteSpace(filter.BookingNo))
                invoiceItems = invoiceItems.Where(x => x.Invoice.Booking.BookingNo.Contains(filter.BookingNo));
            // 30
            if (!string.IsNullOrWhiteSpace(filter.JobNumber))
                invoiceItems = invoiceItems.Where(x => x.Invoice.Job.JobFileNumber.Contains(filter.JobNumber));
            // 31
            if (filter.CreatedAtFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.CreatedAt >= filter.CreatedAtFrom.Value);
            // 32
            if (filter.CreatedAtTo.HasValue)
            {
                var toDate = filter.CreatedAtTo.Value.Date.AddDays(1);
                invoiceItems = invoiceItems.Where(x => x.Invoice.CreatedAt < toDate);
            }
            // 33
            if (!string.IsNullOrWhiteSpace(filter.CreateUserName))
                invoiceItems = invoiceItems.Where(x => x.Invoice.CreateUser.FullName.Contains(filter.CreateUserName));
            // 34
            if (filter.UpdateAtFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.UpdatedAt >= filter.UpdateAtFrom.Value);
            // 35
            if (filter.UpdateAtTo.HasValue)
            {
                var toDate = filter.UpdateAtTo.Value.Date.AddDays(1);
                invoiceItems = invoiceItems.Where(x => x.Invoice.UpdatedAt < toDate);
            }
            // 36
            if (!string.IsNullOrWhiteSpace(filter.UpdateUserName))
                invoiceItems = invoiceItems.Where(x => x.Invoice.UpdateUser.FullName.Contains(filter.UpdateUserName));
            // 37
            if (!string.IsNullOrWhiteSpace(filter.BLNO))
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.BLNo.Contains(filter.BLNO) || x.Invoice.BL.MasterBLNo.Contains(filter.BLNO));
            // 38
            if (filter.IsPostedToAcc.HasValue)
            {
                bool postedAcc = filter.IsPostedToAcc.Value == 1;
                invoiceItems = invoiceItems.Where(x => x.Invoice.IsPostedToAcc == postedAcc);
            }
            // 39
            if (filter.PostedToAccFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.PostedToAcc >= filter.PostedToAccFrom.Value);
            // 40
            if (filter.PostedToAccTo.HasValue)
            {
                var toDate = filter.PostedToAccTo.Value.Date.AddDays(1);
                invoiceItems = invoiceItems.Where(x => x.Invoice.PostedToAcc < toDate);
            }
            // 41
            if (filter.BranchId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.BranchId == filter.BranchId.Value);
            // 42
            if (!string.IsNullOrWhiteSpace(filter.SerialPrefix))
                invoiceItems = invoiceItems.Where(x => x.Invoice.SerialPrefix.Contains(filter.SerialPrefix));
            // 43
            if (filter.SerialFrom.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.Serial >= filter.SerialFrom.Value);
            // 44
            if (filter.SerialTo.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.Serial <= filter.SerialTo.Value);
            // 45
            if (filter.IsReturn.HasValue)
            {
                bool IsReturnStatus = filter.IsReturn.Value == 1;
                invoiceItems = invoiceItems.Where(x => x.Invoice.IsReturn == IsReturnStatus);
            }
            // 46
            if (filter.PartyId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.PartyId == filter.PartyId.Value);
            // 47
            if (filter.POLID.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.POLId == filter.POLID.Value);
            // 48
            if (filter.PODID.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.PODId == filter.PODID.Value);
            // 49
            if (filter.PODEID.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.PODelId == filter.PODEID.Value);
            // 50
            if (filter.PORID.HasValue)
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.PORId == filter.PORID.Value);
            // 51
            if (!string.IsNullOrWhiteSpace(filter.FinalDestination))
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.FinalDestinationText.Contains(filter.FinalDestination));
            // 52
            if (!string.IsNullOrWhiteSpace(filter.Shipper))
            {
                var shipper = filter.Shipper.Trim();
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.BLParties.Any(p => p.RoleTypeId == 51 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(shipper)));
            }
            // 53
            if (!string.IsNullOrWhiteSpace(filter.Consignee))
            {
                var consignee = filter.Consignee.Trim();
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.BLParties.Any(p => p.RoleTypeId == 52 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(consignee)));
            }
            // 54
            if (!string.IsNullOrWhiteSpace(filter.Notify))
            {
                var notify = filter.Notify.Trim();
                invoiceItems = invoiceItems.Where(x => x.Invoice.BL.BLParties.Any(p => p.RoleTypeId == 53 && p.PartyTextInBL != null && p.PartyTextInBL.Contains(notify)));
            }
            //55
            if (filter.ChargeTypeId.HasValue)
                invoiceItems = invoiceItems.Where(x => x.ChargeTypeId == filter.ChargeTypeId.Value);

            try
            {
                var query =
                    from item in invoiceItems
                    let i = item.Invoice
                    join p in _db.Persons.AsNoTracking().Where(x => !x.IsDeleted) on i.PartyId equals p.Id into partyJoin
                    from p in partyJoin.DefaultIfEmpty()
                    join t in _db.InvoiceTypes.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceTypeId equals t.Id
                    join fnf in _db.InvoiceFNFs.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceFNFId equals fnf.Id
                    join imodel in _db.InvoiceModels.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceModelId equals imodel.Id
                    join s in _db.InvoiceStatuses.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceStatusId equals s.Id
                    join subjects in _db.InvoiceSubjects.AsNoTracking().Where(x => !x.IsDeleted) on i.InvoiceSubjectId equals subjects.Id
                    join f in _db.FinanceDirections.AsNoTracking().Where(x => !x.IsDeleted) on i.FinanceDirectionId equals f.Id
                    join c in _db.Currencies.AsNoTracking().Where(x => !x.IsDeleted) on i.CurrencyId equals c.Id
                    join u in _db.Users.AsNoTracking().Where(x => !x.IsDeleted && (x.UserTypeId == 1 || x.UserTypeId == 2)) on i.CreateUserId equals u.Id
                    join uu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.EditUserId equals uu.Id into updateUser
                    from uu in updateUser.DefaultIfEmpty()
                    join uuu in _db.Users.AsNoTracking().Where(x => !x.IsDeleted) on i.DeleteUserId equals uuu.Id into deleteUser
                    from uuu in deleteUser.DefaultIfEmpty()
                    select new InvoiceItemListItemReportVm
                    {
                        Id = item.Id, // آیدی سطر قلم کالا (یا در صورت نیاز به آیدی فاکتور: i.Id)
                        InvoiceNo = i.InvoiceNo,
                        InvoiceDate = i.InvoiceDate,
                        DueDate = i.DueDate,
                        PartyName = p != null ? p.PersonName : "",
                        InvoiceTypeName = t.InvoiceTypeName,
                        InvoiceStatusName = s.InvoiceStatusName,
                        FinanceDirectionName = f.InvoiceContextName,
                        CurrencyCode = c.CurrencyCode,
                        TotalAmount = i.TotalAmount,
                        FNFName = i.InvoiceFNF.FNFName,
                        CreateDateTime = i.CreatedAt,
                        CreateUserName = u.UserName,
                        UpdateDateTime = i.UpdatedAt,
                        UpdateUserName = uu.UserName,
                        DeleteDateTime = i.DeletedAt,
                        DeleteUserName = uuu.UserName,
                        InvoiceSubjectId = i.InvoiceSubjectId,
                        InvoiceSubjectName = subjects.InvoiceSubjectName,
                        InvoiceSubjectTitle = i.InvoiceSubjectTitle,
                        InvoiceModelId = i.InvoiceModelId,
                        InvoiceModelName = i.InvoiceModel.InvoiceModelName,
                        TotalPaid = i.SettledAmount,
                        RemainPayment = i.TotalAmount - i.SettledAmount,
                        PartyId = i.PartyId.Value,
                        SettledAmount = i.SettledAmount,
                        IsFullySettled = i.IsFullySettled,
                        BLNo = i.BL.BLNo,
                        JobFileNo = i.Job.JobFileNumber,
                        BookingNo = i.Booking.BookingNo,
                        Serial = i.Serial,
                        SerialPrefix = i.SerialPrefix,
                        CompanyName = i.Company.CompanyAbb,
                        DirectionId = i.FinanceDirectionId,
                        ExchangeRate = i.ExchangeRate,
                        FirstTotal = i.FirstTotal,
                        Discount = i.Discount,
                        SubTotal = i.SubTotal,
                        TaxAmount = i.TaxAmount,
                        Notes = i.Notes,
                        Remarks = i.Remarks,
                        IsPostedToAcc = i.IsPostedToAcc,
                        PostedToAcc = i.PostedToAcc,
                        DefaultTaxPercent = i.DefaultTaxPercent,
                        IsReturn = i.IsReturn,
                        BranchId = i.BranchId,
                        BranchDesc = i.Branch.BranchName,
                        POD = i.BL.PODText,
                        PODID = i.BL.PODId,
                        POL = i.BL.POLText,
                        POLID = i.BL.POLId,
                        POR = i.BL.PORText,
                        PORID = i.BL.PORId,
                        PODE = i.BL.PODelText,
                        PODEID = i.BL.PODelId,
                        FinalDestination = i.BL.FinalDestinationText,
                        Shipper = i.BL.BLParties.Where(p => p.RoleTypeId == 51).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",
                        Consignee = i.BL.BLParties.Where(p => p.RoleTypeId == 52).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",
                        Notify = i.BL.BLParties.Where(p => p.RoleTypeId == 53).Select(p => p.PartyTextInBL).FirstOrDefault() ?? "",

                        QTY = item.QTY,
                        Descriptsion = item.Description,
                        UnitPrice = item.UnitPrice,
                        AmountBeforeDiscount = item.AmountBeforeDiscount,
                        AmountAfterDiscount = item.AmountAfterDiscount,
                        ItemDiscount = item.Discount,
                        TaxPercent = item.TaxPercent,
                        ItemTaxAmount = item.TaxAmount,
                        ItemTotalAmount = item.TotalAmount,
                        ChargeTypeSupplementRemarks = item.ChargeTypeSupplementRemarks,
                        ChargeTypeId = item.ChargeTypeId,
                        ChargeTypeName = item.ChargeType.ChargeTypeName
                    };

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.InvoiceNo)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync(cancellationToken);

                return new InvoiceItemListReportPageVm
                {
                    Filter = filter,
                    Result = new PagedResultVm<InvoiceItemListItemReportVm>
                    {
                        Items = items,
                        TotalCount = totalCount, // به درستی متصل شد
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize
                    },
                    InvoiceTypes = await GetInvoiceTypesAsync(cancellationToken),
                    InvoiceStatuses = await GetInvoiceStatusesAsync(cancellationToken),
                    FinanceDirections = await GetFinanceDirectionsAsync(cancellationToken),
                    Currencies = await GetCurrenciesAsync(cancellationToken)
                };
            }
            catch (Exception)
            {
                throw;
            }
        }


        public async Task<InvoiceSoaVm?> GetInvoiceSoaAsync(Guid invoiceId, decimal roe, CancellationToken cancellationToken = default)
        {
            if (invoiceId == Guid.Empty)
                throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));

            if (roe <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(roe), "Exchange rate must be greater than zero.");

            var invoice = await _db.Invoices
                .AsNoTracking()
                .Where(x => x.Id == invoiceId)
                .Select(x => new
                {
                    x.Id,
                    x.CompanyId,
                    x.InvoiceNo,
                    x.InvoiceDate,
                    x.PartyId,
                    x.CurrencyId,
                    x.FinanceDirectionId,
                    x.IsReturn,
                    x.ExchangeRate,
                    x.TotalAmount,
                    x.SettledAmount
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return null;

            if (invoice.FinanceDirectionId is not (1 or 2))
                throw new NotSupportedException(
                    "Only sales and purchase invoices are supported.");

            // Return invoices need a separately defined debit/credit rule.
            if (invoice.IsReturn == true)
                throw new NotSupportedException(
                    "Return invoices require a separate debit/credit mapping.");

            var allocations = await _db.SettlementItemPaymentAllocations
                .AsNoTracking()
                .Where(a =>
                    a.SettlementItem.InvoiceId == invoiceId &&
                    a.Settlement.CompanyId == invoice.CompanyId &&
                    a.PaymentDocument.CompanyId == invoice.CompanyId)
                .Select(a => new
                {
                    a.Id,
                    a.Settlement.SettlementDate,
                    a.Settlement.SettlementNo,
                    SettlementRemarks = a.Settlement.Remarks,
                    ItemRemarks = a.SettlementItem.Remarks,
                    AllocationRemarks = a.Remarks,

                    // Amount covered in the invoice item's currency
                    a.SettledAmountInItemCurrency,

                    a.PaymentDocument.DocumentNo,
                    a.PaymentDocument.PaymentMethodId,
                    a.PaymentDocument.BankId,
                    a.PaymentDocument.ReferenceNo,
                    a.PaymentDocument.TrackingNo,
                    PaymentDescription = a.PaymentDocument.Description
                })
                .OrderBy(a => a.SettlementDate)
                .ThenBy(a => a.SettlementNo)
                .ThenBy(a => a.Id)
                .ToListAsync(cancellationToken);

            var report = new InvoiceSoaVm
            {
                InvoiceId = invoice.Id,
                InvoiceNo = invoice.InvoiceNo ?? invoice.Id.ToString(),
                InvoiceDate = invoice.InvoiceDate,
                PartyId = invoice.PartyId,
                CurrencyId = invoice.CurrencyId,
                FinanceDirectionId = invoice.FinanceDirectionId,
                InvoiceExchangeRate = invoice.ExchangeRate,
                InvoiceTotal = invoice.TotalAmount,
                RecordedSettledAmount = invoice.SettledAmount,
                Roe = roe
            };

            decimal runningBalance = 0m;

            void AddRow(InvoiceSoaRowVm row)
            {
                row.RowNo = report.Rows.Count + 1;
                runningBalance += row.Debit - row.Credit;
                row.SignedBalance = runningBalance;
                report.Rows.Add(row);
            }

            // Sales: invoice is debit. Purchase: invoice is credit.
            AddRow(new InvoiceSoaRowVm
            {
                Date = invoice.InvoiceDate,
                Method = "Invoice",
                Description = invoice.FinanceDirectionId == 1
                    ? "Sales invoice issuance"
                    : "Purchase invoice issuance",
                Debit = invoice.FinanceDirectionId == 1
                    ? invoice.TotalAmount : 0m,
                Credit = invoice.FinanceDirectionId == 2
                    ? invoice.TotalAmount : 0m
            });

            foreach (var allocation in allocations)
            {
                var amount = allocation.SettledAmountInItemCurrency;

                // Positive allocation clears the invoice; negative allocation reverses it.
                var isDebit = invoice.FinanceDirectionId == 1
                    ? amount < 0
                    : amount > 0;

                var references = new[]
                {
                allocation.BankId is Guid bankId ? $"Bank ID: {bankId}" : null,
                !string.IsNullOrWhiteSpace(allocation.ReferenceNo)
                    ? $"Ref: {allocation.ReferenceNo}" : null,
                !string.IsNullOrWhiteSpace(allocation.TrackingNo)
                    ? $"Tracking: {allocation.TrackingNo}" : null
            };

                var remarks = new[]
                {
                amount < 0 ? "Allocation reversal" : "Payment allocation",
                allocation.PaymentDescription,
                allocation.AllocationRemarks,
                allocation.ItemRemarks,
                allocation.SettlementRemarks
            };
                AddRow(new InvoiceSoaRowVm
                {
                    Date = allocation.SettlementDate,
                    SettlementNo = allocation.SettlementNo.ToString(),
                    PaymentDocumentNo = allocation.DocumentNo,
                    Method = $"Method #{allocation.PaymentMethodId}",
                    BankReference = string.Join(" / ",
                        references.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Description = string.Join(" | ",
                        remarks.Where(x => !string.IsNullOrWhiteSpace(x))),
                    Debit = isDebit ? Math.Abs(amount) : 0m,
                    Credit = isDebit ? 0m : Math.Abs(amount)
                });
            }

            return report;
        }

        public async Task<List<NosaXmlInvoiceRowDto>> SearchNosaXmlInvoicesAsync(NosaXmlInvoiceFilterDto filter, CancellationToken cancellationToken = default)
        {
            if (!filter.CompanyId.HasValue)
                throw new ArgumentException("انتخاب شرکت الزامی است.");

            var query = _db.Set<Invoice>()
                .AsNoTracking()
                .Where(x => x.CompanyId == filter.CompanyId.Value);

            if (filter.FromDate.HasValue)
            {
                var from = filter.FromDate.Value.Date;
                query = query.Where(x => x.InvoiceDate >= from);
            }

            if (filter.ToDate.HasValue)
            {
                var until = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.InvoiceDate < until);
            }

            if (filter.FinanceDirectionId.HasValue && filter.FinanceDirectionId.Value > 0)
                query = query.Where(x => x.FinanceDirectionId == filter.FinanceDirectionId.Value);

            if (filter.InvoiceFNFId.HasValue && filter.InvoiceFNFId.Value > 0)
                query = query.Where(x => x.InvoiceFNFId == filter.InvoiceFNFId.Value);

            return await query
                .OrderByDescending(x => x.InvoiceDate)
                .ThenByDescending(x => x.Serial)
                .Select(x => new NosaXmlInvoiceRowDto
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    Serial = x.Serial,
                    InvoiceDate = x.InvoiceDate,
                    PartyName = x.Party != null ? (x.Party.PersonName) : "-",
                    JobFileNo = x.Job != null ? x.Job.JobFileNumber : "-",
                    BLNo = x.BL != null ? x.BL.BLNo : "-",
                    FinanceDirectionTitle = x.FinanceDirection != null ? x.FinanceDirection.FinanceDirectionName : x.FinanceDirectionId.ToString(),
                    FNFTitle = x.InvoiceFNF != null ? x.InvoiceFNF.FNFName : "-",
                    TotalAmount = x.TotalAmount,
                    CurrencyCode = x.Currency != null ? x.Currency.CurrencyCode : "",
                    IsPostedToAcc = x.IsPostedToAcc
                })
                .ToListAsync(cancellationToken);
        }

        // متد ۲: تولید فایل XML متناسب با نرم‌افزار مالی نوسا (XPXML)
        public async Task<byte[]> GenerateNosaXmlAsync(NosaXmlExportRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!request.CompanyId.HasValue)
                throw new ArgumentException("انتخاب شرکت الزامی است.");

            var selectedIds = request.SelectedInvoiceIds.Distinct().ToList();
            if (!selectedIds.Any())
                throw new ArgumentException("هیچ صورتحسابی برای صدور فایل انتخاب نشده است.");

            int companyId = request.CompanyId.Value;

            // دریافت فاکتورها همراه با تمام اطلاعات و جزئیات مورد نیاز
            var invoices = await _db.Set<Invoice>()
                .Include(x => x.Job)
                .Include(x => x.BL)
                .Include(x => x.InvoiceFNF)
                .Include(x => x.InvoiceItems)
                    .ThenInclude(i => i.ChargeType)
                .Where(x => x.CompanyId == companyId && selectedIds.Contains(x.Id))
                .OrderBy(x => x.InvoiceDate)
                .ThenBy(x => x.Serial)
                .ToListAsync(cancellationToken);

            if (!invoices.Any())
                throw new InvalidOperationException("فاکتورهای انتخاب شده یافت نشدند.");

            // استخراج کدهای ChargeType موجود در این فاکتورها برای دریافت تنظیمات Nosa
            var chargeTypeIds = invoices
                .SelectMany(x => x.InvoiceItems)
                .Select(x => x.ChargeTypeId)
                .Distinct()
                .ToList();

            var nosaSettings = await _db.Set<ChargeTypeNosaSetting>()
                .AsNoTracking()
                .Where(s => s.CompanyId == companyId && chargeTypeIds.Contains(s.ChargeTypeId))
                .ToDictionaryAsync(k => k.ChargeTypeId, v => v, cancellationToken);

            // استخراج اشخاص و کدهای تفصیلی نوسا از جدول AccountCode
            var partyIds = invoices.Where(x => x.PartyId.HasValue).Select(x => x.PartyId!.Value).Distinct().ToList();
            var accountCodes = await _db.Set<AccountCode>()
                .AsNoTracking()
                .Where(a => a.CompanyId == companyId && a.PersonId.HasValue && partyIds.Contains(a.PersonId.Value))
                .ToDictionaryAsync(k => k.PersonId!.Value, v => v.AccCode?.Trim() ?? string.Empty, cancellationToken);

            using var memoryStream = new MemoryStream();
            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                Indent = true
            };

            using (var writer = XmlWriter.Create(memoryStream, xmlSettings))
            {
                // روت اصلی سند
                writer.WriteStartElement("_XPXML");
                writer.WriteAttributeString("Desc", "");
                writer.WriteAttributeString("AppId", "1");
                writer.WriteAttributeString("Ver", "303");
                writer.WriteAttributeString("Srv", "192.168.0.2"); // یا خواندن از پیکربندی
                writer.WriteAttributeString("DBN", "_AccXP_PAS");
                writer.WriteAttributeString("CN", "XXXX");
                writer.WriteAttributeString("CrtTime", DateTime.Now.ToOADate().ToString("0.##########", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Note", "");

                foreach (var inv in invoices)
                {
                    BuildSingleInvoiceVoucher(writer, inv, nosaSettings, accountCodes);
                }

                writer.WriteEndElement(); // _XPXML
            }

            return memoryStream.ToArray();
        }

        private void BuildSingleInvoiceVoucher(XmlWriter writer, Invoice inv, Dictionary<int, ChargeTypeNosaSetting> nosaSettings, Dictionary<Guid, string> accountCodes)
        {
            // ۱. اعتبارسنجی‌ها و استخراج مقادیر پایه
            if (!inv.PartyId.HasValue || !accountCodes.TryGetValue(inv.PartyId.Value, out var det1) || string.IsNullOrWhiteSpace(det1))
                throw new InvalidOperationException($"کد حسابداری نوسا (AccountCode) برای طرف حساب در فاکتور '{inv.InvoiceNo}' تعریف نشده است.");

            // تفصیلی ۴: بر اساس Job.JobNumber
            string det4 = inv.Job?.JobNumber?.ToString() ?? string.Empty;
            string fileNo = inv.Job?.JobFileNumber ?? "public";
            string blNo = inv.BL?.BLNo ?? (inv.Job?.JobFileNumber ?? "");
            string invDate = inv.InvoiceDate?.ToString("yy/MM/dd") ?? DateTime.Now.ToString("yy/MM/dd");
            string fnfText = inv.InvoiceFNF?.FNFName ?? "F";
            string refN = !string.IsNullOrWhiteSpace(inv.InvoiceNo) ? inv.InvoiceNo : inv.Serial.ToString();
            string dueN = inv.Serial > 0 ? inv.Serial.ToString() : refN;
            string curr = inv.CurrencyId > 0 ? inv.CurrencyId.ToString() : "1";

            // تشخیص فاکتور خرید (مثلاً FinanceDirectionId = 1) یا فروش (مثلاً FinanceDirectionId = 2)
            bool isBuy = inv.FinanceDirectionId == 2;

            // ۲. ایجاد و اعتبارسنجی لاین‌های فاکتور
            if (isBuy)
            {
                // ===== خرید (Buy) =====
                // الف) ردیف‌های بدهکار خدمات (هزینه‌ها)
                foreach (var item in inv.InvoiceItems)
                {
                    if (!nosaSettings.TryGetValue(item.ChargeTypeId, out var setting) || string.IsNullOrWhiteSpace(setting.BuyDebitAccountCode))
                        throw new InvalidOperationException($"کد حساب معین خرید برای نوع هزینه '{item.ChargeType?.ChargeTypeName}' تعریف نشده است.");

                    writer.WriteStartElement("_FTrans");
                    writer.WriteAttributeString("Acc", setting.BuyDebitAccountCode.Trim());
                    writer.WriteAttributeString("Det1", det1);
                    writer.WriteAttributeString("Det4", det4);
                    writer.WriteAttributeString("Deb", item.AmountAfterDiscount.ToString("0.##", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Desc", $"{{File No:{fileNo} }} {{Inv ID:{refN}}} {{ Service:{item.ChargeType?.ChargeTypeName ?? item.Description}}} {{HBL: {blNo}}} {{FNF: {fnfText}}}");
                    writer.WriteAttributeString("ODesc", blNo);
                    writer.WriteAttributeString("InvoiceDate", invDate);
                    writer.WriteAttributeString("Curr", curr);
                    writer.WriteAttributeString("RefN", refN);
                    writer.WriteAttributeString("DueN", dueN);
                    writer.WriteEndElement();
                }

                // ب) ردیف بدهکار مالیات ارزش افزوده خرید (کد پیش‌فرض 110501)
                if (inv.TaxAmount > 0)
                {
                    writer.WriteStartElement("_FTrans");
                    writer.WriteAttributeString("Acc", "110501");
                    writer.WriteAttributeString("Det1", det1);
                    writer.WriteAttributeString("Det4", det4);
                    writer.WriteAttributeString("Deb", inv.TaxAmount.ToString("0.##", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Desc", $" {{Inv ID:{refN}}} {{File No: {fileNo}}} {{FNF: {fnfText}}}");
                    writer.WriteAttributeString("ODesc", blNo);
                    writer.WriteAttributeString("InvoiceDate", invDate);
                    writer.WriteAttributeString("Curr", curr);
                    writer.WriteAttributeString("RefN", refN);
                    writer.WriteAttributeString("DueN", dueN);
                    writer.WriteEndElement();
                }

                // ج) ردیف بستانکار کل فاکتور (تأمین‌کننده / بستانکاران تجاری 311031)
                var firstSetting = nosaSettings[inv.InvoiceItems.First().ChargeTypeId];
                string creditAcc = !string.IsNullOrWhiteSpace(firstSetting.BuyCreditAccountCode) ? firstSetting.BuyCreditAccountCode.Trim() : "311031";

                writer.WriteStartElement("_FTrans");
                writer.WriteAttributeString("Acc", creditAcc);
                writer.WriteAttributeString("Det1", det1);
                writer.WriteAttributeString("Det4", det4);
                writer.WriteAttributeString("Cre", inv.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Desc", $" {{File No: {fileNo}}}{{Inv ID:{refN}}} {{HBL: {blNo}}} {{FNF: {fnfText}}}");
                writer.WriteAttributeString("ODesc", blNo);
                writer.WriteAttributeString("InvoiceDate", invDate);
                writer.WriteAttributeString("Curr", curr);
                writer.WriteAttributeString("RefN", refN);
                writer.WriteAttributeString("DueN", dueN);
                writer.WriteEndElement();
            }
            else
            {
                // ===== فروش (Sell) =====
                // الف) ردیف بدهکار کل فاکتور (مشتری / حساب‌های دریافتنی 110331)
                var firstSetting = nosaSettings[inv.InvoiceItems.First().ChargeTypeId];
                string debitAcc = !string.IsNullOrWhiteSpace(firstSetting.SellDebitAccountCode) ? firstSetting.SellDebitAccountCode.Trim() : "110331";

                writer.WriteStartElement("_FTrans");
                writer.WriteAttributeString("Acc", debitAcc);
                writer.WriteAttributeString("Det1", det1);
                writer.WriteAttributeString("Det4", det4);
                writer.WriteAttributeString("Deb", inv.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Desc", $"{{Serial: {dueN}}} {{File No: {fileNo}}} {{Inv ID:{refN}}} {{Inv Subject: {inv.InvoiceSubjectTitle ?? "Invoice"}}} {{FNF: {fnfText}}}");
                writer.WriteAttributeString("ODesc", blNo);
                writer.WriteAttributeString("InvoiceDate", invDate);
                writer.WriteAttributeString("Curr", curr);
                writer.WriteAttributeString("RefN", refN);
                writer.WriteAttributeString("DueN", dueN);
                writer.WriteEndElement();

                // ب) ردیف‌های بستانکار درآمد هر سرویس
                foreach (var item in inv.InvoiceItems)
                {
                    if (!nosaSettings.TryGetValue(item.ChargeTypeId, out var setting) || string.IsNullOrWhiteSpace(setting.SellCreditAccountCode))
                        throw new InvalidOperationException($"کد حساب معین فروش برای نوع خدمت '{item.ChargeType?.ChargeTypeName}' تعریف نشده است.");

                    writer.WriteStartElement("_FTrans");
                    writer.WriteAttributeString("Acc", setting.SellCreditAccountCode.Trim());
                    writer.WriteAttributeString("Det1", det1);
                    writer.WriteAttributeString("Det4", det4);
                    writer.WriteAttributeString("Cre", item.AmountAfterDiscount.ToString("0.##", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Desc", $"{{Serial: {dueN}}} {{File No: {fileNo}}} {{Inv ID:{refN}}} {{{item.ChargeType?.ChargeTypeName ?? item.Description}}} {{FNF: {fnfText}}}");
                    writer.WriteAttributeString("ODesc", blNo);
                    writer.WriteAttributeString("InvoiceDate", invDate);
                    writer.WriteAttributeString("Curr", curr);
                    writer.WriteAttributeString("RefN", refN);
                    writer.WriteAttributeString("DueN", dueN);
                    writer.WriteEndElement();
                }

                // ج) ردیف بستانکار مالیات ارزش افزوده فروش (کد پیش‌فرض 315001)
                if (inv.TaxAmount > 0)
                {
                    writer.WriteStartElement("_FTrans");
                    writer.WriteAttributeString("Acc", "315001");
                    writer.WriteAttributeString("Det1", det1);
                    writer.WriteAttributeString("Det4", det4);
                    writer.WriteAttributeString("Cre", inv.TaxAmount.ToString("0.##", CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("Desc", $"{{Serial: {dueN}}} {{File No: {fileNo}}} {{Inv ID:{refN}}} {{VAT}} {{FNF: {fnfText}}}");
                    writer.WriteAttributeString("ODesc", blNo);
                    writer.WriteAttributeString("InvoiceDate", invDate);
                    writer.WriteAttributeString("Curr", curr);
                    writer.WriteAttributeString("RefN", refN);
                    writer.WriteAttributeString("DueN", dueN);
                    writer.WriteEndElement();
                }
            }
        }
        public async Task<NosaXmlPageViewModel> GetNosaXmlPageViewModelAsync(CancellationToken cancellationToken = default)
        {
            var companies = await _db.Set<Company>()
                .AsNoTracking()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Person.PersonName
                })
                .ToListAsync(cancellationToken);

            var financeDirections = await _db.Set<FinanceDirection>()
                .AsNoTracking()
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.InvoiceContextName
                })
                .ToListAsync(cancellationToken);

            var invoiceFnfs = await _db.Set<InvoiceFNF>()
                .AsNoTracking()
                .Select(fnf => new SelectListItem
                {
                    Value = fnf.Id.ToString(),
                    Text = fnf.FNFName
                })
                .ToListAsync(cancellationToken);
            return new NosaXmlPageViewModel
            {
                Companies = companies,
                FinanceDirections = financeDirections,
                InvoiceFnfs = invoiceFnfs,
                Filter = new NosaXmlInvoiceFilterDto()
            };
        }
    }
}