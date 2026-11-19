using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.Finance;
using ShipRate.Application.Interfaces;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Application.ViewModels;
using ShipRate.Common.PublicClass;
using ShipRate.Domain.Entities.DetentionCalculation;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Infrastructure.Data;

namespace ShipRate.Application.Services.Finance
{
    public class SettlementService : ISettlementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public SettlementService(ApplicationDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<PagedResultDto<SettlementListItemDto>> SearchAsync(SettlementSearchDto input)
        {
            if (input.Page <= 0)
                input.Page = 1;

            if (input.PageSize is not (10 or 20 or 50))
                input.PageSize = 10;

            var query = _context.Settlements.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

            if (input.SettlementNo.HasValue)
            {
                query = query.Where(x => x.SettlementNo == input.SettlementNo.Value);
            }

            if (input.SettlementDateFrom.HasValue)
            {
                var from = input.SettlementDateFrom.Value.Date;
                query = query.Where(x => x.SettlementDate >= from);
            }

            if (input.SettlementDateTo.HasValue)
            {
                var to = input.SettlementDateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.SettlementDate <= to);
            }

            if (input.PartyId.HasValue)
            {
                query = query.Where(x => x.PartyId == input.PartyId.Value);
            }

            if (input.StatusId.HasValue)
            {
                query = query.Where(x => x.StatusId == input.StatusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.InvoiceNo))
            {
                var invoiceNo = input.InvoiceNo.Trim();

                query = query.Where(x =>
                    _context.SettlementItems.Any(si =>
                        si.SettlementId == x.Id &&
                        si.InvoiceId != null &&
                        _context.Invoices.Any(inv =>
                            inv.Id == si.InvoiceId &&
                            inv.InvoiceNo.Contains(invoiceNo))));
            }

            if (!string.IsNullOrWhiteSpace(input.PaymentDocumentNo))
            {
                var docNo = input.PaymentDocumentNo.Trim();

                query = query.Where(x =>
                    _context.SettlementItemPaymentAllocations.Any(pa =>
                        pa.SettlementId == x.Id &&
                        _context.PaymentDocuments.Any(pd =>
                            pd.Id == pa.PaymentDocumentId &&
                            pd.DocumentNo.Contains(docNo))));
            }
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.SettlementDate)
                .ThenByDescending(x => x.SettlementNo)
                .Skip((input.Page - 1) * input.PageSize)
                .Take(input.PageSize)
                .Select(x => new SettlementListItemDto
                {
                    Id = x.Id,
                    SettlementNo = x.SettlementNo,
                    SettlementDate = x.SettlementDate,
                    CompanyId = x.CompanyId,
                    CompanyName = _context.Companies
                        .Include(x => x.Person)
                        .Where(c => c.Id == x.CompanyId)
                        .Select(c => c.Person.PersonName)
                        .FirstOrDefault() ?? "",

                    PartyId = x.PartyId,
                    PartyName = _context.Persons
                        .Where(p => p.Id == x.PartyId)
                        .Select(p => p.PersonName)
                        .FirstOrDefault() ?? "",

                    CurrencyId = x.CurrencyId,
                    CurrencyName = _context.Currencies
                        .Where(cu => cu.Id == x.CurrencyId)
                        .Select(cu => cu.CurrencyName)
                        .FirstOrDefault() ?? "",

                    StatusId = x.StatusId,
                    StatusName = _context.SettlementStatuses
                        .Where(st => st.Id == x.StatusId)
                        .Select(st => st.SettlementStatusName)
                        .FirstOrDefault() ?? "",

                    TotalSettlementAmount = _context.SettlementItems
                        .Where(si => si.SettlementId == x.Id && !si.IsDeleted)
                        .Sum(si => (decimal?)si.Amount) ?? 0,

                    TotalAllocatedAmount = _context.SettlementItemPaymentAllocations
                        .Where(pa => pa.SettlementId == x.Id && !pa.IsDeleted)
                        .Sum(pa => (decimal?)pa.AllocatedAmount) ?? 0,

                    Remarks = x.Remarks,
                    CreateDate = x.CreatedAt,
                    CreateUserName = x.CreateUser.FullName
                })
                .ToListAsync();

            return new PagedResultDto<SettlementListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = input.Page,
                PageSize = input.PageSize
            };
        }
        public Task<SettlementHeaderCreateDto> GetCreateModelAsync()
        {
            return Task.FromResult(new SettlementHeaderCreateDto
            {
                SettlementDate = DateTime.Today,
                StatusId = 1
            });
        }
        public async Task<Guid> CreateHeaderAsync(SettlementHeaderCreateDto input, Guid userId)
        {
            var settlementNo = await GenerateSettlementNoAsync(input.CompanyId);

            var entity = new Settlement
            {
                Id = Guid.CreateVersion7(),
                CompanyId = input.CompanyId,
                SettlementNo = settlementNo,
                SettlementDate = input.SettlementDate,
                PartyId = input.PartyId,
                CurrencyId = input.CurrencyId,
                StatusId = 1,
                Remarks = input.Remarks ?? "",
                //IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = userId
            };

            _context.Settlements.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }
        public async Task<SettlementDetailsDto?> GetDetailsAsync(Guid id)
        {
            var header = await GetHeaderForEditAsync(id);

            if (header == null)
                return null;

            return new SettlementDetailsDto
            {
                Header = header,
                Items = await GetItemsAsync(id),
                PaymentAllocations = await GetPaymentAllocationsAsync(id)
            };
        }
        public async Task<SettlementHeaderEditDto?> GetHeaderForEditAsync(Guid id)
        {
            return await _context.Settlements
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new SettlementHeaderEditDto
                {
                    Id = x.Id,
                    SettlementNo = x.SettlementNo,
                    CompanyId = x.CompanyId,
                    SettlementDate = x.SettlementDate,
                    PartyId = x.PartyId,
                    PartyName = _context.Persons
                        .Where(p => p.Id == x.PartyId)
                        .Select(p => p.PersonName)
                        .FirstOrDefault(),
                    CurrencyId = x.CurrencyId,
                    StatusId = x.StatusId,
                    Remarks = x.Remarks
                })
                .FirstOrDefaultAsync();
        }
        public async Task UpdateHeaderAsync(SettlementHeaderEditDto input, Guid userId)
        {
            var entity = await _context.Settlements
                .FirstOrDefaultAsync(x => x.Id == input.Id && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Settlement not found.");

            entity.CompanyId = input.CompanyId;
            entity.SettlementDate = input.SettlementDate;
            entity.PartyId = input.PartyId;
            entity.CurrencyId = input.CurrencyId;
            //entity.StatusId = input.StatusId;
            entity.Remarks = input.Remarks ?? "";
            entity.UpdatedAt = DateTime.Now;
            entity.EditUserId = userId;
            await _context.SaveChangesAsync();
        }
        public async Task<List<SettlementItemDto>> GetItemsAsync(Guid settlementId)
        {
            return await _context.SettlementItems
                .AsNoTracking()
                .Where(x => x.SettlementId == settlementId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new SettlementItemDto
                {
                    Id = x.Id,
                    SettlementId = x.SettlementId,
                    InvoiceId = x.InvoiceId,
                    InvoiceNo = x.InvoiceId == null
                        ? null
                        : _context.Invoices
                            .Where(i => i.Id == x.InvoiceId)
                            .Select(i => i.InvoiceNo)
                            .FirstOrDefault(),

                    InvoiceItemId = x.InvoiceItemId,
                    ChargeId = x.ChargeId,
                    ChargeDescription = x.ChargeId == null
                        ? null
                        : _context.Charges
                            .Where(c => c.Id == x.ChargeId)
                            .Select(c => c.ChargeDescription)
                            .FirstOrDefault(),

                    Amount = x.Amount,
                    AmountInBaseCurrency = x.AmountInBaseCurrency,
                    BaseExchangeRate = x.BaseExchangeRate,
                    Remarks = x.Remarks
                })
                .ToListAsync();
        }
        public async Task<Guid> AddItemAsync(SettlementItemCreateDto input, Guid userId)
        {
            var settlementExists = await _context.Settlements
                .AnyAsync(x => x.Id == input.SettlementId && !x.IsDeleted);

            if (!settlementExists)
                throw new Exception("Settlement not found.");

            if (input.Amount <= 0)
                throw new Exception("Amount must be greater than zero.");

            if (input.InvoiceId == null && input.InvoiceItemId == null && input.ChargeId == null)
                throw new Exception("Invoice, Invoice Item or Charge is required.");

            var entity = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                SettlementId = input.SettlementId,
                InvoiceId = input.InvoiceId,
                InvoiceItemId = input.InvoiceItemId,
                ChargeId = input.ChargeId,
                Amount = input.Amount,
                AmountInBaseCurrency = input.AmountInBaseCurrency,
                BaseExchangeRate = input.BaseExchangeRate,
                Remarks = input.Remarks ?? "",
                //IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = userId
            };

            _context.SettlementItems.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }
        public async Task DeleteItemAsync(Guid id)
        {
            var entity = await _context.SettlementItems.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return;

            var hasAllocation = await _context.SettlementItemPaymentAllocations
                .AnyAsync(x => x.SettlementItemId == id && !x.IsDeleted);

            if (hasAllocation)
                throw new Exception("This item has payment allocations and cannot be deleted.");

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
        public async Task<List<PaymentAllocationDto>> GetPaymentAllocationsAsync(Guid settlementId)
        {
            return await _context.SettlementItemPaymentAllocations
                .AsNoTracking()
                .Where(x => x.SettlementId == settlementId && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new PaymentAllocationDto
                {
                    Id = x.Id,
                    SettlementId = x.SettlementId,
                    SettlementItemId = x.SettlementItemId,
                    PaymentDocumentId = x.PaymentDocumentId,

                    PaymentDocumentNo = _context.PaymentDocuments
                        .Where(p => p.Id == x.PaymentDocumentId)
                        .Select(p => p.DocumentNo)
                        .FirstOrDefault() ?? "",

                    PaymentDocumentDate = _context.PaymentDocuments
                        .Where(p => p.Id == x.PaymentDocumentId)
                        .Select(p => p.DocumentDate)
                        .FirstOrDefault(),

                    PaymentDocumentAmount = _context.PaymentDocuments
                        .Where(p => p.Id == x.PaymentDocumentId)
                        .Select(p => p.Amount)
                        .FirstOrDefault(),

                    AllocatedAmount = x.AllocatedAmount,
                    SettledAmountInItemCurrency = x.SettledAmountInItemCurrency,
                    ExchangeRateValue = x.ExchangeRateValue,
                    AllocatedAmountInBaseCurrency = x.AllocatedAmountInBaseCurrency,
                    Remarks = x.Remarks
                })
                .ToListAsync();
        }
        public async Task AddPaymentAllocationAsync(PaymentAllocationCreateDto input, Guid userId)
        {
            if (input.AllocatedAmount <= 0)
                throw new Exception("Allocated amount must be greater than zero.");

            var item = await _context.SettlementItems
                .FirstOrDefaultAsync(x => x.Id == input.SettlementItemId && !x.IsDeleted);

            if (item == null)
                throw new Exception("Settlement item not found.");

            if (item.SettlementId != input.SettlementId)
                throw new Exception("Invalid settlement item.");

            var payment = await _context.PaymentDocuments
                .FirstOrDefaultAsync(x => x.Id == input.PaymentDocumentId && !x.IsDeleted);

            if (payment == null)
                throw new Exception("Payment document not found.");

            var alreadyAllocated = await _context.SettlementItemPaymentAllocations
                .Where(x => x.PaymentDocumentId == input.PaymentDocumentId && !x.IsDeleted)
                .SumAsync(x => (decimal?)x.AllocatedAmount) ?? 0;

            if (alreadyAllocated + input.AllocatedAmount > payment.Amount)
                throw new Exception("Allocated amount is greater than available payment amount.");

            var itemRemaining = item.Amount -
                await _context.SettlementItemPaymentAllocations
                .Where(x => x.SettlementItemId == item.Id && !x.IsDeleted)
                .SumAsync(x => (decimal?)x.AllocatedAmount) ?? 0;

            if (input.AllocatedAmount > itemRemaining)
            {
                throw new Exception("Allocated amount exceeds remaining settlement item amount.");
            }



            var entity = new SettlementItemPaymentAllocation
            {
                Id = Guid.CreateVersion7(),
                SettlementId = input.SettlementId,
                SettlementItemId = input.SettlementItemId,
                PaymentDocumentId = input.PaymentDocumentId,
                AllocatedAmount = input.AllocatedAmount,
                SettledAmountInItemCurrency = input.SettledAmountInItemCurrency,
                ExchangeRateId = input.ExchangeRateId,
                ExchangeRateBaseId = input.ExchangeRateBaseId,
                ExchangeRateDate = input.ExchangeRateDate,
                ExchangeRateValue = input.ExchangeRateValue,
                AllocatedAmountInBaseCurrency = input.AllocatedAmountInBaseCurrency,
                Remarks = input.Remarks ?? "",
                //IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = userId
            };

            _context.SettlementItemPaymentAllocations.Add(entity);
            await _context.SaveChangesAsync();
        }
        public async Task DeletePaymentAllocationAsync(Guid id)
        {
            var entity = await _context.SettlementItemPaymentAllocations.FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return;

            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
        public async Task<List<DTOs.Finance.LookupDto>> SearchPartiesAsync(string term)
        {
            term = term?.Trim() ?? "";

            if (term.Length < 2)
                return new List<DTOs.Finance.LookupDto>();

            return await _context.Persons
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    (
                        x.PersonName.Contains(term) ||
                        x.PersonPersianName.Contains(term) ||
                        x.NationalID.Contains(term) ||
                        x.Mobile.Contains(term)
                    ))
                .OrderBy(x => x.PersonName)
                .Take(20)
                .Select(x => new LookupDto
                {
                    Id = x.Id.ToString(),
                    Text = x.PersonName
                })
                .ToListAsync();
        }
        public async Task<List<LookupDto>> SearchInvoicesAsync(Guid? partyId, string term)
        {
            term = term?.Trim() ?? "";

            var query = _context.Invoices
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (partyId.HasValue)
                query = query.Where(x => x.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(x => x.InvoiceNo.Contains(term));

            return await query
                .OrderByDescending(x => x.InvoiceDate)
                .Take(20)
                .Select(x => new LookupDto
                {
                    Id = x.Id.ToString(),
                    Text = x.InvoiceNo + " - " + x.TotalAmount
                })
                .ToListAsync();
        }
        public async Task<List<LookupDto>> SearchPaymentDocumentsAsync(Guid? partyId, string term)
        {
            term = term?.Trim() ?? "";

            var query = _context.PaymentDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (partyId.HasValue)
                query = query.Where(x => x.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(x => x.DocumentNo.Contains(term));

            return await query
                .OrderByDescending(x => x.DocumentDate)
                .Take(20)
                .Select(x => new LookupDto
                {
                    Id = x.Id.ToString(),
                    Text = x.DocumentNo + " - " + x.Amount
                })
                .ToListAsync();
        }
        public async Task<List<LookupDto>> SearchChargesAsync(Guid? partyId, string term)
        {
            term = term?.Trim() ?? "";

            var query = _context.Charges
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (partyId.HasValue)
                query = query.Where(x => x.PartyId == partyId.Value);

            if (!string.IsNullOrWhiteSpace(term))
                query = query.Where(x => x.ChargeDescription.Contains(term));

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .Select(x => new LookupDto
                {
                    Id = x.Id.ToString(),
                    Text = x.ChargeDescription + " - " + x.Amount
                })
                .ToListAsync();
        }
        private async Task<int> GenerateSettlementNoAsync(int companyId)
        {
            var lastNo = await _context.Settlements
                .Where(x => x.CompanyId == companyId)
                .MaxAsync(x => (int?)x.SettlementNo) ?? 0;

            return lastNo + 1;
        }
        public async Task<PaymentAllocationFormDto> GetPaymentAllocationForm(Guid settlementId)
        {
            var items = await _context.SettlementItems
                .Where(x => x.SettlementId == settlementId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Amount,
                    InvoiceNo = _context.Invoices
                        .Where(i => i.Id == x.InvoiceId)
                        .Select(i => i.InvoiceNo)
                        .FirstOrDefault(),

                    ChargeDescription = _context.Charges
                        .Where(c => c.Id == x.ChargeId)
                        .Select(c => c.ChargeDescription)
                        .FirstOrDefault(),

                    Allocated = _context.SettlementItemPaymentAllocations
                        .Where(p => p.SettlementItemId == x.Id && !p.IsDeleted)
                        .Sum(p => (decimal?)p.AllocatedAmount) ?? 0
                })
                .ToListAsync();

            var list = items.Select(x => new SettlementItemLookupDto
            {
                Id = x.Id,
                Description = (x.InvoiceNo ?? "") + " " + (x.ChargeDescription ?? ""),
                ItemAmount = x.Amount,
                AllocatedAmount = x.Allocated,
                RemainingAmount = x.Amount - x.Allocated
            }).ToList();

            return new PaymentAllocationFormDto
            {
                SettlementId = settlementId,
                SettlementItems = list,
                Input = new PaymentAllocationCreateDto
                {
                    SettlementId = settlementId,
                    ExchangeRateValue = 1
                }
            };
        }
        public async Task<List<JobSettlementGridRowDto>> GetJobSettlementsAsync(Guid jobId)
        {
            var query = await (
                from si in _context.SettlementItems
                join s in _context.Settlements on si.SettlementId equals s.Id

                join i0 in _context.Invoices on si.InvoiceId equals i0.Id into invoiceGroup
                from i in invoiceGroup.DefaultIfEmpty()

                    //join c0 in _context.Charges on si.ChargeId equals c0.Id into chargeGroup
                    //from c in chargeGroup.DefaultIfEmpty()

                join pa0 in _context.SettlementItemPaymentAllocations
                    .Where(x => !x.IsDeleted)
                    on si.Id equals pa0.SettlementItemId into allocationGroup
                from pa in allocationGroup.DefaultIfEmpty()

                join pd0 in _context.PaymentDocuments
                    on pa.PaymentDocumentId equals pd0.Id into paymentDocGroup
                from pd in paymentDocGroup.DefaultIfEmpty()

                where !si.IsDeleted
                      && !s.IsDeleted
                      && i != null
                      && i.JobId == jobId
                select new JobSettlementGridRowDto
                {
                    SettlementItemId = si.Id,
                    SettlementId = s.Id,
                    SettlementNo = s.SettlementNo,
                    SettlementDate = s.SettlementDate,
                    InvoiceNo = i != null ? i.InvoiceNo : "",
                    InvoiceSerial = i.Serial,
                    CompanyName = s.Company.CompanyAbb,
                    InvoiceDate = i.InvoiceDate,
                    InvoicePartyName = i.Party.PersonName,
                    SettlementUserFullName = s.CreateUser.FullName,
                    PaymentDocumentNo = pd != null ? pd.DocumentNo : "",
                    Amount = i.TotalAmount,
                    SettledAmount = pa != null ? pa.SettledAmountInItemCurrency : 0,
                    InvoiceDirectionName = i.FinanceDirection.InvoiceContextName,
                    DirectionId = i.FinanceDirectionId
                })
                .ToListAsync();
            return query;
        }
        public async Task<List<JobSettlementDetailDto>> GetSettlementItemDetailsAsync(Guid settlementItemId)
        {
            var result = await (
                from si in _context.SettlementItems

                join s in _context.Settlements
                    on si.SettlementId equals s.Id

                join i0 in _context.Invoices
                    on si.InvoiceId equals i0.Id into invoiceGroup
                from i in invoiceGroup.DefaultIfEmpty()

                join c0 in _context.Charges
                    on si.ChargeId equals c0.Id into chargeGroup
                from c in chargeGroup.DefaultIfEmpty()

                join pa0 in _context.SettlementItemPaymentAllocations.Where(x => !x.IsDeleted)
                    on si.Id equals pa0.SettlementItemId into paGroup
                from pa in paGroup.DefaultIfEmpty()

                from pd in _context.PaymentDocuments
                    .Where(x => pa != null
                                && x.Id == pa.PaymentDocumentId
                                && !x.IsDeleted)
                    .DefaultIfEmpty()

                where si.Id == settlementItemId
                      && !si.IsDeleted
                      && !s.IsDeleted

                select new JobSettlementDetailDto
                {
                    SettlementId = s.Id,
                    SettlementItemId = si.Id,
                    SettlementNo = s.SettlementNo.ToString(),
                    SettlementDate = s.SettlementDate,

                    InvoiceNo = i != null ? i.InvoiceNo : "",
                    ChargeName = c != null ? c.ChargeDescription : "",

                    ItemAmount = si.Amount,

                    ItemType = i != null ? "Invoice" : "Charge",
                    ItemTitle = i != null
                    ? i.InvoiceNo
                    : c != null
                        ? c.ChargeDescription
                        : "",


                    PaymentDocumentNo = pd != null ? pd.DocumentNo : "",

                    AllocatedAmount = pa != null ? pa.AllocatedAmount : 0,
                    SettledAmount = pa != null ? pa.SettledAmountInItemCurrency : 0,
                    ExchangeRate = pa != null ? pa.ExchangeRateValue : 0,
                    Remarks = pa != null ? pa.Remarks : ""
                })
                .ToListAsync();

            return result;
        }
        public async Task<OperationResult> ApproveSettlementAsync(Guid settlementId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            // ۱. دریافت رکورد تسویه به همراه تراکنش/وابستگی‌های مرتبط در صورت نیاز
            var settlement = await _context.Settlements
                .Include(s => s.Items) // در صورت وجود آیتم‌ها یا فاکتورهای متصل
                .FirstOrDefaultAsync(s => s.Id == settlementId, cancellationToken);

            if (settlement is null)
            {
                return OperationResult.Failure("Settlement Not Found!!!!");
            }

            // ۲. اعتبارسنجی وضعیت (فقط تسویه‌های Draft یا وضعیت 1 قابل Approve هستند)
            if (settlement.StatusId != 1) // یا SettlementStatus.Draft
            {
                return OperationResult.Failure("Only Draft Settlements Can Be Approved");
            }


            try
            {
                // ۴. تغییر وضعیت به Approved (2)
                settlement.StatusId = 2; // SettlementStatus.Approved
                //settlement.ApprovedAt = DateTime.Now;
                //settlement.ApprovedByUserId = currentUserId;

                // در صورت نیاز به ردیابی Auditing:
                settlement.UpdatedAt = DateTime.Now;
                settlement.EditUserId = currentUserId;

                // ۵. در صورت نیاز به به‌روزرسانی فاکتورها، تراکنش‌های بانکی یا Ledger
                /*
                foreach (var item in settlement.SettlementItems)
                {
                    // مثلاً تغییر وضعیت فاکتور مربوطه به Paid یا Settled
                }
                */

                await _context.SaveChangesAsync(cancellationToken);

                return OperationResult.Success("Settlement Approved Successfully");
            }
            catch (DbUpdateConcurrencyException)
            {
                return OperationResult.Failure("Concurrency Error");
            }
            catch (Exception ex)
            {
                // لاگ کردن خطا
                return OperationResult.Failure("An Error Occurred While Approving Settlement");
            }
        }
        public async Task<SettlementReceiptReportDto?> GetSettlementReceiptReportDataAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var settlement = await _context.Settlements
                .AsNoTracking()
                .Include(s => s.Company).ThenInclude(c => c.Person)
                .Include(s => s.Party)
                .Include(s => s.Currency)
                .Include(s => s.Status)
                .Include(s => s.Items).ThenInclude(i => i.Invoice)
                .Include(s => s.Items).ThenInclude(i => i.Charge)
                .Include(s => s.Items).ThenInclude(i => i.Detention)
                .Include(s => s.Items).ThenInclude(i => i.PaymentAllocations)
                .Include(s => s.PaymentAllocations).ThenInclude(p => p.PaymentDocument).ThenInclude(x => x.PaymentMethod)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

            if (settlement == null)
                return null;

            var totalItemsAmount = settlement.Items.Sum(x => x.Amount);
            var totalAllocatedAmount = settlement.PaymentAllocations.Sum(x => x.AllocatedAmount);
            var totalSettledInItemCurrency = settlement.PaymentAllocations.Sum(x => x.SettledAmountInItemCurrency);

            var dto = new SettlementReceiptReportDto
            {
                CompanyId = settlement.CompanyId,
                CompanyName = settlement.Company?.Person?.PersonName ?? "شرکت حمل و نقل بین‌المللی",
                SettlementNo = settlement.SettlementNo,
                SettlementDate = settlement.SettlementDate.ToString("yyyy/MM/dd"),
                PartyName = settlement.Party?.PersonName ?? "-",
                CurrencyName = settlement.Currency?.CurrencyCode ?? "-",
                StatusName = settlement.Status?.SettlementStatusName ?? "-",
                Remarks = settlement.Remarks ?? string.Empty,
                TotalAmount = totalItemsAmount,
                TotalAllocated = totalAllocatedAmount,
                RemainingAmount = totalItemsAmount - totalSettledInItemCurrency,
                Items = settlement.Items.Select((x, idx) => new SettlementReceiptItemDto
                {
                    RowIndex = idx + 1,
                    InvoiceNo = x.Invoice?.InvoiceNo ?? x.Detention?.DetentionNo.ToString() ?? "-",
                    //ChargeTitle = x.Charge?.ChargeDescription ?? (!string.IsNullOrEmpty(x.Remarks) ? x.Remarks : "بابت فاکتور / هزینه"),
                    ChargeTitle = x.Invoice != null ? "فاکتور" : x.Detention != null ? "دیتنشن" : "-",
                    Amount = x.Amount,
                    CoveredAmount = x.CoveredAmountInItemCurrency,
                    RemainingAmount = x.RemainingAmountInItemCurrency,
                    Remarks = x.Remarks ?? string.Empty
                }).ToList(),
                Payments = settlement.PaymentAllocations.Select((p, idx) => new SettlementReceiptPaymentDto
                {
                    RowIndex = idx + 1,
                    DocumentNo = p.PaymentDocument?.DocumentNo ?? "-",
                    DocumentDate = p.PaymentDocument?.DocumentDate.ToString("yyyy/MM/dd") ?? "-",
                    PaymentMethod = p.PaymentDocument?.PaymentMethod.PaymentMethodName ?? "سند مالی / حواله",
                    AllocatedAmount = p.AllocatedAmount,
                    SettledInItemCurrency = p.SettledAmountInItemCurrency,
                    ExchangeRate = p.ExchangeRateValue,
                    Remarks = p.Remarks ?? string.Empty
                }).ToList()
            };

            return dto;
        }
        public async Task<List<InvoiceSettlementDto>> GetInvoicesForSettlementAsync(List<Guid> invoiceIds)
        {
            // استفاده از Projection برای واکشی فقط ستون‌های مورد نیاز جهت افزایش سرعت
            return await _context.Invoices
                .Where(x => invoiceIds.Contains(x.Id))
                .Select(x => new InvoiceSettlementDto
                {
                    Id = x.Id,
                    InvoiceNo = x.SerialDisplay,
                    TotalAmount = x.TotalAmount,
                    // اگر RemainingAmount فیلد محاسبه شده در دیتابیس نیست، 
                    // در اینجا می‌توانید از محاسبه دستی استفاده کنید: (x.TotalAmount - x.PaidAmount)
                    RemainingAmount = x.TotalAmount,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency.CurrencyCode, // فرض بر این است که Navigation Property برقرار است
                    PartyId = x.PartyId.Value,
                    PartyName = x.Party.PersonName,
                    CompanyId = x.CompanyId
                })
                .ToListAsync();
        }
        public async Task<List<DetentionSettlementDto>> GetDetentionsForSettlementAsync(List<Guid> detentionIds)
        {
            // استفاده از Projection برای واکشی فقط ستون‌های مورد نیاز جهت افزایش سرعت
            return await _context.Detentions
                .Where(x => detentionIds.Contains(x.Id))
                .Select(x => new DetentionSettlementDto
                {
                    Id = x.Id,
                    DetentionNo = x.DetentionNo.ToString(),
                    TotalAmount = x.FinalPayableAmount,
                    // اگر RemainingAmount فیلد محاسبه شده در دیتابیس نیست، 
                    // در اینجا می‌توانید از محاسبه دستی استفاده کنید: (x.TotalAmount - x.PaidAmount)
                    RemainingAmount = x.FinalPayableAmount - x.SettledAmount,
                    CurrencyId = x.FinalCurrencyId,
                    CurrencyCode = x.PaymentCurrency.CurrencyCode, // فرض بر این است که Navigation Property برقرار است
                    PartyId = x.PartyId.Value,
                    PartyName = x.Party.PersonName,
                    CompanyId = x.CompanyId
                })
                .ToListAsync();
        }
        public async Task SyncSettlementBalancesAsync(IEnumerable<Guid> invoiceIds, IEnumerable<Guid> paymentDocIds, IEnumerable<Guid> detentionIds)
        {
            // ۱. به‌روزرسانی فاکتورها
            if (invoiceIds.Any())
            {
                var invoices = await _context.Invoices
                    .Where(i => invoiceIds.Contains(i.Id))
                    .ToListAsync();

                var settledSums = await _context.SettlementItemPaymentAllocations
                    .Where(a => a.SettlementItem.InvoiceId != null
                             && invoiceIds.Contains(a.SettlementItem.InvoiceId.Value)
                             && a.Settlement.StatusId != 10)
                    .GroupBy(a => a.SettlementItem.InvoiceId.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(x => x.SettledAmountInItemCurrency) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                foreach (var inv in invoices)
                {
                    inv.SettledAmount = settledSums.GetValueOrDefault(inv.Id, 0m);
                    inv.InvoiceStatusId = inv.SettledAmount >= inv.TotalAmount ? 5 : (inv.SettledAmount > 0 ? 4 : inv.InvoiceStatusId);
                }
            }

            // ۲. به‌روزرسانی Detentionها (جدید)
            if (detentionIds.Any())
            {
                var detentions = await _context.Detentions // فرض بر وجود جدول Detentions
                    .Where(d => detentionIds.Contains(d.Id))
                    .ToListAsync();

                var settledSums = await _context.SettlementItemPaymentAllocations
                    .Where(a => a.SettlementItem.DetentionId != null // فرض بر وجود این فیلد
                             && detentionIds.Contains(a.SettlementItem.DetentionId.Value)
                             && a.Settlement.StatusId != 10)
                    .GroupBy(a => a.SettlementItem.DetentionId.Value)
                    .Select(g => new { Id = g.Key, Total = g.Sum(x => x.SettledAmountInItemCurrency) })
                    .ToDictionaryAsync(x => x.Id, x => x.Total);

                foreach (var det in detentions)
                {
                    det.SettledAmount = settledSums.GetValueOrDefault(det.Id, 0m);
                }
            }

            // ۳. به‌روزرسانی اسناد پرداخت
            if (paymentDocIds.Any())
            {
                var paymentDocs = await _context.PaymentDocuments
                    .Where(p => paymentDocIds.Contains(p.Id))
                    .ToListAsync();

                var allocatedSums = await _context.SettlementItemPaymentAllocations
                    .Where(a => paymentDocIds.Contains(a.PaymentDocumentId)
                             && a.Settlement.StatusId != 10)
                    .GroupBy(a => a.PaymentDocumentId)
                    .Select(g => new { DocId = g.Key, TotalAllocated = g.Sum(x => x.AllocatedAmount) })
                    .ToDictionaryAsync(x => x.DocId, x => x.TotalAllocated);

                foreach (var doc in paymentDocs)
                {
                    doc.AllocatedAmount = allocatedSums.GetValueOrDefault(doc.Id, 0m);
                }
            }

            await _context.SaveChangesAsync();
        }
        //Reports Repository
        public async Task<PagedResultDto<SettlementListItemDto_Reports>> SearchAsync_Reports(SettlementSearchDto_Reports searchModel)
        {
            if (searchModel.Page <= 0)
                searchModel.Page = 1;

            if (searchModel.PageSize is not (10 or 20 or 50))
                searchModel.PageSize = 10;

            var query = _context.SettlementItemPaymentAllocations
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            searchModel ??= new SettlementSearchDto_Reports();
            //Search
            //1
            if (searchModel.CompanyId.HasValue) query = query.Where(x => x.Settlement.CompanyId == searchModel.CompanyId.Value);
            //2
            if (searchModel.SettlementNo.HasValue) query = query.Where(x => x.Settlement.SettlementNo == searchModel.SettlementNo.Value);
            //3
            if (searchModel.SettlementDateFrom.HasValue) query = query.Where(x => x.Settlement.SettlementDate >= searchModel.SettlementDateFrom.Value);
            //4
            if (searchModel.SettlementDateTo.HasValue) query = query.Where(x => x.Settlement.SettlementDate <= searchModel.SettlementDateTo.Value);
            //5
            if (!string.IsNullOrWhiteSpace(searchModel.SettlementDateShamsiFrom))
            {
                DateTime? miladi = searchModel.SettlementDateShamsiFrom.ToGregorianDateTime();
                if (miladi.HasValue)
                {
                    query = query.Where(x => x.Settlement.CreatedAt >= miladi.Value.Date);
                }
            }
            //6
            if (!string.IsNullOrWhiteSpace(searchModel.SettlementDateShamsiTo))
            {
                DateTime? miladi = searchModel.SettlementDateShamsiTo.ToGregorianDateTime();
                if (miladi.HasValue)
                {
                    query = query.Where(x => x.Settlement.CreatedAt <= miladi.Value.Date);
                }
            }
            //7
            if (searchModel.PartyId.HasValue) query = query.Where(x => x.Settlement.PartyId == searchModel.PartyId);
            //8
            if (searchModel.CurrencyId.HasValue) query = query.Where(x => x.Settlement.CurrencyId == searchModel.CurrencyId.Value);
            //9
            if (!string.IsNullOrWhiteSpace(searchModel.Remarks)) query = query.Where(x => x.Settlement.Remarks.Contains(searchModel.Remarks));
            //10
            if (searchModel.SettlementStatusId.HasValue) query = query.Where(x => x.Settlement.StatusId == searchModel.SettlementStatusId.Value);
            //11
            if (searchModel.SettlemenetCreateAtFrom.HasValue) query = query.Where(x => x.Settlement.CreatedAt >= searchModel.SettlemenetCreateAtFrom.Value);
            //12
            if (searchModel.SettlemenetCreateAtTo.HasValue) query = query.Where(x => x.Settlement.CreatedAt <= searchModel.SettlemenetCreateAtTo.Value);
            //13
            if (searchModel.SettlemenetUpdateAtFrom.HasValue) query = query.Where(x => x.Settlement.UpdatedAt >= searchModel.SettlemenetUpdateAtFrom.Value);
            //14
            if (searchModel.SettlemenetUpdateAtTo.HasValue) query = query.Where(x => x.Settlement.UpdatedAt <= searchModel.SettlemenetUpdateAtTo.Value);
            //15
            if (!string.IsNullOrWhiteSpace(searchModel.CreateUserName)) query = query.Where(x => x.Settlement.CreateUser.FullName.Contains(searchModel.CreateUserName));
            //16
            if (!string.IsNullOrWhiteSpace(searchModel.UpdateUserName)) query = query.Where(x => x.Settlement.EditUser.FullName.Contains(searchModel.UpdateUserName));
            //17
            if (!string.IsNullOrWhiteSpace(searchModel.BLNo)) query = query.Where(x => x.Settlement.BL.BLNo.Contains(searchModel.BLNo));
            //18
            if (!string.IsNullOrWhiteSpace(searchModel.MasterBLNo)) query = query.Where(x => x.Settlement.BL.MasterBLNo.Contains(searchModel.MasterBLNo));
            //19
            if (!string.IsNullOrWhiteSpace(searchModel.BookingNo)) query = query.Where(x => x.Settlement.Booking.BookingNo.Contains(searchModel.BookingNo));
            //20
            if (!string.IsNullOrWhiteSpace(searchModel.InvoiceNo)) query = query.Where(x => x.SettlementItem.Invoice.InvoiceNo.Contains(searchModel.InvoiceNo));
            //21
            if (searchModel.InvCustomerId.HasValue) query = query.Where(x => x.SettlementItem.Invoice.PartyId == searchModel.InvCustomerId);
            //22
            if (searchModel.InvoiceSerial.HasValue) query = query.Where(x => x.SettlementItem.Invoice.Serial == searchModel.InvoiceSerial.Value);
            //23
            if (searchModel.InvoiceFinanceDirectionId.HasValue) query = query.Where(x => x.SettlementItem.Invoice.FinanceDirectionId == searchModel.InvoiceFinanceDirectionId.Value);
            //24
            if (searchModel.DetentionNo.HasValue) query = query.Where(x => x.SettlementItem.Detention.DetentionNo == searchModel.DetentionNo);
            //25
            if (searchModel.DetentionFinanceDirectionId.HasValue) query = query.Where(x => x.SettlementItem.Detention.FinanceDirectionId == searchModel.DetentionFinanceDirectionId.Value);
            //26
            if (!string.IsNullOrWhiteSpace(searchModel.PaymentDocumentNo)) query = query.Where(x => x.PaymentDocument.DocumentNo.Contains(searchModel.PaymentDocumentNo));
            //27
            if (searchModel.PaymentAmount.HasValue) query = query.Where(x => x.PaymentDocument.Amount == searchModel.PaymentAmount);
            //28
            if (searchModel.PayCustomerId.HasValue) query = query.Where(x => x.PaymentDocument.PartyId == searchModel.PayCustomerId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Settlement.SettlementDate)
                .ThenByDescending(x => x.Settlement.SettlementNo)
                .Skip((searchModel.Page - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .Select(x => new SettlementListItemDto_Reports
                {
                    Id = x.Id,
                    SettlementNo = x.Settlement.SettlementNo,
                    SettlementDate = x.Settlement.SettlementDate,
                    CompanyId = x.Settlement.CompanyId,
                    CompanyName = _context.Companies
                        .Include(x => x.Person)
                        .Where(c => c.Id == x.Settlement.CompanyId)
                        .Select(c => c.Person.PersonName)
                        .FirstOrDefault() ?? "",

                    PartyId = x.Settlement.PartyId,
                    PartyName = _context.Persons
                        .Where(p => p.Id == x.Settlement.PartyId)
                        .Select(p => p.PersonName)
                        .FirstOrDefault() ?? "",

                    CurrencyId = x.Settlement.CurrencyId,
                    CurrencyName = _context.Currencies
                        .Where(cu => cu.Id == x.Settlement.CurrencyId)
                        .Select(cu => cu.CurrencyName)
                        .FirstOrDefault() ?? "",

                    StatusId = x.Settlement.StatusId,
                    StatusName = _context.SettlementStatuses
                        .Where(st => st.Id == x.Settlement.StatusId)
                        .Select(st => st.SettlementStatusName)
                        .FirstOrDefault() ?? "",


                    Remarks = x.Remarks,
                    CreateDate = x.CreatedAt,
                    SettlementDateShamsi = ConvertDate.ToPersianDate(x.CreatedAt),
                    CreateUserName = x.Settlement.CreateUser.FullName,
                    UpdateDate = x.UpdatedAt,
                    UpdateUserName = x.Settlement.EditUser.FullName,
                    BLId = x.Settlement.BLId,
                    BLNo = x.Settlement.BL.BLNo,
                    MBLNo = x.Settlement.BL.MasterBLNo,
                    BookingId = x.Settlement.BookingId,
                    BookingNo = x.Settlement.Booking.BookingNo,
                    //-----settlements Item-----
                    InvoiceNo = x.SettlementItem.Invoice.InvoiceNo,
                    InvoiceParty = x.SettlementItem.Invoice.Party.PersonName,
                    InvoiceSerial = x.SettlementItem.Invoice.Serial,
                    InvoiceFinanceDirectionText = x.SettlementItem.Invoice.FinanceDirection.FinanceDirectionName,
                    DetentionNo = x.SettlementItem.Detention.DetentionNo + "",
                    DetentionFinanceDirectionText = x.SettlementItem.Detention.FinanceDirection.FinanceDirectionName,
                    //-----Settlement Item Payment Allocations-----
                    DocumentNo = x.PaymentDocument.DocumentNo,
                    Amount = x.PaymentDocument.Amount,
                    PaymentParty = x.PaymentDocument.Party.PersonName,
                    AllocatedAmount = x.AllocatedAmount,
                    SettledAmountInItemCurrency = x.AllocatedAmountInBaseCurrency,
                    ExchangeRateValue = x.ExchangeRateValue,
                    AllocatedRemarks = x.Remarks
                })
                .ToListAsync();

            return new PagedResultDto<SettlementListItemDto_Reports>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = searchModel.Page,
                PageSize = searchModel.PageSize
            };
        }
        public async Task<ReleaseDepositsResult> ReleaseDepositsOnFinalDetentionAsync(Guid blId, Guid executedByUserId, DateTime? settlementDate = null, string? remarks = null, CancellationToken ct = default)
        {
            settlementDate ??= DateTime.Now;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // 1) کنترل وجود Detention نهایی برای این BL
            var finalDetentionExists = await _context.Set<Detention>()
                .AnyAsync(x => x.BLId == blId && x.IsFinal && !x.IsDeleted, ct);

            if (!finalDetentionExists)
                throw new InvalidOperationException("برای این بارنامه هنوز Detention نهایی (IsFinal=1) ثبت نشده است.");

            var allocationsToReverse = await _context.Set<SettlementItemPaymentAllocation>()
                .Include(a => a.SettlementItem)
                    .ThenInclude(si => si.Settlement)
                .Include(a => a.PaymentDocument)
                .Include(a => a.SettlementItem)
                    .ThenInclude(si => si.Invoice)
                .Where(a =>
                    a.SettlementItem.Settlement.BLId == blId &&
                    a.SettlementItem.InvoiceId != null &&
                    a.SettlementItem.Invoice != null &&
                    a.SettlementItem.Invoice.FinanceDirectionId == 1 &&
                    (a.SettlementItem.Invoice.InvoiceSubjectId == 10 ||
                     a.SettlementItem.Invoice.InvoiceSubjectId == 11) &&
                    a.AllocatedAmount > 0
                )
                .ToListAsync(ct);

            if (allocationsToReverse.Count == 0)
            {
                return new ReleaseDepositsResult(
                    Guid.Empty,
                    0,
                    0,
                    "هیچ تخصیص/تسویه‌ای برای سپرده‌های این بارنامه جهت آزادسازی یافت نشد."
                );
            }

            var paymentDocIds = allocationsToReverse.Select(x => x.PaymentDocumentId).Distinct().ToList();

            var alreadyReversed = await _context.Set<SettlementItemPaymentAllocation>()
                .Include(x => x.Settlement)
                .AnyAsync(x =>
                    x.Settlement.BLId == blId &&
                    paymentDocIds.Contains(x.PaymentDocumentId) &&
                    x.AllocatedAmount < 0, ct);

            if (alreadyReversed)
                throw new InvalidOperationException("برای این بارنامه قبلاً عملیات آزادسازی (Reverse Allocation) انجام شده است.");

            var partyId = allocationsToReverse
                .Select(x => x.SettlementItem?.Settlement?.PartyId)
                .FirstOrDefault(x => x != null);

            var baseCurrencyId = allocationsToReverse
                .Select(x => x.SettlementItem.Settlement.CurrencyId)
                .First();

            //var nextNo = (await _context.Set<Settlement>()
            //    .Where(s => s.CompanyId == allocationsToReverse.First().SettlementItem.Settlement.CompanyId)
            //    .MaxAsync(s => (int?)s.SettlementNo, ct) ?? 0) + 1;

            var releaseSettlement = new Settlement
            {
                Id = Guid.CreateVersion7(),
                CompanyId = allocationsToReverse.First().SettlementItem.Settlement.CompanyId,
                SettlementDate = settlementDate.Value,
                PartyId = partyId,
                BLId = blId,
                CurrencyId = baseCurrencyId,
                Remarks = string.IsNullOrWhiteSpace(remarks)
                    ? "آزادسازی سپرده نقدی/غیرنقدی پس از Detention نهایی"
                    : remarks,
                StatusId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = executedByUserId
            };

            _context.Add(releaseSettlement);
            var totalSettledInItemCurrency = allocationsToReverse.Sum(x => x.SettledAmountInItemCurrency);

            var releaseItem = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                SettlementId = releaseSettlement.Id,
                InvoiceId = null,
                InvoiceItemId = null,
                DetentionId = null,
                ChargeId = null,
                Amount = -totalSettledInItemCurrency,
                Remarks = "آزادسازی سپرده ها",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = executedByUserId
            };
            releaseSettlement.Items.Add(releaseItem);

            foreach (var oldAlloc in allocationsToReverse)
            {
                var reversal = new SettlementItemPaymentAllocation
                {
                    Id = Guid.CreateVersion7(),
                    SettlementId = releaseSettlement.Id,
                    SettlementItemId = releaseItem.Id,
                    PaymentDocumentId = oldAlloc.PaymentDocumentId,
                    AllocatedAmount = -oldAlloc.AllocatedAmount,
                    SettledAmountInItemCurrency = -oldAlloc.SettledAmountInItemCurrency,

                    ExchangeRateId = oldAlloc.ExchangeRateId,
                    ExchangeRateBaseId = oldAlloc.ExchangeRateBaseId,
                    ExchangeRateDate = oldAlloc.ExchangeRateDate,
                    ExchangeRateValue = oldAlloc.ExchangeRateValue,

                    AllocatedAmountInBaseCurrency = oldAlloc.AllocatedAmountInBaseCurrency.HasValue
                        ? -oldAlloc.AllocatedAmountInBaseCurrency.Value
                        : null,

                    Remarks = $"آزادسازی سپرده",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreateUserId = executedByUserId
                };
                releaseSettlement.PaymentAllocations.Add(reversal);
                oldAlloc.PaymentDocument.AllocatedAmount += reversal.AllocatedAmount;
                // محافظت: AllocatedAmount منفی نشود
                if (oldAlloc.PaymentDocument.AllocatedAmount < 0)
                    throw new InvalidOperationException($"AllocatedAmount سند پرداخت {oldAlloc.PaymentDocumentId} منفی شد؛ داده‌های قبلی یا منطق محاسبه را بررسی کنید.");
            }
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new ReleaseDepositsResult(
                releaseSettlement.Id,
                allocationsToReverse.Count,
                allocationsToReverse.Sum(x => x.AllocatedAmount),
                "آزادسازی با موفقیت انجام شد"
            );
        }
        public async Task<ReleaseDepositsResult> ReleaseDepositByPaymentDocumentAsync(Guid paymentDocumentId, Guid executedByUserId, DateTime? settlementDate = null, string? remarks = null, CancellationToken ct = default)
        {
            settlementDate ??= DateTime.Now;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // 1) واکشی سند پرداخت به همراه تمام تخصیص‌های قبلی مرتبط با فاکتورهای سپرده
            var paymentDoc = await _context.Set<PaymentDocument>()
                .FirstOrDefaultAsync(p => p.Id == paymentDocumentId && !p.IsDeleted, ct);
            if (paymentDoc == null) throw new InvalidOperationException("Payment document not found.");
            var depositAllocations = await _context.Set<SettlementItemPaymentAllocation>()
                .Include(a => a.SettlementItem)
                    .ThenInclude(si => si.Settlement)
                .Include(a => a.SettlementItem)
                    .ThenInclude(si => si.Invoice)
                .Where(a =>
                    a.PaymentDocumentId == paymentDocumentId &&
                    !a.IsDeleted &&
                    a.AllocatedAmount > 0 &&
                    a.SettlementItem.InvoiceId != null &&
                    a.SettlementItem.Invoice != null &&
                    a.SettlementItem.Invoice.FinanceDirectionId == 1 &&
                    (a.SettlementItem.Invoice.InvoiceSubjectId == 10 ||
                     a.SettlementItem.Invoice.InvoiceSubjectId == 11)
                )
                .ToListAsync(ct);

            if (depositAllocations.Count == 0) throw new InvalidOperationException("No deposit settlement allocations found for this payment document.");
            var blId = depositAllocations
                .Select(a => a.SettlementItem.Settlement.BLId)
                .FirstOrDefault(id => id.HasValue);

            if (!blId.HasValue)
                throw new InvalidOperationException("Could not associate the deposit document with a valid Bill of Lading (BLId).");

            // 4) کنترل وجود Detention نهایی برای این بارنامه
            var finalDetentionExists = await _context.Set<Detention>()
                .AnyAsync(x => x.BLId == blId.Value && x.IsFinal && !x.IsDeleted, ct);

            if (!finalDetentionExists)
                throw new InvalidOperationException("Final detention (IsFinal=1) has not been registered yet for this BL.");

            // 5) کنترل آزادسازی‌های قبلی این سند روی همین بارنامه و محاسبه خالص مانده مسدود
            var priorReversals = await _context.Set<SettlementItemPaymentAllocation>()
                .Include(a => a.Settlement)
                .Where(a =>
                    a.PaymentDocumentId == paymentDocumentId &&
                    a.Settlement.BLId == blId.Value &&
                    !a.IsDeleted &&
                    a.AllocatedAmount < 0
                )
                .ToListAsync(ct);

            decimal totalPositiveAllocated = depositAllocations.Sum(a => a.AllocatedAmount);
            decimal totalAlreadyReversed = Math.Abs(priorReversals.Sum(a => a.AllocatedAmount));
            decimal netActiveDeposit = totalPositiveAllocated - totalAlreadyReversed;

            if (netActiveDeposit <= 0)
                throw new InvalidOperationException("The deposit on this payment document has already been fully released or reversed.");

            // 6) ساخت سند تسویه آزادسازی
            var firstAlloc = depositAllocations.First();
            var companyId = firstAlloc.SettlementItem.Settlement.CompanyId;
            var partyId = paymentDoc.PartyId ?? firstAlloc.SettlementItem.Settlement.PartyId;
            var currencyId = paymentDoc.CurrencyId;

            var releaseSettlement = new Settlement
            {
                Id = Guid.CreateVersion7(),
                CompanyId = companyId,
                SettlementDate = settlementDate.Value,
                PartyId = partyId,
                BLId = blId.Value,
                CurrencyId = currencyId,
                Remarks = string.IsNullOrWhiteSpace(remarks)
                    ? $"آزادسازی سپرده سند پرداخت {paymentDoc.DocumentNo} پس از Detention نهایی"
                    : remarks,
                StatusId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = executedByUserId
            };
            _context.Add(releaseSettlement);

            // محاسبه متناظر مبلغ به ارز ردیف
            decimal totalSettledInItemCurrency = depositAllocations.Sum(x => x.SettledAmountInItemCurrency) -
                                                Math.Abs(priorReversals.Sum(x => x.SettledAmountInItemCurrency));

            var releaseItem = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                SettlementId = releaseSettlement.Id,
                InvoiceId = null,
                InvoiceItemId = null,
                DetentionId = null,
                ChargeId = null,
                Amount = -totalSettledInItemCurrency,
                Remarks = $"آزادسازی سپرده سند پرداخت {paymentDoc.DocumentNo}",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = executedByUserId
            };
            releaseSettlement.Items.Add(releaseItem);

            // 7) ثبت Reversal به تفکیک ردیف‌های تخصیص اولیه
            decimal remainingToRelease = netActiveDeposit;
            int reversedRowsCount = 0;

            foreach (var oldAlloc in depositAllocations)
            {
                if (remainingToRelease <= 0)
                    break;

                // سهم این ردیف چقدر مانده است
                decimal allocReversedSoFar = Math.Abs(priorReversals
                    .Where(r => r.PaymentDocumentId == oldAlloc.PaymentDocumentId)
                    .Sum(r => r.AllocatedAmount));

                decimal availableToReverseInThisAlloc = oldAlloc.AllocatedAmount - allocReversedSoFar;
                if (availableToReverseInThisAlloc <= 0)
                    continue;

                decimal amountToReverse = Math.Min(availableToReverseInThisAlloc, remainingToRelease);

                // نسبت تسویه در ارز آیتم
                decimal itemCurrencyToReverse = oldAlloc.AllocatedAmount != 0
                    ? (amountToReverse / oldAlloc.AllocatedAmount) * oldAlloc.SettledAmountInItemCurrency
                    : amountToReverse;

                var reversal = new SettlementItemPaymentAllocation
                {
                    Id = Guid.CreateVersion7(),
                    SettlementId = releaseSettlement.Id,
                    SettlementItemId = releaseItem.Id,
                    PaymentDocumentId = paymentDocumentId,
                    AllocatedAmount = -amountToReverse,
                    SettledAmountInItemCurrency = -itemCurrencyToReverse,

                    ExchangeRateId = oldAlloc.ExchangeRateId,
                    ExchangeRateBaseId = oldAlloc.ExchangeRateBaseId,
                    ExchangeRateDate = oldAlloc.ExchangeRateDate,
                    ExchangeRateValue = oldAlloc.ExchangeRateValue,

                    AllocatedAmountInBaseCurrency = oldAlloc.AllocatedAmountInBaseCurrency.HasValue && oldAlloc.AllocatedAmount != 0
                        ? -(amountToReverse / oldAlloc.AllocatedAmount) * oldAlloc.AllocatedAmountInBaseCurrency.Value
                        : null,

                    Remarks = $"آزادسازی سپرده سند {paymentDoc.DocumentNo}",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreateUserId = executedByUserId
                };

                releaseSettlement.PaymentAllocations.Add(reversal);

                // آزادسازی مانده در سند پرداخت اصلی
                paymentDoc.AllocatedAmount -= amountToReverse;
                remainingToRelease -= amountToReverse;
                reversedRowsCount++;
            }

            // کنترل ایمنی: AllocatedAmount نباید منفی شود
            if (paymentDoc.AllocatedAmount < 0)
                throw new InvalidOperationException($"AllocatedAmount on payment document {paymentDocumentId} became negative ({paymentDoc.AllocatedAmount}). Please verify previous allocation logs.");

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new ReleaseDepositsResult(
                releaseSettlement.Id,
                reversedRowsCount,
                netActiveDeposit,
                "آزادسازی با موفقیت انجام شد"
            );
        }
        public async Task<Guid> SettleNegativeFinalDetentionByLIFOReversalAsync(Guid finalDetentionId, Guid currentUserId, DateTime? settlementDate = null, string? remarks = null, CancellationToken ct = default)
        {
            settlementDate ??= DateTime.Now;
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            var detention = await _context.Set<Detention>()
                .Include(d => d.BL)
                .FirstOrDefaultAsync(d => d.Id == finalDetentionId, ct);

            if (detention == null) throw new InvalidOperationException("Detention record not found.");

            if (!detention.IsFinal) throw new InvalidOperationException("This operation is only permitted for final detentions (IsFinal=true).");

            if (detention.FinalPayableAmount >= 0) throw new InvalidOperationException("Detention amount is not negative; no payment reversal is required.");

            var targetBlId = detention.BLId ?? detention.BL?.Id;
            if (!targetBlId.HasValue || targetBlId == Guid.Empty) throw new InvalidOperationException("The detention record is not linked to any valid Bill of Lading (BL).");

            // 2. Find all Detention IDs belonging to this BL (covers single or multiple interim detentions)
            var targetDetentionIds = await _context.Set<Detention>()
                .Where(d => d.BLId == targetBlId.Value)
                .Select(d => d.Id)
                .ToListAsync(ct);

            decimal remainingToReverse = Math.Abs(detention.FinalPayableAmount);

            var previousAllocations = await _context.Set<SettlementItemPaymentAllocation>()
                .Include(a => a.SettlementItem)
                    .ThenInclude(si => si.Settlement)
                .Include(a => a.PaymentDocument)
                .Where(a =>
                    a.SettlementItem.DetentionId.HasValue &&
                    targetDetentionIds.Contains(a.SettlementItem.DetentionId.Value) &&
                    a.AllocatedAmount > 0)
                .OrderByDescending(a => a.SettlementItem.Settlement.SettlementDate)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            decimal totalAvailableToReverse = previousAllocations.Sum(a => a.AllocatedAmount);
            if (totalAvailableToReverse < remainingToReverse)
            {
                throw new InvalidOperationException($"Total previous payments for this BL's detentions ({totalAvailableToReverse:N2}) are less than the negative final amount ({remainingToReverse:N2}), making a full reversal impossible.");
            }

            //var nextSettlementNo = (await _context.Set<Settlement>()
            //    .Where(s => s.CompanyId == detention.CompanyId)
            //    .MaxAsync(s => (int?)s.SettlementNo, ct) ?? 0) + 1;

            // 5. Create reversal settlement header
            var reversalSettlement = new Settlement
            {
                CompanyId = detention.CompanyId,
                //SettlementNo = nextSettlementNo,
                SettlementDate = settlementDate.Value,
                PartyId = detention.PartyId,
                BLId = targetBlId, // Specifically linked to this single BL for the reversal document
                BookingId = detention.BookingId,
                CurrencyId = detention.FinalCurrencyId,
                StatusId = 1,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"تسویه معکوس پرداخت مازاد دیرکرد (Negative balance: {detention.FinalPayableAmount:N2})" : remarks,
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId,
                IsDeleted = false
            };
            _context.Add(reversalSettlement);

            // 6. Create SettlementItem for final detention (negative amount to clear the balance)
            var detentionReversalItem = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId,
                IsDeleted = false,
                SettlementId = reversalSettlement.Id,
                DetentionId = detention.Id,
                InvoiceId = null,
                InvoiceItemId = null,
                ChargeId = null,
                Amount = detention.FinalPayableAmount, // Negative amount
                Remarks = "تسویه معکوس مازاد دیتنشن"
            };
            reversalSettlement.Items.Add(detentionReversalItem);

            // 7. Execute LIFO Unwind process across matching allocations
            foreach (var oldAlloc in previousAllocations)
            {
                if (remainingToReverse <= 0)
                    break;

                decimal amountToReverse = Math.Min(oldAlloc.AllocatedAmount, remainingToReverse);

                decimal settledInItemCurrencyToReverse = oldAlloc.AllocatedAmount != 0
                    ? Math.Round((amountToReverse / oldAlloc.AllocatedAmount) * oldAlloc.SettledAmountInItemCurrency, 2)
                    : amountToReverse;

                // Create negative allocation item
                var negativeAllocation = new SettlementItemPaymentAllocation
                {
                    SettlementId = reversalSettlement.Id,
                    SettlementItemId = detentionReversalItem.Id,
                    PaymentDocumentId = oldAlloc.PaymentDocumentId,

                    AllocatedAmount = -amountToReverse,
                    SettledAmountInItemCurrency = -settledInItemCurrencyToReverse,

                    ExchangeRateId = oldAlloc.ExchangeRateId,
                    ExchangeRateBaseId = oldAlloc.ExchangeRateBaseId,
                    ExchangeRateDate = oldAlloc.ExchangeRateDate,
                    ExchangeRateValue = oldAlloc.ExchangeRateValue,
                    AllocatedAmountInBaseCurrency = oldAlloc.AllocatedAmountInBaseCurrency.HasValue
                        ? -(amountToReverse * (oldAlloc.AllocatedAmountInBaseCurrency.Value / oldAlloc.AllocatedAmount))
                        : null,
                    Remarks = $"برگشت مازاد دریافتی دیرکرد {oldAlloc.PaymentDocument.DocumentNo} "
                };
                reversalSettlement.PaymentAllocations.Add(negativeAllocation);

                // 8. Decrease AllocatedAmount from the payment document (restore available balance)
                oldAlloc.PaymentDocument.AllocatedAmount -= amountToReverse;

                remainingToReverse -= amountToReverse;
            }
            // Zero out detention settled amount
            detention.SettledAmount += detention.FinalPayableAmount;
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return reversalSettlement.Id;
        }
        public async Task<Guid> SettleChequeReturnAsync(Guid chequeId, Guid currentUserId, DateTime? settlementDate = null, string? remarks = null, CancellationToken ct = default)
        {
            settlementDate ??= DateTime.Now;
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            // 1. Load cheque with its payment document
            var cheque = await _context.Set<Cheque>()
                .Include(c => c.PaymentDocument)
                .FirstOrDefaultAsync(c => c.Id == chequeId, ct);

            if (cheque == null)
                throw new InvalidOperationException("Cheque record not found.");

            var paymentDocument = cheque.PaymentDocument
                ?? await _context.Set<PaymentDocument>().FirstOrDefaultAsync(p => p.Id == cheque.PaymentDocumentId, ct);

            if (paymentDocument == null)
                throw new InvalidOperationException("The linked payment document for this cheque was not found.");

            if (cheque.StatusId == 90)
                throw new InvalidOperationException("This cheque has already been returned/refunded (StatusId=90).");

            // 2. Compute the open (unsettled) balance still held by us
            decimal openBalance = cheque.PaymentDocument.Amount - paymentDocument.AllocatedAmount;
            if (openBalance != cheque.PaymentDocument.Amount)
                throw new InvalidOperationException(
                    $"Cheque has no open balance to return (Amount: {cheque.Amount:N2}, Allocated: {paymentDocument.AllocatedAmount:N2}).");

            //// 3. Calculate next settlement number
            //var nextSettlementNo = (await _context.Set<Settlement>()
            //    .Where(s => s.CompanyId == paymentDocument.CompanyId)
            //    .MaxAsync(s => (int?)s.SettlementNo, ct) ?? 0) + 1;

            // 4. Create the reversal settlement header
            var returnSettlement = new Settlement
            {
                CompanyId = paymentDocument.CompanyId,
                //SettlementNo = nextSettlementNo,
                SettlementDate = settlementDate.Value,
                PartyId = paymentDocument.PartyId,
                CurrencyId = cheque.CurrencyId,
                StatusId = 1,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? $"Guarantee cheque return after release (Cheque No: {cheque.ChequeNo}, Amount: {cheque.Amount:N2})" : remarks,

                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId,
                IsDeleted = false,
                Id = Guid.CreateVersion7(),
            };
            _context.Add(returnSettlement);

            // 5. Settlement item representing the cheque return
            var chequeReturnItem = new SettlementItem
            {
                SettlementId = returnSettlement.Id,
                DetentionId = null,
                InvoiceId = null,
                InvoiceItemId = null,
                ChargeId = null,
                Amount = openBalance,
                Remarks = $"تسویه عودت چک(Cheque No: {cheque.ChequeNo})",
                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId,
                IsDeleted = false,
                Id = Guid.CreateVersion7(),
            };
            returnSettlement.Items.Add(chequeReturnItem);

            // 6. Negative allocation tying the return to the cheque's payment document
            var returnAllocation = new SettlementItemPaymentAllocation
            {
                SettlementId = returnSettlement.Id,
                SettlementItemId = chequeReturnItem.Id,
                PaymentDocumentId = paymentDocument.Id,

                AllocatedAmount = openBalance,
                SettledAmountInItemCurrency = openBalance,
                ExchangeRateValue = 1,
                AllocatedAmountInBaseCurrency = openBalance,
                Remarks = $"عودت چک(Cheque No: {cheque.ChequeNo})",
                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId,
                IsDeleted = false,
                Id = Guid.CreateVersion7(),
            };
            returnSettlement.PaymentAllocations.Add(returnAllocation);

            // 7. Zero out the cheque's open balance on the payment document
            paymentDocument.AllocatedAmount += openBalance; // AllocatedAmount now equals Amount => remaining = 0

            // 8. Mark the cheque as returned/refunded
            var oldStatusId = cheque.StatusId;
            cheque.StatusId = 90;
            cheque.LastStatusDate = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(remarks))
                cheque.Remarks = remarks;

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await _activityLogService.LogHistoryAsync(
                new CreateActivityLogRequest
                {
                    ActivityCategoryId = 3,
                    ActivityDateTime = DateTime.Now,
                    Title = "عودت چک به صادر کننده",
                    Description = remarks,
                    EntityTypeId = 18,
                    EntityId = chequeId,
                    FromStatusEntityType = "ChequeStatus",
                    FromStatusId = oldStatusId,
                    ToStatusId = 90,
                    PerformedByUserId = currentUserId,
                    IsSystemGenerated = true,
                }
                );
            return returnSettlement.Id;
        }
        public async Task<(Guid PaymentDocumentId, Guid SettlementId)> ExecuteCustomerCreditRefundAsync(Guid paymentRequestId, Guid currentUserId, DateTime? documentDate = null, string? remarks = null, CancellationToken ct = default)
        {
            documentDate ??= DateTime.Now;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            // 1. Fetch the approved payment request along with its items and source references
            var request = await _context.Set<PaymentRequest>()
                .Include(r => r.Items)
                    .ThenInclude(i => i.SourcePaymentDocument)
                .FirstOrDefaultAsync(r => r.Id == paymentRequestId, ct);

            if (request == null)
                throw new InvalidOperationException("Payment request not found.");

            if (request.PaymentRequestCategoryId != 1)
                throw new InvalidOperationException("Only customer credit refund requests (Category = 1) are supported by this method.");

            //if (request.PaymentDocumentId.HasValue)
            //    throw new InvalidOperationException("This payment request has already been executed; a payment document already exists.");

            if (request.StatusId != 3)
                throw new InvalidOperationException("Only approved payment requests can be executed.");

            if (request.TotalAmount <= 0)
                throw new InvalidOperationException("Refund amount must be greater than zero.");

            if (!request.PartyId.HasValue)
                throw new InvalidOperationException("A party (customer) must be specified on the payment request.");

            var partyId = request.PartyId.Value;
            var currencyId = request.CurrencyId;

            // 2. Collect explicit source payment documents defined in request items, or fetch available inbound credits
            var explicitSourceIds = request.Items
                .Where(i => i.SourcePaymentDocumentId.HasValue)
                .Select(i => i.SourcePaymentDocumentId!.Value)
                .Distinct()
                .ToList();

            List<PaymentDocument> sourceDocuments;
            sourceDocuments = await _context.Set<PaymentDocument>()
                .Where(p => explicitSourceIds.Contains(p.Id))
                .OrderBy(p => p.DocumentDate)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(ct);

            foreach (var sourceId in explicitSourceIds)
            {
                if (!sourceDocuments.Any(p => p.Id == sourceId))
                    throw new InvalidOperationException($"Source payment document {sourceId} specified in request items was not found.");
            }

            //else
            //{
            //    // Fallback: automatically pick available inbound credits for this customer & currency
            //    sourceDocuments = await _context.Set<PaymentDocument>()
            //        .Where(p =>
            //            p.PartyId == partyId &&
            //            p.CurrencyId == currencyId &&
            //            p.CompanyId == request.CompanyId &&
            //            p.FinanceDirectionId == 1 && // Inbound
            //            p.Amount > p.AllocatedAmount)
            //        .OrderBy(p => p.DocumentDate)
            //        .ThenBy(p => p.CreatedAt)
            //        .ToListAsync(ct);
            //}

            decimal availableCredit = sourceDocuments.Sum(p => p.Amount - p.AllocatedAmount);
            if (availableCredit + 0.01m < request.TotalAmount)
            {
                throw new InvalidOperationException(
                    $"Insufficient customer credit balance. Available: {availableCredit:N2}, Requested: {request.TotalAmount:N2}.");
            }

            // 3. Generate document number for the Outbound payment document
            var documentNo = await GeneratePaymentDocumentNoAsync(request.CompanyId, request.BranchId, ct);

            // 4. Create the Outbound refund PaymentDocument (FinanceDirectionId = 2)
            var refundDocument = new PaymentDocument
            {
                Id = Guid.CreateVersion7(),
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                DocumentNo = documentNo,
                PaymentMethodId = request.PreferredPaymentMethodId,
                FinanceDirectionId = 2,
                PartyId = partyId,
                Amount = request.TotalAmount,
                AllocatedAmount = 0,
                CurrencyId = currencyId,
                DocumentDate = documentDate.Value,
                EffectiveDate = documentDate,
                ReferenceNo = $"REFUND-PR-{request.RequestNo}",
                Description = request.Description ?? $"Customer credit refund for request #{request.RequestNo}",
                RelatedPaymentDocumentId = explicitSourceIds.FirstOrDefault(),
                CreateUserId = currentUserId,
                CreatedAt = DateTime.Now,
                PaymentRequestId = paymentRequestId,
                IsDeleted = false
            };
            _context.Add(refundDocument);

            //// 5. Calculate next settlement number
            //var nextSettlementNo = (await _context.Set<Settlement>()
            //    .Where(s => s.CompanyId == request.CompanyId)
            //    .MaxAsync(s => (int?)s.SettlementNo, ct) ?? 0) + 1;

            // 6. Create the Settlement header

            var settlement = new Settlement
            {
                Id = Guid.CreateVersion7(),
                CompanyId = request.CompanyId,
                //SettlementNo = nextSettlementNo,
                SettlementDate = documentDate.Value,
                PartyId = partyId,
                BLId = null,
                BookingId = null,
                CurrencyId = currencyId,
                StatusId = 1,
                Remarks = $"Customer credit refund settlement (PaymentRequest No: {request.RequestNo})",
                CreateUserId = currentUserId,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            _context.Add(settlement);

            // 7. Settlement item representing the refund liability clearing
            var refundItem = new SettlementItem
            {
                Id = Guid.CreateVersion7(),
                SettlementId = settlement.Id,
                Amount = -request.TotalAmount, // Negative amount to clear the customer credit
                Remarks = $"Refund clearing item for PaymentRequest #{request.RequestNo}",
                CreateUserId = currentUserId,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            settlement.Items.Add(refundItem);

            // 8. Consume source payment documents (LIFO unwind / capacity release)
            decimal remainingToConsume = request.TotalAmount;
            var orderedSources = sourceDocuments
                .OrderByDescending(p => p.DocumentDate)
                .ThenByDescending(p => p.CreatedAt)
                .ToList();

            foreach (var source in orderedSources)
            {
                if (remainingToConsume <= 0)
                    break;

                decimal availableInSource = source.Amount - source.AllocatedAmount;
                if (availableInSource <= 0)
                    continue;

                decimal take = Math.Min(availableInSource, remainingToConsume);

                var negativeAllocation = new SettlementItemPaymentAllocation
                {
                    Id = Guid.CreateVersion7(),
                    SettlementId = settlement.Id,
                    SettlementItemId = refundItem.Id,
                    PaymentDocumentId = source.Id,
                    AllocatedAmount = -take, // Negative allocation releases prior consumed credit
                    SettledAmountInItemCurrency = -take,
                    ExchangeRateValue = 1m,
                    Remarks = $"Credit consumption unwind from source document {source.DocumentNo}",
                    CreateUserId = currentUserId,
                    CreatedAt = DateTime.Now,
                    IsDeleted = false
                };
                settlement.PaymentAllocations.Add(negativeAllocation);

                // Update source document allocated amount (reducing its remaining available credit)
                source.AllocatedAmount += take;
                remainingToConsume -= take;
            }

            if (remainingToConsume > 0)
            {
                throw new InvalidOperationException(
                    $"Unable to fully cover the refund amount from available sources. Uncovered remainder: {remainingToConsume:N2}.");
            }

            // 9. Zero out the new refund document's open balance via positive allocation
            var refundAllocation = new SettlementItemPaymentAllocation
            {
                Id = Guid.CreateVersion7(),
                Settlement = settlement,
                SettlementItem = refundItem,
                PaymentDocumentId = refundDocument.Id,
                AllocatedAmount = request.TotalAmount, // Fully allocated -> UnallocatedAmount = 0
                SettledAmountInItemCurrency = request.TotalAmount,
                ExchangeRateValue = 1m,
                Remarks = $"Refund payment document settled upon issuance (PaymentRequest #{request.RequestNo})",
                CreateUserId = currentUserId,
                CreatedAt = DateTime.Now,
                IsDeleted = false
            };
            settlement.PaymentAllocations.Add(refundAllocation);
            refundDocument.AllocatedAmount = request.TotalAmount;

            // 10. Update PaymentRequest execution references and status
            //request.PaymentDocumentId = refundDocument.Id;
            request.SettlementId = settlement.Id;

            request.ExecutedByUserId = currentUserId;
            request.ExecutionDate = DateTime.Now;
            request.StatusId = 4; // Executed status
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return (refundDocument.Id, settlement.Id);
        }
        private async Task<string> GeneratePaymentDocumentNoAsync(int companyId, Guid? branchId, CancellationToken ct)
        {
            var count = await _context.Set<PaymentDocument>()
                .CountAsync(p => p.CompanyId == companyId, ct);
            return $"PD-REFUND-{DateTime.Now:yyyyMM}-{count + 1:D5}";
        }
        //Stimul Report
        public async Task<SettlementReportDto> GetSettlementReportDataAsync(Guid settlementId)
        {
            // ۱. اجرای کوئری هدر با Left Joinهای معادل
            var headerQuery = from s in _context.Settlements.AsNoTracking().Where(x => x.Id == settlementId)

                              join co in _context.Companies.AsNoTracking() on s.CompanyId equals co.Id into coGroup
                              from co in coGroup.DefaultIfEmpty()
                              join c in _context.Persons.AsNoTracking() on co.PersonId equals c.Id into cGroup
                              from c in cGroup.DefaultIfEmpty()
                              join p in _context.Persons.AsNoTracking() on s.PartyId equals p.Id into pGroup
                              from p in pGroup.DefaultIfEmpty()
                              join cur in _context.Currencies.AsNoTracking() on s.CurrencyId equals cur.Id into curGroup
                              from cur in curGroup.DefaultIfEmpty()
                              join ss in _context.SettlementStatuses.AsNoTracking() on s.StatusId equals ss.Id into ssGroup
                              from ss in ssGroup.DefaultIfEmpty()
                              join b in _context.Bookings.AsNoTracking() on s.BookingId equals b.Id into bGroup
                              from b in bGroup.DefaultIfEmpty()

                              select new SettlementHeaderDto
                              {
                                  SettlementNo = s.SettlementNo,
                                  SettlementDate = s.SettlementDate,
                                  Remarks = s.Remarks,
                                  //CompanyId = s.CompanyId,
                                  CompanyName = c != null ? c.PersonPersianName : null,
                                  PartyId = s.PartyId,
                                  PartyName = p != null ? p.PersonPersianName : null,
                                  //CurrencyId = s.CurrencyId,
                                  CurrencyCode = cur != null ? cur.CurrencyCode : null,
                                  StatusId = s.StatusId,
                                  StatusName = ss != null ? ss.SettlementStatusLocalName : null,
                                  BookingId = s.BookingId,
                                  BookingName = b != null ? b.BookingNo : null,
                                  CompanyId = s.CompanyId
                              };

            var header = await headerQuery.FirstOrDefaultAsync();

            // ۲. اجرای کوئری جزئیات با محاسبات شرطی (Case When) و Left Joinها
            var detailsQuery = from si in _context.SettlementItems.AsNoTracking().Where(x => x.SettlementId == settlementId)

                               join i in _context.Invoices.AsNoTracking() on si.InvoiceId equals i.Id into iGroup
                               from i in iGroup.DefaultIfEmpty()

                               join d in _context.Detentions.AsNoTracking() on si.DetentionId equals d.Id into dGroup
                               from d in dGroup.DefaultIfEmpty()

                               join sip in _context.SettlementItemPaymentAllocations.AsNoTracking() on si.Id equals sip.SettlementItemId into sipGroup
                               from sip in sipGroup.DefaultIfEmpty()

                               join pd in _context.PaymentDocuments.AsNoTracking() on sip.PaymentDocumentId equals pd.Id into pdGroup
                               from pd in pdGroup.DefaultIfEmpty()

                               join pm in _context.PaymentMethods.AsNoTracking() on pd.PaymentMethodId equals pm.Id into pmGroup
                               from pm in pmGroup.DefaultIfEmpty()

                               select new SettlementDetailDto
                               {
                                   SettlementItemId = si.Id,
                                   SettlementId = si.SettlementId,
                                   Remarks = si.Remarks,

                                   InvoiceId = si.InvoiceId,
                                   DetentionId = si.DetentionId,

                                   ExpenseRemarks = si.InvoiceId != null
                                       ? (i.Remarks ?? "-")
                                       : (si.DetentionId != null ? (d.Remarks ?? "-") : "-"),

                                   ExpenseTotalAmount = si.InvoiceId != null
                                                        ? i.TotalAmount
                                                        : (si.DetentionId != null ? d.FinalPayableAmount : 0m),



                                   ExpenseSettledAmount = si.InvoiceId != null
                                       ? i.SettledAmount
                                       : (si.DetentionId != null ? d.SettledAmount : 0m),

                                   ExpenseRemain = si.InvoiceId != null
                                       ? (i.TotalAmount - i.SettledAmount)
                                       : (si.DetentionId != null ? (d.FinalPayableAmount - d.SettledAmount) : 0m),

                                   ExpenseInvDetNo = si.InvoiceId != null
                                       ? (i.InvoiceNo ?? "-")
                                       : (si.DetentionId != null ? d.DetentionNo + "" : "-"),

                                   InvoiceNo = i != null ? i.InvoiceNo : null,
                                   DetentionNo = d != null ? d.DetentionNo + "" : null,

                                   Amount = si.Amount,
                                   AmountInBaseCurrency = si.AmountInBaseCurrency,
                                   BaseExchangeRate = si.BaseExchangeRate,

                                   AlloSettlementItemId = sip != null ? (Guid?)sip.SettlementItemId : null,
                                   AlloRemarks = sip != null ? sip.Remarks : null,
                                   AlloSettledAmountInItemCurrency = sip != null ? sip.SettledAmountInItemCurrency : null,
                                   AlloAllocatedAmount = sip != null ? sip.AllocatedAmount : null,
                                   PaymentDocumentId = sip != null ? sip.PaymentDocumentId : null,
                                   AlloDocumentNo = pd != null ? pd.DocumentNo : null,
                                   AlloDocumentDate = pd != null ? pd.DocumentDate : null,
                                   AlloPaymentMethodName = pm != null ? pm.PaymentMethodLocalName : null
                               };

            var details = await detailsQuery
                .OrderBy(x => x.SettlementItemId)
                .ThenBy(x => x.AlloDocumentDate)
                .ThenBy(x => x.AlloDocumentNo)
                .ToListAsync();

            // خروجی تجمیع‌شده برای هر دو کوئری
            return new SettlementReportDto
            {
                Header = header,
                Details = details
            };
        }

    }
}