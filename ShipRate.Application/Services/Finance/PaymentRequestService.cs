using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.Finance;
using ShipRate.Application.Interfaces;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Application.Utilities;
using ShipRate.Common.PublicClass;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Infrastructure.Data;
namespace ShipRate.Application.Services.Finance
{
    public class PaymentRequestService : IPaymentRequestService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        public PaymentRequestService(ApplicationDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResultDto<PaymentRequestListDto>> GetPagedAsync(PaymentRequestFilterDto input)
        {
            var query = _db.PaymentRequests.AsNoTracking().AsQueryable();

            if (input.RequestNo.HasValue)
                query = query.Where(x => x.RequestNo == input.RequestNo.Value);

            if (input.StatusId.HasValue)
                query = query.Where(x => x.StatusId == input.StatusId.Value);

            if (input.PaymentRequestCategoryId.HasValue)
                query = query.Where(x => x.PaymentRequestCategoryId == input.PaymentRequestCategoryId.Value);

            if (input.PartyId.HasValue)
                query = query.Where(x => x.PartyId == input.PartyId.Value);

            if (input.CurrencyId.HasValue)
                query = query.Where(x => x.CurrencyId == input.CurrencyId.Value);

            if (input.BranchId.HasValue)
                query = query.Where(x => x.BranchId == input.BranchId.Value);

            if (input.FromDate.HasValue)
                query = query.Where(x => x.RequestDate >= input.FromDate.Value.Date);

            if (input.ToDate.HasValue)
            {
                var toDate = input.ToDate.Value.Date.AddDays(1);
                query = query.Where(x => x.RequestDate < toDate);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();

                query = query.Where(x =>
                    (x.Description != null && x.Description.Contains(term)) ||
                    (x.TargetAccountName != null && x.TargetAccountName.Contains(term)) ||
                    (x.TargetBankName != null && x.TargetBankName.Contains(term)) ||
                    (x.TargetIban != null && x.TargetIban.Contains(term)) ||
                    (x.Party != null && x.Party.PersonName.Contains(term)));
            }

            query = query.OrderByDescending(x => x.RequestDate).ThenByDescending(x => x.RequestNo);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((input.PageNumber - 1) * input.PageSize)
                .Take(input.PageSize)
                .Select(x => new PaymentRequestListDto
                {
                    Id = x.Id,
                    RequestNo = x.RequestNo,
                    RequestDate = x.RequestDate,
                    CategoryName = x.PaymentRequestCategory != null
                        ? (x.PaymentRequestCategory.PaymentRequestCategoryLocalName ?? x.PaymentRequestCategory.PaymentRequestCategoryName)
                        : "",
                    StatusName = x.PaymentRequestStatus != null
                        ? (x.PaymentRequestStatus.PaymentRequestStatusLocalName ?? x.PaymentRequestStatus.PaymentRequestStatusName)
                        : "",
                    PartyName = x.Party != null ? x.Party.PersonName : "",
                    TotalAmount = x.TotalAmount,
                    CurrencyName = x.Currency.CurrencyCode,
                    PreferredPaymentMethodName = x.PreferredPaymentMethod.PaymentMethodName,
                    BranchName = x.Branch != null ? x.Branch.BranchName : "",
                    Description = x.Description,
                    ApprovalDate = x.ApprovalDate,
                    ExecutionDate = x.ExecutionDate,
                    StatusId = x.StatusId,
                    RequestedByUserName = x.RequestedByUser != null ? x.RequestedByUser.FullName : "-",
                    ApprovedByUserName = x.ApprovedByUser == null ? "-" : x.ApprovedByUser.FullName,
                    RejectedByUserName = x.RequestedByUser == null ? "-" : x.RequestedByUser.FullName,
                    ConfirmedByUserName = x.FinalConfirmedByUser == null ? "-" : x.FinalConfirmedByUser.FullName,
                    ExecutedByUserName = x.ExecutedByUser == null ? "-" : x.ExecutedByUser.FullName
                })
                .ToListAsync();
            return new PagedResultDto<PaymentRequestListDto>(items, totalCount, input.PageNumber, input.PageSize);
        }
        public async Task<PaymentRequestEditDto> GetForEditAsync(Guid? id, CancellationToken cancellationToken = default)
        {
            // حالت Create (ثبت درخواست جدید)
            if (!id.HasValue || id == Guid.Empty)
            {
                return new PaymentRequestEditDto
                {
                    RequestDate = DateTime.Today,
                    FinanceDirectionId = 2, // Outbound
                    StatusId = 1, // Draft / Pending
                    Items = new List<PaymentRequestItemEditDto>(),
                    SelectedPaymentDocumentIds = new List<Guid>(),
                    SelectedInvoiceIds = new List<Guid>()
                };
            }

            // حالت Edit (واکشی اطلاعات موجود)
            var entity = await _db.PaymentRequests
                .Include(x => x.Items)
                .Include(x => x.Party) // برای نمایش متن Select2 در ادیت
                .Include(x => x.ServiceRequest) // عطف به درخواست خدمات
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken);

            if (entity == null)
                throw new KeyNotFoundException("درخواست پرداخت مورد نظر یافت نشد.");

            // ۱. استخراج شناسه‌های اسناد دریافتی قبلاً لینک‌شده (برای دسته‌بندی ۱)
            var linkedDocIds = entity.Items
                .Where(i => i.SourcePaymentDocumentId.HasValue)
                .Select(i => i.SourcePaymentDocumentId!.Value)
                .Distinct()
                .ToList();

            // ۲. استخراج شناسه‌های فاکتورهای خرید قبلاً لینک‌شده (برای دسته‌بندی ۲)
            var linkedInvoiceIds = entity.Items
                .Where(i => i.InvoiceId.HasValue)
                .Select(i => i.InvoiceId!.Value)
                .Distinct()
                .ToList();

            // ۳. فیلتر کردن ردیف‌های جدول پایین جهت جلوگیری از تکرار اقلام تیک‌خورده بالا
            IEnumerable<PaymentRequestItem> itemsToDisplay;

            if (entity.PaymentRequestCategoryId == 1)
            {
                // در دسته ۱، ردیف‌های اسناد دریافتی در چک‌باکس‌های بالا مدیریت می‌شوند
                itemsToDisplay = entity.Items.Where(i => !i.SourcePaymentDocumentId.HasValue);
            }
            else if (entity.PaymentRequestCategoryId == 2)
            {
                // در دسته ۲، ردیف‌های فاکتورهای خرید در چک‌باکس‌های بالا مدیریت می‌شوند
                itemsToDisplay = entity.Items.Where(i => !i.InvoiceId.HasValue);
            }
            else
            {
                itemsToDisplay = entity.Items;
            }

            var dto = new PaymentRequestEditDto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                BranchId = entity.BranchId,
                RequestNo = entity.RequestNo,
                RequestDate = entity.RequestDate,
                FinanceDirectionId = entity.FinanceDirectionId,
                PaymentRequestCategoryId = entity.PaymentRequestCategoryId,
                PartyId = entity.PartyId,
                PartyName = entity.Party != null ? entity.Party.PersonName : null,
                ServiceRequestId = entity.ServiceRequestId,
                ServiceRequestTitle = entity.ServiceRequest != null ? $"#{entity.ServiceRequest.RequestNo} - {entity.ServiceRequest.Subject}" : null,
                TotalAmount = entity.TotalAmount,
                CurrencyId = entity.CurrencyId,
                PreferredPaymentMethodId = entity.PreferredPaymentMethodId,
                TargetIban = entity.TargetIban,
                TargetAccountName = entity.TargetAccountName,
                TargetBankName = entity.TargetBankName,
                StatusId = entity.StatusId,
                Description = entity.Description,
                RequestedByUserId = entity.RequestedByUserId,
                RejectedByUserId = entity.RejectedByUserId,
                RejectDate = entity.RejectDate,
                RejectionReason = entity.RejectionReason,
                ApprovedByUserId = entity.ApprovedByUserId,
                ApprovalDate = entity.ApprovalDate,
                FinalConfirmedByUserId = entity.FinalConfirmedByUserId,
                FinanlConfirmDate = entity.FinanlConfirmDate,
                ExecutedByUserId = entity.ExecutedByUserId,
                ExecutionDate = entity.ExecutionDate,
                VaultId = entity.VaultId,

                // چک‌باکس‌های انتخابی فرم
                SelectedPaymentDocumentIds = linkedDocIds,
                SelectedInvoiceIds = linkedInvoiceIds,

                // ردیف‌های دستی جدول آیتم‌ها
                Items = itemsToDisplay.Select(i => new PaymentRequestItemEditDto
                {
                    Id = i.Id,
                    Amount = i.Amount,
                    Description = i.Description,
                    InvoiceId = i.InvoiceId,
                    SourcePaymentDocumentId = i.SourcePaymentDocumentId,
                    DetentionId = i.DetentionId,
                    JobId = i.JobId
                }).ToList()
            };

            return dto;
        }
        public async Task<Guid> CreateAsync(PaymentRequestEditDto input, CancellationToken cancellationToken = default)
        {
            using var trx = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var entity = new PaymentRequest
                {
                    CompanyId = input.CompanyId,
                    BranchId = input.BranchId,
                    RequestNo = input.RequestNo == 0 ? await GetNextRequestNoAsync(input.CompanyId) : input.RequestNo,
                    RequestDate = input.RequestDate,
                    FinanceDirectionId = 2, // Outbound payment request
                    PaymentRequestCategoryId = input.PaymentRequestCategoryId,
                    PartyId = input.PartyId,
                    CurrencyId = input.CurrencyId,
                    PreferredPaymentMethodId = input.PreferredPaymentMethodId,
                    TargetIban = input.TargetIban,
                    TargetAccountName = input.TargetAccountName,
                    TargetBankName = input.TargetBankName,
                    StatusId = 10, // Draft / Pending
                    Description = input.Description,
                    ServiceRequestId = input.PaymentRequestCategoryId == 1 ? input.ServiceRequestId : null,
                    RequestedByUserId = input.RequestedByUserId,
                    RejectedByUserId = input.RejectedByUserId,
                    RejectDate = input.RejectDate,
                    RejectionReason = input.RejectionReason,
                    ApprovedByUserId = input.ApprovedByUserId,
                    ApprovalDate = input.ApprovalDate,
                    FinalConfirmedByUserId = input.FinalConfirmedByUserId,
                    FinanlConfirmDate = input.FinanlConfirmDate,
                    ExecutedByUserId = input.ExecutedByUserId,
                    ExecutionDate = input.ExecutionDate,
                    CreatedAt = DateTime.Now,
                    CreateUserId = _currentUserService.UserId,
                    IsDeleted = false,
                    VaultId = input.VaultId
                };

                // ۱. بررسی و ثبت آیتم‌های دستی وارد شده در جدول Items
                if (input.Items != null && input.Items.Any())
                {
                    foreach (var item in input.Items.Where(x => x.Amount > 0))
                    {
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = item.Amount,
                            Description = item.Description,
                            InvoiceId = item.InvoiceId,
                            SourcePaymentDocumentId = item.SourcePaymentDocumentId,
                            DetentionId = item.DetentionId,
                            JobId = item.JobId,
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // ۲. الف) اگر دسته‌بندی ۱ (عودت بستانکاری مشتری) باشد
                if (input.PaymentRequestCategoryId == 1 &&
                    input.SelectedPaymentDocumentIds != null &&
                    input.SelectedPaymentDocumentIds.Any())
                {
                    var validDocIds = input.SelectedPaymentDocumentIds.Distinct().ToList();

                    var documents = await _db.PaymentDocuments
                        .AsNoTracking()
                        .Where(pd => validDocIds.Contains(pd.Id) &&
                                     pd.PartyId == input.PartyId &&
                                     pd.FinanceDirectionId == 1 &&
                                     pd.Amount > pd.AllocatedAmount)
                        .ToListAsync(cancellationToken);

                    foreach (var doc in documents)
                    {
                        var remaining = doc.Amount - doc.AllocatedAmount;
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = remaining,
                            SourcePaymentDocumentId = doc.Id,
                            Description = !string.IsNullOrWhiteSpace(doc.DocumentNo)
                                ? $"Customer credit refund from receipt doc #{doc.DocumentNo}"
                                : $"Customer credit refund from receipt doc ({doc.Id})",
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // ۲. ب) اگر دسته‌بندی ۲ (تسویه فاکتور خرید خطوط/تامین‌کنندگان) باشد
                if (input.PaymentRequestCategoryId == 2 &&
                    input.SelectedInvoiceIds != null &&
                    input.SelectedInvoiceIds.Any())
                {
                    var validInvoiceIds = input.SelectedInvoiceIds.Distinct().ToList();

                    var invoices = await _db.Invoices
                        .AsNoTracking()
                        .Where(inv => validInvoiceIds.Contains(inv.Id) &&
                                      inv.PartyId == input.PartyId &&
                                      inv.InvoiceTypeId == 2)
                        .ToListAsync(cancellationToken);

                    foreach (var inv in invoices)
                    {
                        var dueAmount = inv.TotalAmount - inv.SettledAmount;
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = dueAmount > 0 ? dueAmount : inv.TotalAmount,
                            InvoiceId = inv.Id,
                            Description = $"Settlement for Vendor Invoice #{inv.InvoiceNo}",
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // ۳. محاسبه TotalAmount نهایی
                if (entity.Items.Any())
                {
                    entity.TotalAmount = entity.Items.Sum(x => x.Amount);
                }
                else
                {
                    entity.TotalAmount = input.TotalAmount;
                }

                _db.PaymentRequests.Add(entity);
                await _db.SaveChangesAsync(cancellationToken);
                await trx.CommitAsync(cancellationToken);

                return entity.Id;
            }
            catch
            {
                await trx.RollbackAsync(cancellationToken);
                throw;
            }
        }
        public async Task UpdateAsync(PaymentRequestEditDto input, CancellationToken cancellationToken = default)
        {
            if (!input.Id.HasValue || input.Id == Guid.Empty)
                throw new ArgumentException("شناسه درخواست پرداخت نامعتبر است.");

            using var trx = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var entity = await _db.PaymentRequests
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == input.Id.Value, cancellationToken);

                if (entity == null)
                    throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

                // بروزرسانی فیلدهای هدر
                entity.CompanyId = input.CompanyId;
                entity.BranchId = input.BranchId;
                entity.RequestDate = input.RequestDate;
                entity.PaymentRequestCategoryId = input.PaymentRequestCategoryId;
                entity.PartyId = input.PartyId;
                entity.ServiceRequestId = input.PaymentRequestCategoryId == 1 ? input.ServiceRequestId : null;
                entity.CurrencyId = input.CurrencyId;
                entity.PreferredPaymentMethodId = input.PreferredPaymentMethodId;
                entity.TargetIban = input.TargetIban;
                entity.TargetAccountName = input.TargetAccountName;
                entity.TargetBankName = input.TargetBankName;
                entity.Description = input.Description;
                entity.RequestedByUserId = input.RequestedByUserId;
                entity.RejectedByUserId = input.RejectedByUserId;
                entity.RejectDate = input.RejectDate;
                entity.RejectionReason = input.RejectionReason;
                entity.ApprovedByUserId = input.ApprovedByUserId;
                entity.ApprovalDate = input.ApprovalDate;
                entity.FinalConfirmedByUserId = input.FinalConfirmedByUserId;
                entity.FinanlConfirmDate = input.FinanlConfirmDate;
                entity.ExecutedByUserId = input.ExecutedByUserId;
                entity.ExecutionDate = input.ExecutionDate;
                entity.UpdatedAt = DateTime.Now;
                entity.EditUserId = _currentUserService.UserId;
                entity.VaultId = input.VaultId;
                // =========================================================================
                // بخش ۱: مدیریت اقلام متصل به اسناد دریافتی عودت (Category == 1)
                // =========================================================================
                var selectedDocIds = (input.PaymentRequestCategoryId == 1
                    ? (input.SelectedPaymentDocumentIds ?? new List<Guid>())
                    : new List<Guid>()).Distinct().ToList();

                var currentDocItems = entity.Items.Where(x => x.SourcePaymentDocumentId.HasValue).ToList();

                // الف) حذف اسنادی که تیک آنها برداشته شده یا کتگوری تغییر کرده است
                foreach (var docItem in currentDocItems)
                {
                    if (!selectedDocIds.Contains(docItem.SourcePaymentDocumentId!.Value))
                    {
                        _db.PaymentRequestItems.Remove(docItem);
                    }
                }

                // ب) افزودن اسناد جدید انتخاب‌شده
                var existingDocIds = currentDocItems
                    .Select(x => x.SourcePaymentDocumentId!.Value)
                    .Intersect(selectedDocIds)
                    .ToList();

                var newDocIdsToAdd = selectedDocIds.Except(existingDocIds).ToList();

                if (newDocIdsToAdd.Any())
                {
                    var newDocuments = await _db.PaymentDocuments
                        .AsNoTracking()
                        .Where(pd => newDocIdsToAdd.Contains(pd.Id) &&
                                     pd.PartyId == input.PartyId &&
                                     pd.FinanceDirectionId == 1)
                        .ToListAsync(cancellationToken);

                    foreach (var doc in newDocuments)
                    {
                        var remaining = doc.Amount - doc.AllocatedAmount;
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = remaining > 0 ? remaining : doc.Amount,
                            SourcePaymentDocumentId = doc.Id,
                            Description = !string.IsNullOrWhiteSpace(doc.DocumentNo)
                                ? $"Customer credit refund from receipt doc #{doc.DocumentNo}"
                                : $"Customer credit refund from receipt doc ({doc.Id})",
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // =========================================================================
                // بخش ۲: مدیریت اقلام متصل به فاکتورهای خرید (Category == 2)
                // =========================================================================
                var selectedInvIds = (input.PaymentRequestCategoryId == 2
                    ? (input.SelectedInvoiceIds ?? new List<Guid>())
                    : new List<Guid>()).Distinct().ToList();

                var currentInvoiceItems = entity.Items.Where(x => x.InvoiceId.HasValue).ToList();

                // الف) حذف فاکتورهایی که تیک آنها برداشته شده یا کتگوری عوض شده است
                foreach (var invItem in currentInvoiceItems)
                {
                    if (!selectedInvIds.Contains(invItem.InvoiceId!.Value))
                    {
                        _db.PaymentRequestItems.Remove(invItem);
                    }
                }

                // ب) افزودن فاکتورهای خرید جدید انتخاب‌شده
                var existingInvIds = currentInvoiceItems
                    .Select(x => x.InvoiceId!.Value)
                    .Intersect(selectedInvIds)
                    .ToList();

                var newInvIdsToAdd = selectedInvIds.Except(existingInvIds).ToList();

                if (newInvIdsToAdd.Any())
                {
                    var newInvoices = await _db.Invoices
                        .AsNoTracking()
                        .Where(inv => newInvIdsToAdd.Contains(inv.Id) &&
                                      inv.PartyId == input.PartyId &&
                                      inv.InvoiceTypeId == 2)
                        .ToListAsync(cancellationToken);

                    foreach (var inv in newInvoices)
                    {
                        var dueAmount = inv.TotalAmount - inv.SettledAmount;
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = dueAmount > 0 ? dueAmount : inv.TotalAmount,
                            InvoiceId = inv.Id,
                            Description = $"Settlement for Vendor Invoice #{inv.InvoiceNo}",
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // =========================================================================
                // بخش ۳: مدیریت اقلام دستی جدول پایین (فاقد سند و فاکتور)
                // =========================================================================
                var manualInputs = (input.Items ?? new List<PaymentRequestItemEditDto>())
                    .Where(x => x.Amount > 0)
                    .ToList();

                var manualInputIds = manualInputs
                    .Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
                    .Select(x => x.Id!.Value)
                    .ToList();

                // اقلام دستی موجود در انتیتی (نه سند هستند نه فاکتور)
                var currentManualItems = entity.Items
                    .Where(x => !x.SourcePaymentDocumentId.HasValue && !x.InvoiceId.HasValue)
                    .ToList();

                // الف) حذف ردیف‌های دستی برداشته شده از جدول
                foreach (var oldManualItem in currentManualItems)
                {
                    if (!manualInputIds.Contains(oldManualItem.Id))
                    {
                        _db.PaymentRequestItems.Remove(oldManualItem);
                    }
                }

                // ب) بروزرسانی ردیف‌های موجود یا ایجاد ردیف دستی جدید
                foreach (var itemDto in manualInputs)
                {
                    if (itemDto.Id.HasValue && itemDto.Id.Value != Guid.Empty)
                    {
                        var existingItem = currentManualItems.FirstOrDefault(x => x.Id == itemDto.Id.Value);
                        if (existingItem != null)
                        {
                            existingItem.Amount = itemDto.Amount;
                            existingItem.Description = itemDto.Description;
                            existingItem.DetentionId = itemDto.DetentionId;
                            existingItem.JobId = itemDto.JobId;
                            existingItem.UpdatedAt = DateTime.Now;
                            existingItem.EditUserId = _currentUserService.UserId;
                        }
                    }
                    else
                    {
                        entity.Items.Add(new PaymentRequestItem
                        {
                            Amount = itemDto.Amount,
                            Description = itemDto.Description,
                            InvoiceId = null,
                            SourcePaymentDocumentId = null,
                            DetentionId = itemDto.DetentionId,
                            JobId = itemDto.JobId,
                            CreatedAt = DateTime.Now,
                            CreateUserId = _currentUserService.UserId,
                            IsDeleted = false
                        });
                    }
                }

                // =========================================================================
                // بخش ۴: محاسبه سرجمع کل درخواست
                // =========================================================================
                entity.TotalAmount = entity.Items.Any()
                    ? entity.Items.Sum(x => x.Amount)
                    : input.TotalAmount;

                await _db.SaveChangesAsync(cancellationToken);
                await trx.CommitAsync(cancellationToken);
            }
            catch
            {
                await trx.RollbackAsync(cancellationToken);
                throw;
            }
        }
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null || entity.IsDeleted)
                return;

            var now = DateTime.Now;
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeleteUserId = _currentUserService.UserId;

            if (entity.Items != null && entity.Items.Any())
            {
                foreach (var item in entity.Items)
                {
                    item.IsDeleted = true;
                    item.DeletedAt = now;
                    item.DeleteUserId = _currentUserService.UserId;
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task<long> GetNextRequestNoAsync(int companyId)
        {
            var maxNo = await _db.PaymentRequests
                .Where(x => x.CompanyId == companyId)
                .Select(x => (long?)x.RequestNo)
                .MaxAsync();

            return (maxNo ?? 0) + 1;
        }
        public async Task<List<SelectListItem>> GetStatusesSelectListAsync()
        {
            return await _db.PaymentRequestStatuses
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.PaymentRequestStatusLocalName ?? x.PaymentRequestStatusName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetCategoriesSelectListAsync()
        {
            return await _db.PaymentRequestCategories
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.PaymentRequestCategoryLocalName ?? x.PaymentRequestCategoryName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetCurrenciesSelectListAsync()
        {
            return await _db.Currencies
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.CurrencyCode
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetPaymentMethodsSelectListAsync()
        {
            return await _db.PaymentMethods
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.PaymentMethodName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetVaultsSelectListAsync()
        {
            return await _db.Vaults
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.VaultName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetBranchesSelectListAsync(int companyId)
        {
            return await _db.Branches
                .AsNoTracking()
                .OrderBy(x => x.BranchName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.BranchName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetCompaniesSelectListAsync()
        {
            return await _db.Companies
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Person.PersonName
                }).ToListAsync();
        }
        public async Task<List<SelectListItem>> GetPartiesSelectListAsync()
        {
            return await _db.Persons
                .AsNoTracking()
                .OrderBy(x => x.PersonName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.PersonName
                }).ToListAsync();
        }
        public async Task<List<LookupDto>> GetPartyServiceRequestsAsync(Guid partyId, CancellationToken cancellationToken = default)
        {
            if (partyId == Guid.Empty)
                return new List<LookupDto>();

            return await _db.ServiceRequests
                .AsNoTracking()
                .Where(x => x.PartyId == partyId && !x.IsDeleted)
                .OrderByDescending(x => x.RequestDate)
                .Select(x => new LookupDto
                {
                    Id = x.Id.ToString(),
                    Text = $"#{x.RequestNo} - {x.Subject} ({x.RequestDate:yyyy/MM/dd})"
                })
                .ToListAsync(cancellationToken);
        }
        public async Task<IReadOnlyList<PaymentDocumentChoiceDto>> GetPaymentDocumentChoicesAsync(Guid partyId, Guid? serviceRequestId, CancellationToken cancellationToken = default)
        {
            if (partyId == Guid.Empty)
                return Array.Empty<PaymentDocumentChoiceDto>();

            // Only inbound documents are valid sources for customer credit refunds.
            var query = _db.PaymentDocuments
                .AsNoTracking()
                .Where(pd =>
                    pd.PartyId == partyId &&
                    pd.FinanceDirectionId == 1 &&
                    pd.PaymentMethodId != 3 &&
                    pd.Amount > pd.AllocatedAmount);

            if (serviceRequestId.HasValue && serviceRequestId.Value != Guid.Empty)
            {
                var isValidServiceRequest = await _db.ServiceRequests
                    .AsNoTracking()
                    .AnyAsync(sr =>
                        sr.Id == serviceRequestId.Value &&
                        sr.PartyId == partyId &&
                        !sr.IsDeleted,
                        cancellationToken);

                if (!isValidServiceRequest)
                    return Array.Empty<PaymentDocumentChoiceDto>();

                query = query.Where(pd =>
                    _db.ServiceRequestPaymentDocuments.Any(link =>
                        link.ServiceRequestId == serviceRequestId.Value &&
                        link.PaymentDocumentId == pd.Id));
            }

            var documents = await query
                .OrderBy(pd => pd.Id)
                .Select(pd => new
                {
                    pd.Id,
                    pd.Amount,
                    pd.AllocatedAmount,
                    pd.DocumentNo,
                    pd.Currency.CurrencyCode,
                    pd.DocumentDate
                })
                .ToListAsync(cancellationToken);

            return documents
                .Select(pd => new PaymentDocumentChoiceDto
                {
                    Id = pd.Id,
                    Text = $"Payment Document - {pd.DocumentNo} - {pd.DocumentDate.ToString("yyyy-MM-dd")} - {pd.CurrencyCode} ",
                    RemainingAmount = pd.Amount - pd.AllocatedAmount
                })
                .ToList();
        }
        public async Task<List<VendorInvoiceChoiceDto>> GetVendorInvoiceChoicesAsync(Guid partyId, CancellationToken cancellationToken = default)
        {
            if (partyId == Guid.Empty)
                return new List<VendorInvoiceChoiceDto>();

            var invoices = await _db.Invoices
                .AsNoTracking()
                .Where(x => x.PartyId == partyId && x.FinanceDirectionId == 2)
                .Select(x => new
                {
                    x.Id,
                    x.InvoiceNo,
                    x.InvoiceDate,
                    x.TotalAmount,
                    RemainingAmount = x.TotalAmount - x.SettledAmount
                })
                .Where(x => x.RemainingAmount > 0)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync(cancellationToken);
            return invoices.Select(x => new VendorInvoiceChoiceDto
            {
                Id = x.Id,
                InvoiceNo = x.InvoiceNo,
                InvoiceDate = x.InvoiceDate.Value.ToString("yyyy-MM-dd"),
                TotalAmount = x.TotalAmount,
                RemainingAmount = x.RemainingAmount,
                Text = $"Inv #{x.InvoiceNo} - {x.InvoiceDate:yyyy-MM-dd} (Total: {x.TotalAmount:N0})"
            }).ToList();
        }
        public async Task SendRequestAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

            if (entity.StatusId != 10) // فقط از حالت Draft قابل ارسال است
                throw new InvalidOperationException("فقط درخواست‌های در حالت درافت قابل ارسال هستند.");

            entity.StatusId = 15; // Request Sent
            entity.RequestSentDate = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task ConfirmRequestAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

            if (entity.StatusId != 15)
                throw new InvalidOperationException("درخواست باید در وضعیت 'ارسال درخواست' باشد.");

            entity.StatusId = 20; // Confirmed (تایید میانی)
                                  // در صورت وجود فیلد ConfirmDate یا استفاده از ApprovedByUserId
            entity.ApprovedByUserId = _currentUserService.UserId;
            entity.ApprovalDate = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task ApproveRequestAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

            if (entity.StatusId != 20)
                throw new InvalidOperationException("درخواست باید در وضعیت 'تایید میانی' باشد.");

            entity.StatusId = 30; // Approved (تایید نهایی)
            entity.FinalConfirmedByUserId = _currentUserService.UserId;
            entity.FinanlConfirmDate = DateTime.Now;
            _db.Entry(entity).State = EntityState.Modified;

            if (entity.VaultId != null && entity.VaultId.HasValue && entity.VaultId != Guid.Empty)
            {
                var cpr = new CashPaymentRequest();
                cpr.Id = Guid.CreateVersion7();
                cpr.VaultId = entity.VaultId.Value;
                cpr.Reason = entity.Description;
                cpr.PartyId = entity.PartyId.Value;
                cpr.StatusId = 5; //Initial Confirmed
                cpr.CreateUserId = entity.CreateUserId;
                cpr.CreatedAt = entity.CreatedAt;
                cpr.InitialConfirmedByUserId = _currentUserService.UserId;
                cpr.InitialConfirmDate = DateTime.Now;
                cpr.Amount = entity.TotalAmount;
                cpr.CurrencyId = entity.CurrencyId;
                cpr.RequestNo = entity.RequestNo.ToString();
                entity.CashPaymentRequestId = cpr.Id;
                _db.CashPaymentRequests.Add(cpr);
                _db.Entry(cpr).State = EntityState.Added;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task PayRequestAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

            if (entity.StatusId != 30)
                throw new InvalidOperationException("درخواست باید در وضعیت 'تایید نهایی' باشد تا پرداخت شود.");

            entity.StatusId = 40; // Paid (پرداخت شده)
            entity.ExecutedByUserId = _currentUserService.UserId;
            entity.ExecutionDate = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task RejectRequestAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.PaymentRequests.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) throw new KeyNotFoundException("درخواست پرداخت یافت نشد.");

            if (entity.StatusId != 15 && entity.StatusId != 20)
                throw new InvalidOperationException("درخواست باید در وضعیت 'تایید نهایی' باشد تا پرداخت شود.");

            entity.StatusId = 99; // Paid (پرداخت شده)
            entity.RejectedByUserId = _currentUserService.UserId;
            entity.RejectDate = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }

        //------------Stimul Soft Report-------------------
        public async Task<PaymentRequestDetailsViewModel?> GetPaymentRequestDetailsAsync(Guid paymentRequestId, CancellationToken cancellationToken = default)
        {
            var header = await (
                from pr in _db.PaymentRequests.AsNoTracking()

                join co0 in _db.Companies.AsNoTracking() on pr.CompanyId equals co0.Id into companyGroup
                from co in companyGroup.DefaultIfEmpty()
                join c0 in _db.Persons.AsNoTracking() on co!.PersonId equals c0.Id into companyPersonGroup
                from c in companyPersonGroup.DefaultIfEmpty()
                join b0 in _db.Branches.AsNoTracking() on pr.BranchId equals b0.Id into branchGroup
                from b in branchGroup.DefaultIfEmpty()
                join fd0 in _db.FinanceDirections.AsNoTracking() on pr.FinanceDirectionId equals fd0.Id into financeDirectionGroup
                from fd in financeDirectionGroup.DefaultIfEmpty()
                join prc0 in _db.PaymentRequestCategories.AsNoTracking() on pr.PaymentRequestCategoryId equals prc0.Id into categoryGroup
                from prc in categoryGroup.DefaultIfEmpty()
                join p0 in _db.Persons.AsNoTracking() on (Guid?)pr.PartyId equals (Guid?)p0.Id into partyGroup
                from p in partyGroup.DefaultIfEmpty()
                join ua0 in _db.Users.AsNoTracking() on (Guid?)pr.ApprovedByUserId equals (Guid?)ua0.Id into approvedUserGroup
                from ua in approvedUserGroup.DefaultIfEmpty()
                join uf0 in _db.Users.AsNoTracking() on (Guid?)pr.FinalConfirmedByUserId equals (Guid?)uf0.Id into finalConfirmedUserGroup
                from uf in finalConfirmedUserGroup.DefaultIfEmpty()
                join ue0 in _db.Users.AsNoTracking() on (Guid?)pr.ExecutedByUserId equals (Guid?)ue0.Id into executedUserGroup
                from ue in executedUserGroup.DefaultIfEmpty()
                where pr.Id == paymentRequestId && !pr.IsDeleted

                select new PaymentRequestHeaderViewModel
                {
                    Id = pr.Id,
                    RequestNo = pr.RequestNo,
                    RequestDate = ConvertDate.ToPersianDate(pr.RequestDate),
                    TotalAmount = pr.TotalAmount,
                    Description = pr.Description,

                    CompanyName = c == null ? null : c.PersonPersianName,
                    BranchName = b == null ? null : b.BranchName,
                    FinanceDirectionName = fd == null ? null : fd.ChargeContextName,
                    PaymentRequestCategoryName = prc == null ? null : prc.PaymentRequestCategoryName,
                    PartyName = p == null ? null : p.PersonPersianName,

                    ApprovalDate = ConvertDate.ToPersianDate(pr.ApprovalDate),
                    ApprovedName = ua == null ? null : ua.FullName,

                    // اگر نام Property در Entity شما FinalConfirmDate است، همین را جایگزین کنید.
                    FinalConfirmDate = ConvertDate.ToPersianDate(pr.FinanlConfirmDate),
                    FinalConfirmName = uf == null ? null : uf.FullName,

                    ExecutionDate = ConvertDate.ToPersianDate(pr.ExecutionDate),
                    ExecutedName = ue == null ? null : ue.FullName,
                    TargetAccountName = pr.TargetAccountName,
                    CompanyId = pr.CompanyId,

                    //CreatorMessage =
                    //    $"درخواست پرداخت به مبلغ <b>(عدد) {pr.TotalAmount:###,###} ریال</b>، " +
                    //    $"به حروف <b>{NumberToWordsExtensions.ToPersianWords(pr.TotalAmount)} ریال</b> " +
                    //    $"در وجه <b>{p.PersonPersianName}</b> بابت " +
                    //    $"<b>{prc.PaymentRequestCategoryName}</b> به شماره حساب " +
                    //    $"<b>{pr.TargetAccountName}</b> را دارم."

                    //CreatorMessage = "درخواست پرداخت به مبلغ (عدد) " + pr.TotalAmount.ToString("###,###") + " ریال، به حروف " + NumberToWordsExtensions.ToPersianWords(pr.TotalAmount) + " ریال در وجه " + p.PersonPersianName + " بابت " + prc.PaymentRequestCategoryName + " به شماره حساب " + pr.TargetAccountName + " را دارم."
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (header is null)
            {
                return null;
            }

            var items = await (
                from pri in _db.PaymentRequestItems.AsNoTracking()

                join j0 in _db.Jobs.AsNoTracking() on (Guid?)pri.JobId equals (Guid?)j0.Id into jobGroup
                from j in jobGroup.DefaultIfEmpty()
                join pd0 in _db.PaymentDocuments.AsNoTracking() on (Guid?)pri.SourcePaymentDocumentId equals (Guid?)pd0.Id into paymentDocumentGroup
                from pd in paymentDocumentGroup.DefaultIfEmpty()
                where pri.PaymentRequestId == paymentRequestId

                select new PaymentRequestItemViewModel
                {
                    Id = pri.Id,
                    Amount = pri.Amount,
                    Description = pri.Description,

                    DocumentNo = pd == null ? null : pd.DocumentNo,
                    SourcePaymentDocumentId = pri.SourcePaymentDocumentId,
                    MainAmount = pd == null ? null : pd.Amount,
                    AllocatedAmount = pd == null ? null : pd.AllocatedAmount,

                    JobId = pri.JobId,
                    JobNumber = j == null ? null : j.JobNumber,
                    JobFileNumber = j == null ? null : j.JobFileNumber
                }).ToListAsync(cancellationToken);

            return new PaymentRequestDetailsViewModel
            {
                Header = header,
                Items = items
            };
        }
    }
}