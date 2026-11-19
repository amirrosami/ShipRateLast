using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.Finance;
using ShipRate.Application.Interfaces;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Application.Utilities;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Infrastructure.Data;

namespace ShipRate.Application.Services.Finance
{
    public class PaymentDocumentService : IPaymentDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private const int ChequePaymentMethodId = 3;
        public PaymentDocumentService(ApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<PagedResultDto<PaymentDocumentListItemVm>> GetPagedAsync(PaymentDocumentSearchVm searchModel)
        {
            searchModel ??= new PaymentDocumentSearchVm();

            var query = _context.PaymentDocuments
                .AsNoTracking()
                //.Include(x => x.Party)
                //.Include(x => x.Company)
                //.Include(x => x.Currency)
                //.Include(x => x.FinanceDirection)
                //.Include(x => x.PaymentMethod)
                //.Include(x => x.Cheque)
                .Where(x => !x.IsDeleted)
                .AsQueryable();


            if (searchModel.FinanceDirectionId != null)
            {
                query = query.Where(x => x.FinanceDirectionId == searchModel.FinanceDirectionId);
            }

            if (!string.IsNullOrWhiteSpace(searchModel.DocumentNumber))
            {
                var docNo = searchModel.DocumentNumber.Trim();
                query = query.Where(x => x.DocumentNo != null && x.DocumentNo.Contains(docNo));
            }

            if (!string.IsNullOrWhiteSpace(searchModel.PersonName))
            {
                var personName = searchModel.PersonName.Trim();
                query = query.Where(x => x.Party != null && x.Party.PersonName != null && x.Party.PersonName.Contains(personName));
            }

            if (!string.IsNullOrWhiteSpace(searchModel.TrackingNumber))
            {
                var tracking = searchModel.TrackingNumber.Trim();
                query = query.Where(x => x.TrackingNo != null && x.TrackingNo.Contains(tracking));
            }

            if (searchModel.CompanyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == searchModel.CompanyId.Value);
            }

            if (searchModel.PaymentMethodId.HasValue)
            {
                query = query.Where(x => x.PaymentMethodId == searchModel.PaymentMethodId.Value);
            }

            if (searchModel.CurrencyId.HasValue)
            {
                query = query.Where(x => x.CurrencyId == searchModel.CurrencyId.Value);
            }

            if (searchModel.FinanceDirectionId.HasValue)
            {
                query = query.Where(x => x.FinanceDirectionId == searchModel.FinanceDirectionId.Value);
            }

            if (searchModel.ChequePurposeId.HasValue)
            {
                query = query.Where(x => x.Cheque.ChequePurposeId == searchModel.ChequePurposeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchModel.FromDateShamsi))
            {
                var fromDate = ParseShamsiToGregorian(searchModel.FromDateShamsi);
                if (fromDate.HasValue)
                {
                    query = query.Where(x => x.DocumentDate >= fromDate.Value.Date);
                }
            }
            if (searchModel.PersonId.HasValue)
            {
                query = query.Where(x => x.PartyId == searchModel.PersonId);
            }
            if (!string.IsNullOrWhiteSpace(searchModel.ToDateShamsi))
            {
                var toDate = ParseShamsiToGregorian(searchModel.ToDateShamsi);
                if (toDate.HasValue)
                {
                    var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(x => x.DocumentDate <= endOfDay);
                }
            }

            if (searchModel.OnlyHasRemaining)
            {
                query = query.Where(x => (x.Amount - x.AllocatedAmount) > 0);
            }


            var totalCount = await query.CountAsync();

            var page = searchModel.Page <= 0 ? 1 : searchModel.Page;
            var pageSize = searchModel.PageSize <= 0 ? 10 : searchModel.PageSize;

            var items = await query
                .OrderByDescending(x => x.SystemId)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentDocumentListItemVm
                {
                    Id = x.Id,
                    DocumentNumber = x.DocumentNo,
                    DocumentDateGregorian = x.DocumentDate,
                    DocumentDateShamsi = x.DocumentDate != null
                        ? PersianDateHelper.ToShamsi(x.DocumentDate)
                        : null,
                    PersonName = x.Party != null ? x.Party.PersonName : null,
                    CompanyName = x.Company != null ? x.Company.Person.PersonName : null,
                    CurrencyTitle = x.Currency != null ? x.Currency.CurrencyCode : null,
                    FinanceDirectionTitle = x.FinanceDirection != null ? x.FinanceDirection.PaymentDocumentContextName : null,
                    PaymentMethodTitle = x.PaymentMethod != null ? x.PaymentMethod.PaymentMethodName : null,
                    Amount = x.Amount,
                    AllocatedAmount = x.AllocatedAmount,
                    UnAllocatedAmount = x.UnallocatedAmount,
                    TrackingNumber = x.TrackingNo,
                    HasCheque = x.Cheque != null && !x.Cheque.IsDeleted,
                    SystemId = x.SystemId,
                    CreatedAt = x.CreatedAt,
                    CreateUserName = x.CreateUser.FullName,
                    UpdatedAt = x.UpdatedAt,
                    UpdateUserName = x.EditUser.FullName,
                    HasVaultTransaction = _context.VaultMovements.Any(y => y.SourceEntityId == x.Id && y.VaultMovementSourceTypeId == 1),
                    ExchangeRate = x.ExchangeRate,
                    HasThirdParty = x.IsThirdPartySettlement,
                    ThirdPartyName = x.ThirdParty == null ? string.Empty : x.ThirdParty.PersonName,
                    ThirdPartyRoleName = x.ThirdPartyRole.ToString() ?? string.Empty,
                    RelatedPaymentSystemId = x.RelatedPaymentDocument == null ? null : x.RelatedPaymentDocument.SystemId,
                    ChequeStatusName = x.Cheque == null ? "-" : x.Cheque.Status.ChequeStatusName,
                    ChequeStatusDate = x.Cheque == null ? null : x.Cheque.LastStatusDate,
                    ChequeId = x.Cheque == null ? null : x.Cheque.Id,
                })
                .ToListAsync();
            return new PagedResultDto<PaymentDocumentListItemVm>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PaymentDocumentCreateEditVm> GetCreateModelAsync()
        {
            var model = new PaymentDocumentCreateEditVm
            {
                DocumentDateShamsi = PersianDateHelper.ToShamsi(DateTime.Now),
                Cheque = new ChequeFormVm
                {
                    ChequeDateShamsi = PersianDateHelper.ToShamsi(DateTime.Now)
                }
            };

            await FillLookupListsAsync(model);
            return model;
        }
        public async Task<PaymentDocumentCreateEditVm?> GetEditModelAsync(Guid id)
        {
            var entity = await _context.PaymentDocuments
                .AsNoTracking()
                .Include(x => x.Cheque)
                .Include(x => x.ThirdParty)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            var model = new PaymentDocumentCreateEditVm
            {
                Id = entity.Id,
                DocumentNumber = entity.DocumentNo ?? string.Empty,
                DocumentDateGregorian = entity.DocumentDate,
                DocumentDateShamsi = entity.DocumentDate != null
                    ? PersianDateHelper.ToShamsi(entity.DocumentDate)
                    : string.Empty,
                CompanyId = entity.CompanyId,
                PersonId = entity.PartyId,
                CurrencyId = entity.CurrencyId,
                FinanceDirectionId = entity.FinanceDirectionId,
                PaymentMethodId = entity.PaymentMethodId,
                Amount = entity.Amount,
                TrackingNumber = entity.TrackingNo,
                Description = entity.Description,
                BranchId = entity.BranchId,
                BankAccountId = entity.BankAccountId,
                BankId = entity.BankId,
                HasThirdParty = entity.IsThirdPartySettlement,
                ThirdPartyId = entity.ThirdPartyId,
                ExchangeRate = entity.ExchangeRate == null ? 1 : entity.ExchangeRate.Value,
                ThirdPartyRole = entity.ThirdPartyRole,
                ThirdPartyName = entity.ThirdParty?.PersonName,
                PaymentRequestId = entity.PaymentRequestId,
                Cheque = new ChequeFormVm()
            };

            if (entity.PartyId.HasValue)
            {
                model.PersonName = await _context.Persons
                    .AsNoTracking()
                    .Where(x => x.Id == entity.PartyId.Value && !x.IsDeleted)
                    .Select(x => x.PersonName)
                    .FirstOrDefaultAsync();
            }

            if (entity.Cheque != null && !entity.Cheque.IsDeleted)
            {
                model.Cheque = new ChequeFormVm
                {
                    Id = entity.Cheque.Id,
                    ChequeNumber = entity.Cheque.ChequeNo,
                    ChequeDateGregorian = entity.Cheque.DueDate,
                    ChequeDateShamsi = entity.Cheque.DueDate != null
                        ? PersianDateHelper.ToShamsi(entity.Cheque.DueDate)
                        : null,
                    ChequeTypeId = entity.Cheque.ChequeTypeId,
                    BankId = entity.Cheque.BankId,
                    BankAccountId = entity.Cheque.BankAccountId,
                    AccountHolderName = entity.Cheque.IssuerName,
                    Description = entity.Cheque.Remarks,
                    ChequePurposeId = entity.Cheque.ChequePurposeId,
                    BankAccountNo = entity.Cheque.BankAccountNo,
                    SayadNo = entity.Cheque.SayadNo,
                    IBAN = entity.Cheque.IBAN,
                };
            }

            await FillLookupListsAsync(model);
            return model;
        }
        public async Task<Guid> CreateAsync(PaymentDocumentCreateEditVm model, string? currentUserId = null)
        {
            NormalizeModel(model);
            await FillLookupListsAsync(model);

            var validationErrors = await ValidateBusinessAsync(model);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" | ", validationErrors));
            }

            var documentDate = ParseShamsiToGregorian(model.DocumentDateShamsi);
            if (!documentDate.HasValue)
            {
                throw new InvalidOperationException("Document date is invalid.");
            }

            var entity = new PaymentDocument
            {
                Id = Guid.CreateVersion7(),
                CompanyId = model.CompanyId.Value,
                BranchId = model.BranchId,
                DocumentNo = model.DocumentNumber?.Trim(),
                DocumentDate = documentDate.Value,
                PartyId = model.PersonId,
                CurrencyId = model.CurrencyId.Value,
                FinanceDirectionId = model.FinanceDirectionId.Value,
                PaymentMethodId = model.PaymentMethodId.Value,
                Amount = model.Amount,
                TrackingNo = model.TrackingNumber?.Trim(),
                Description = model.Description?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = _currentUserService.UserId,
                BankAccountId = model.BankAccountId,
                IsThirdPartySettlement = model.HasThirdParty,
                ThirdPartyId = model.ThirdPartyId,
                BankId = model.BankId,
                ExchangeRate = model.ExchangeRate,
                ThirdPartyRole = model.ThirdPartyRole == null ? ThirdPartyRole.None : model.ThirdPartyRole.Value,
                PaymentRequestId = model.PaymentRequestId
            };

            if (RequiresCheque(model.PaymentMethodId))
            {
                entity.Cheque = BuildChequeEntityFromModel(model.Cheque, _currentUserService.UserId.ToString());
                entity.Cheque.CurrencyId = model.CurrencyId.Value;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.PaymentDocuments.Add(entity);

                // اگر شرایط Vendor Offset برقرار باشد، سند دوم (Outbound / متناظر) ساخته می‌شود
                if (model.ThirdPartyRole.HasValue &&
                    model.ThirdPartyId.HasValue &&
                    model.ThirdPartyId != Guid.Empty &&
                    model.ThirdPartyRole.Value == ThirdPartyRole.VendorOffset &&
                    model.FinanceDirectionId == 1)
                {
                    var entity2 = new PaymentDocument
                    {
                        Id = Guid.CreateVersion7(),
                        CompanyId = model.CompanyId.Value,
                        BranchId = model.BranchId,
                        DocumentNo = model.DocumentNumber?.Trim(),
                        DocumentDate = documentDate.Value,
                        PartyId = model.ThirdPartyId.Value,
                        CurrencyId = model.CurrencyId.Value,
                        FinanceDirectionId = 2,
                        PaymentMethodId = model.PaymentMethodId.Value,
                        Amount = model.Amount,
                        TrackingNo = model.TrackingNumber?.Trim(),
                        Description = model.Description?.Trim() + " (Vendor Offset Mirror)",
                        IsDeleted = false,
                        CreatedAt = DateTime.Now,
                        CreateUserId = _currentUserService.UserId,
                        BankAccountId = model.BankAccountId,
                        IsThirdPartySettlement = false,
                        ThirdPartyRole = ThirdPartyRole.None,
                        BankId = model.BankId,
                        ExchangeRate = model.ExchangeRate,
                        RelatedPaymentDocumentId = entity.Id
                    };
                    _context.PaymentDocuments.Add(entity2);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return entity.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> UpdateAsync(PaymentDocumentCreateEditVm model)
        {
            NormalizeModel(model);
            await FillLookupListsAsync(model);

            var validationErrors = await ValidateBusinessAsync(model);
            if (validationErrors.Any())
            {
                throw new InvalidOperationException(string.Join(" | ", validationErrors));
            }

            var currentuserId = _currentUserService.UserId;

            var entity = await _context.PaymentDocuments
                .Include(x => x.Cheque)
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.IsDeleted);

            if (entity == null)
                return false;

            var documentDate = ParseShamsiToGregorian(model.DocumentDateShamsi);
            if (!documentDate.HasValue)
            {
                throw new InvalidOperationException("Document date is invalid.");
            }

            entity.DocumentNo = model.DocumentNumber?.Trim();
            entity.DocumentDate = documentDate.Value;
            entity.CompanyId = model.CompanyId.Value;
            entity.PartyId = model.PersonId.Value;
            entity.CurrencyId = model.CurrencyId.Value;
            entity.FinanceDirectionId = model.FinanceDirectionId.Value;
            entity.PaymentMethodId = model.PaymentMethodId.Value;
            entity.Amount = model.Amount;
            entity.TrackingNo = model.TrackingNumber?.Trim();
            entity.Description = model.Description?.Trim();
            entity.UpdatedAt = DateTime.Now;
            entity.EditUserId = currentuserId;
            entity.BankAccountId = model.BankAccountId;
            entity.PaymentRequestId = model.PaymentRequestId;

            entity.IsThirdPartySettlement = model.HasThirdParty;
            entity.ThirdPartyId = model.ThirdPartyId;
            entity.BankId = model.BankId;
            entity.ExchangeRate = model.ExchangeRate;
            entity.ThirdPartyRole = model.ThirdPartyRole == null ? ThirdPartyRole.None : model.ThirdPartyRole.Value;

            if (RequiresCheque(model.PaymentMethodId))
            {
                if (entity.Cheque == null || entity.Cheque.IsDeleted)
                {
                    entity.Cheque = BuildChequeEntityFromModel(model.Cheque, currentuserId.ToString());
                }
                else
                {
                    UpdateChequeEntityFromModel(entity.Cheque, model.Cheque, currentuserId.ToString());
                }
            }
            else
            {
                if (entity.Cheque != null && !entity.Cheque.IsDeleted)
                {
                    entity.Cheque.IsDeleted = true;
                    entity.Cheque.UpdatedAt = DateTime.Now;
                    //entity.Cheque.ModifiedBy = currentUserId;
                }
            }




            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SoftDeleteAsync(Guid id, string? currentUserId = null)
        {
            var entity = await _context.PaymentDocuments
                .Include(x => x.Cheque)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return false;








            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.Now;
            //entity.EditUserId = currentUserId;

            if (entity.Cheque != null && !entity.Cheque.IsDeleted)
            {
                entity.Cheque.IsDeleted = true;
                entity.Cheque.UpdatedAt = DateTime.Now;
                //entity.Cheque.ModifiedBy = currentUserId;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<PaymentDocumentDetailsVm?> GetDetailsAsync(Guid id)
        {
            var entity = await _context.PaymentDocuments
                .AsNoTracking()
                .Include(x => x.Party)
                .Include(x => x.Company)
                .ThenInclude(x => x.Person)
                .Include(x => x.Currency)
                .Include(x => x.FinanceDirection)
                .Include(x => x.PaymentMethod)
                .Include(x => x.Cheque)
                    .ThenInclude(x => x.Bank)
                .Include(x => x.Cheque)
                    .ThenInclude(x => x.BankAccount)
                .Include(x => x.Cheque)
                    .ThenInclude(x => x.ChequeType)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return null;

            return new PaymentDocumentDetailsVm
            {
                Id = entity.Id,
                DocumentNumber = entity.DocumentNo,
                DocumentDateGregorian = entity.DocumentDate,
                DocumentDateShamsi = entity.DocumentDate != null
                    ? PersianDateHelper.ToShamsi(entity.DocumentDate)
                    : null,
                CompanyName = entity.Company?.Person.PersonName,
                PersonName = entity.Party?.PersonName,
                CurrencyTitle = entity.Currency?.CurrencyName,
                FinanceDirectionTitle = entity.FinanceDirection?.FinanceDirectionName,
                PaymentMethodTitle = entity.PaymentMethod?.PaymentMethodName,
                Amount = entity.Amount,
                TrackingNumber = entity.TrackingNo,
                Description = entity.Description,
                Cheque = entity.Cheque != null && !entity.Cheque.IsDeleted
                    ? new ChequeDetailsVm
                    {
                        Id = entity.Cheque.Id,
                        ChequeNumber = entity.Cheque.ChequeNo,
                        ChequeDateGregorian = entity.Cheque.DueDate,
                        ChequeDateShamsi = entity.Cheque.DueDate != null
                            ? PersianDateHelper.ToShamsi(entity.Cheque.DueDate)
                            : null,
                        ChequeTypeTitle = entity.Cheque.ChequeType?.ChequeTypeName,
                        BankName = entity.Cheque.Bank?.BankName,
                        BankAccountNumber = entity.Cheque.BankAccountNo,
                        AccountHolderName = entity.Cheque.IssuerName,
                        Description = entity.Cheque.Remarks

                    }
                    : null
            };
        }
        public async Task<List<PersonLookupVm>> SearchPersonsAsync(string? term, int take = 10)
        {
            take = take <= 0 ? 10 : take;

            var query = _context.Persons
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(term))
            {
                term = term.Trim();

                query = query.Where(x =>
                    (x.PersonName != null && x.PersonName.Contains(term)) ||
                    (x.NationalID != null && x.NationalID.Contains(term)));
            }

            return await query
                .OrderBy(x => x.PersonName)
                .Take(take)
                .Select(x => new PersonLookupVm
                {
                    Id = x.Id,
                    FullName = x.PersonName ?? string.Empty,
                    NationalCode = x.NationalID
                })
                .ToListAsync();
        }
        public async Task<List<SelectListItem>> GetBankAccountsByBankIdAsync(Guid? bankId)
        {
            //if (!bankId.HasValue)
            //    return new List<SelectListItem>();

            if (bankId == null || !bankId.HasValue)
            {
                return await _context.BankAccounts
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.BankAccountTitle)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.BankAccountTitle
                    })
                    .ToListAsync();
            }
            else
            {
                return await _context.BankAccounts
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.BankId == bankId.Value)
                    .OrderBy(x => x.BankAccountTitle)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.BankAccountTitle
                    })
                    .ToListAsync();
            }
        }
        private async Task FillLookupListsAsync(PaymentDocumentCreateEditVm model)
        {
            var currentDocumentId = model.Id; // در Create برابر Guid.Empty است
            var requests = await _context.PaymentRequests
                .AsNoTracking()
                .Where(x => x.StatusId == 30)
                .Where(x => !_context.PaymentDocuments.Any(document =>
                    document.PaymentRequestId == x.Id &&
                    document.Id != currentDocumentId))
                .OrderByDescending(x => x.RequestNo)
                .ThenByDescending(x => x.Id)
                .Select(x => new PaymentRequestFormOptionVm
                {
                    Id = x.Id,
                    RequestNo = x.RequestNo,
                    CompanyId = x.CompanyId,
                    BranchId = x.BranchId,
                    FinanceDirectionId = x.FinanceDirectionId,
                    PartyId = x.PartyId,
                    TotalAmount = x.TotalAmount,
                    CurrencyId = x.CurrencyId,
                    PreferredPaymentMethodId = x.PreferredPaymentMethodId,
                    Description = x.Description,
                    PartyName = x.Party.PersonName,
                    PartyLocalName = x.Party.PersonPersianName,
                    CurrencyCode = x.Currency.CurrencyCode
                })
                .ToListAsync();
            model.PaymentRequests = requests;

            model.Companies = await _context.Companies
                .Include(x => x.Person)
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Person.PersonName,
                    Selected = model.CompanyId.HasValue && model.CompanyId.Value == x.Id
                })
                .ToListAsync();


            model.Branches = await _context.Branches
                .Include(x => x.Parent)
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Parent.PersonName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.BranchName + "(" + x.Parent.PersonName + ")",
                    Selected = model.BranchId.HasValue && model.BranchId == x.Id
                })
                .ToListAsync();




            model.Currencies = await _context.Currencies
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive == true)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.CurrencyName,
                    Selected = model.CurrencyId.HasValue && model.CurrencyId.Value == x.Id
                })
                .ToListAsync();

            model.FinanceDirections = await _context.FinanceDirections
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.PaymentDocumentContextName,
                    Selected = model.FinanceDirectionId.HasValue && model.FinanceDirectionId.Value == x.Id
                })
                .ToListAsync();

            model.PaymentMethods = await _context.PaymentMethods
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = $"{x.PaymentMethodName}-{x.PaymentMethodLocalName}",
                    Selected = model.PaymentMethodId.HasValue && model.PaymentMethodId.Value == x.Id
                })
                .ToListAsync();
            model.Banks = await _context.Banks
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.BankName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.BankName,
                    Selected = model.Cheque.BankId.HasValue && model.Cheque.BankId == x.Id
                })
                .ToListAsync();

            model.ChequeTypes = await _context.ChequeTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.ChequeTypeName + "-" + x.ChequeTypeLocalName,
                    Selected = model.Cheque.ChequeTypeId.HasValue && model.Cheque.ChequeTypeId.Value == x.Id
                })
                .ToListAsync();


            model.ChequePurposes = await _context.ChequePurposes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.ChequePurposeName + "-" + x.ChequePurposeLocalName,
                    Selected = model.Cheque.ChequePurposeId.HasValue && model.Cheque.ChequePurposeId.Value == x.Id
                })
                .ToListAsync();

            model.ChequeStatus = await _context.ChequeStatuses
               .AsNoTracking()
               .Where(x => !x.IsDeleted)
               .OrderBy(x => x.Id)
               .Select(x => new SelectListItem
               {
                   Value = x.Id.ToString(),
                   Text = x.ChequeStatusName + "-" + x.ChequeStatusLocalName,
                   //Selected = model.Cheque.cheque.HasValue && model.Cheque.ChequePurposeId.Value == x.Id
               })
               .ToListAsync();

            model.BankAccounts = await GetBankAccountsByBankIdAsync(null);
            //model.BankAccounts = await GetBankAccountsByBankIdAsync(model.Cheque.BankId);
            //if (model.Cheque.BankAccountId.HasValue)
            //{
            //    foreach (var item in model.BankAccounts)
            //    {
            //        item.Selected = item.Value == model.Cheque.BankAccountId.Value.ToString();
            //    }
            //}
        }
        private async Task<List<string>> ValidateBusinessAsync(PaymentDocumentCreateEditVm model)
        {
            var errors = new List<string>();

            //if (string.IsNullOrWhiteSpace(model.DocumentDateShamsi) || !ParseShamsiToGregorian(model.DocumentDateShamsi).HasValue)
            //{
            //    errors.Add("Document date is invalid.");
            //}

            if (model.CompanyId.HasValue)
            {
                var companyExists = await _context.Companies.AnyAsync(x => x.Id == model.CompanyId.Value && !x.IsDeleted);
                if (!companyExists)
                    errors.Add("Selected company is invalid.");
            }

            if (model.PersonId.HasValue)
            {
                var personExists = await _context.Persons.AnyAsync(x => x.Id == model.PersonId.Value && !x.IsDeleted);
                if (!personExists)
                    errors.Add("Selected person is invalid.");
            }

            if (model.CurrencyId.HasValue)
            {
                var currencyExists = await _context.Currencies.AnyAsync(x => x.Id == model.CurrencyId.Value && !x.IsDeleted);
                if (!currencyExists)
                    errors.Add("Selected currency is invalid.");
            }

            if (model.FinanceDirectionId.HasValue)
            {
                var directionExists = await _context.FinanceDirections.AnyAsync(x => x.Id == model.FinanceDirectionId.Value && !x.IsDeleted);
                if (!directionExists)
                    errors.Add("Selected finance direction is invalid.");
            }

            if (model.PaymentMethodId.HasValue)
            {
                var paymentMethodExists = await _context.PaymentMethods.AnyAsync(x => x.Id == model.PaymentMethodId.Value && !x.IsDeleted);
                if (!paymentMethodExists)
                    errors.Add("Selected payment method is invalid.");
            }

            if (RequiresCheque(model.PaymentMethodId))
            {
                if (string.IsNullOrWhiteSpace(model.Cheque.ChequeDateShamsi) || !ParseShamsiToGregorian(model.Cheque.ChequeDateShamsi).HasValue)
                {
                    errors.Add("Cheque date is invalid.");
                }

                if (model.Cheque.BankId.HasValue)
                {
                    var bankExists = await _context.Banks.AnyAsync(x => x.Id == model.Cheque.BankId.Value && !x.IsDeleted);
                    if (!bankExists)
                        errors.Add("Selected bank is invalid.");
                }

                if (model.Cheque.BankAccountId.HasValue)
                {
                    var bankAccountExists = await _context.BankAccounts.AnyAsync(x =>
                        x.Id == model.Cheque.BankAccountId.Value &&
                        !x.IsDeleted &&
                        (!model.Cheque.BankId.HasValue || x.BankId == model.Cheque.BankId.Value));

                    if (!bankAccountExists)
                        errors.Add("Selected bank account is invalid.");
                }

                if (model.Cheque.ChequeTypeId.HasValue)
                {
                    var chequeTypeExists = await _context.ChequeTypes.AnyAsync(x => x.Id == model.Cheque.ChequeTypeId.Value && !x.IsDeleted);
                    if (!chequeTypeExists)
                        errors.Add("Selected cheque type is invalid.");
                }
            }
            return errors;
        }
        private static void NormalizeModel(PaymentDocumentCreateEditVm model)
        {
            model.DocumentNumber = model.DocumentNumber?.Trim() ?? string.Empty;
            model.DocumentDateShamsi = model.DocumentDateShamsi?.Trim() ?? string.Empty;
            model.TrackingNumber = model.TrackingNumber?.Trim();
            model.Description = model.Description?.Trim();
            model.PersonName = model.PersonName?.Trim();

            model.Cheque ??= new ChequeFormVm();
            model.Cheque.ChequeNumber = model.Cheque.ChequeNumber?.Trim();
            model.Cheque.ChequeDateShamsi = model.Cheque.ChequeDateShamsi?.Trim();
            model.Cheque.AccountHolderName = model.Cheque.AccountHolderName?.Trim();
            model.Cheque.Description = model.Cheque.Description?.Trim();
        }
        private static bool RequiresCheque(int? paymentMethodId)
        {
            return paymentMethodId.HasValue && paymentMethodId.Value == ChequePaymentMethodId;
        }
        private static DateTime? ParseShamsiToGregorian(string? shamsiDate)
        {
            if (string.IsNullOrWhiteSpace(shamsiDate))
                return null;

            try
            {
                return PersianDateHelper.ToGregorian(shamsiDate);
            }
            catch
            {
                return null;
            }
        }
        private Cheque BuildChequeEntityFromModel(ChequeFormVm model, string? currentUserId)
        {
            var chequeDate = ParseShamsiToGregorian(model.ChequeDateShamsi);
            return new Cheque
            {
                ChequeNo = model.ChequeNumber?.Trim(),
                DueDate = chequeDate.GetValueOrDefault(),
                ChequeTypeId = model.ChequeTypeId.Value,
                ChequePurposeId = model.ChequePurposeId.Value,
                BankId = model.BankId.Value,
                BankAccountNo = model.BankAccountNo,
                IssuerName = model.AccountHolderName?.Trim(),
                SayadNo = model.SayadNo,
                IBAN = model.IBAN,
                Remarks = "-",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = _currentUserService.UserId,
                StatusId = 10, //Draft
                ChequeInquiryResultText = model.ChequeInquiryResultText,
            };
        }
        private void UpdateChequeEntityFromModel(Cheque entity, ChequeFormVm model, string? currentUserId)
        {
            var chequeDate = ParseShamsiToGregorian(model.ChequeDateShamsi);

            entity.ChequeNo = model.ChequeNumber?.Trim();
            entity.DueDate = chequeDate.GetValueOrDefault();
            entity.ChequeTypeId = model.ChequeTypeId.Value;
            entity.ChequePurposeId = model.ChequePurposeId.Value;
            entity.BankId = model.BankId.Value;
            //entity.BankAccountId = model.BankAccountId;
            entity.BankAccountNo = model.BankAccountNo;
            entity.IssuerName = model.AccountHolderName?.Trim();
            entity.Remarks = "-";
            entity.IsDeleted = false;
            entity.SayadNo = model.SayadNo;
            entity.IBAN = model.IBAN;
            entity.UpdatedAt = DateTime.Now;
            entity.EditUserId = Guid.Parse(currentUserId);
        }
        public async Task<IEnumerable<PaymentDocumentListItemVm>> GetEligibleAsync(int companyId, Guid partyId, int treasuryDocumentTypeId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public async Task<List<AvailablePaymentDocDto>> GetAvailablePaymentDocsByPartyAsync(Guid partyId)
        {
            var availablePaymentDocs = await _context.PaymentDocuments
                //.Where(p.UnallocatedAmount > 0)
                .Where(x => x.PartyId == partyId)
                .Select(p => new AvailablePaymentDocDto
                {
                    PaymentDocumentId = p.Id,
                    DocumentNo = p.DocumentNo,
                    DocumentDate = p.DocumentDate,
                    TotalAmount = p.Amount,
                    UnallocatedAmount = p.UnallocatedAmount,
                    CurrencyId = p.CurrencyId,
                    CurrencyCode = p.Currency.CurrencyCode,
                    PaymentMethodName = p.PaymentMethod.PaymentMethodName
                })
                .ToListAsync();
            return availablePaymentDocs;
        }
        public async Task<List<AvailablePaymentDocDto>> GetAvailablePaymentDocumentsAsync(
            Guid partyId,
            int? currencyId = null,
            int? companyId = null,
            Guid? lawyerId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.PaymentDocuments
                .AsNoTracking()
                .Where(p => !p.IsDeleted);
            if (lawyerId != null && lawyerId.HasValue && lawyerId != Guid.Empty)
            {
                query = query.Where(p => p.PartyId == partyId || p.PartyId == lawyerId);
            }
            else
            {
                query = query.Where(p => p.PartyId == partyId);
            }


            // در صورتی که ارز مشخص شده باشد، اسناد با همان ارز فیلتر می‌شوند
            if (currencyId.HasValue && currencyId.Value > 0)
            {
                query = query.Where(p => p.CurrencyId == currencyId.Value);
            }
            if (companyId.HasValue && companyId.Value > 0)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }
            // فیلتر اسنادی که مانده قابل تخصیص دارند
            // (از آنجایی که AllocatedAmount را Sync نگه می‌داریم، محاسبه به صورت Amount - AllocatedAmount است)
            query = query.Where(p => (p.Amount - p.AllocatedAmount) > 0);

            var result = await query
                .OrderByDescending(p => p.DocumentDate)
                .Select(p => new AvailablePaymentDocDto
                {
                    PaymentDocumentId = p.Id,
                    DocumentNo = p.DocumentNo,
                    DocumentDate = p.DocumentDate,
                    PaymentMethodName = p.PaymentMethod != null ? p.PaymentMethod.PaymentMethodName : "N/A",
                    TotalAmount = p.Amount,
                    UnallocatedAmount = p.Amount - p.AllocatedAmount,
                    CurrencyId = p.CurrencyId,
                    CurrencyCode = p.Currency != null ? p.Currency.CurrencyCode : "",
                    PayerName = p.Party.PersonName
                })
                .ToListAsync(cancellationToken);
            return result;
        }
        public async Task<bool> CanBeDeletedPaymentDocumentAsync(Guid id)
        {
            if (await _context.SettlementItemPaymentAllocations.AnyAsync(x => x.PaymentDocumentId == id) || await _context.ReturnCashRequests.AnyAsync(x => x.PaymentDocumentId == id)
                 || await _context.TreasuryDocumentItems.AnyAsync(x => x.PaymentDocumentId == id)) return false;
            if (await _context.VaultMovements.AnyAsync(x => x.SourceEntityId == id && x.VaultMovementSourceTypeId == 1)) return false;
            return true;
        }
        public async Task<PaymentDocumentUsagePanelVm?> GetUsagePanelAsync(
                Guid paymentDocumentId,
                CancellationToken cancellationToken = default)
        {
            var payment = await _context.PaymentDocuments
                .AsNoTracking()
                .Where(x => x.Id == paymentDocumentId)
                .Select(x => new PaymentDocumentUsagePanelVm
                {
                    PaymentDocumentId = x.Id,
                    PaymentDocumentNumber = x.DocumentNo,
                    Amount = x.Amount,
                    CurrencyTitle = x.Currency.CurrencyCode
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (payment is null)
                return null;

            payment.Items = await _context.SettlementItemPaymentAllocations
                .AsNoTracking()
                .Where(x => x.PaymentDocumentId == paymentDocumentId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new PaymentDocumentUsageItemVm
                {
                    UsageType = "Settlement",

                    ReferenceNumber = x.Settlement.SettlementNo.ToString(),
                    ReferenceDate = x.Settlement.CreatedAt.ToString(),
                    PartyName = x.Settlement.Party.PersonName,
                    AllocatedAmount = x.AllocatedAmount,
                    CurrencyTitle = payment.CurrencyTitle,
                    Description = x.Remarks
                })
                .ToListAsync(cancellationToken);

            payment.AllocatedAmount = payment.Items.Sum(x => x.AllocatedAmount);
            payment.RemainingAmount = payment.Amount - payment.AllocatedAmount;
            for (var index = 0; index < payment.Items.Count; index++)
            {
                payment.Items[index].RowNumber = index + 1;
            }
            return payment;
        }
        public async Task<List<PaymentDocumentLookupDto>> GetAvailablePaymentDocumentsForVaultAsync()
        {
            return await _context.PaymentDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                            x.PaymentMethodId == 1 &&
                            !_context.VaultMovements.Any(vm => vm.SourceEntityId == x.Id))
                .OrderByDescending(x => x.DocumentDate)
                .Select(x => new PaymentDocumentLookupDto
                {
                    Id = x.Id,
                    DocumentNo = x.DocumentNo ?? x.TrackingNo,
                    PayerName = x.Party != null ? x.Party.PersonName : "-",
                    DocumentDate = x.DocumentDate,
                    Amount = x.Amount,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency != null ? x.Currency.CurrencyCode : "N/A"
                })
                .ToListAsync();
        }

        //-----------------------------Report Payment Documents------------------------
        public async Task<PagedResultDto<PaymentDocument_ReportListItemVm>> GetPagedReportAsync(PaymentDocument_ReportsSearchVm searchModel)
        {
            searchModel ??= new PaymentDocument_ReportsSearchVm();

            var query = _context.PaymentDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            //Search
            //1
            if (searchModel.CompanyId.HasValue)
            {
                query = query.Where(x => x.CompanyId == searchModel.CompanyId.Value);
            }
            //2
            if (!string.IsNullOrWhiteSpace(searchModel.DocumentNo))
            {
                var docNo = searchModel.DocumentNo.Trim();
                query = query.Where(x => x.DocumentNo != null && x.DocumentNo.Contains(docNo));
            }
            //3
            if (searchModel.PaymentMethodId.HasValue)
            {
                query = query.Where(x => x.PaymentMethodId == searchModel.PaymentMethodId.Value);
            }
            //4
            if (searchModel.FinanceDirectionId != null)
            {
                query = query.Where(x => x.FinanceDirectionId == searchModel.FinanceDirectionId);
            }
            //5
            if (searchModel.PartyId.HasValue)
            {
                query = query.Where(x => x.PartyId == searchModel.PartyId);
            }
            //6
            if (searchModel.CurrencyId.HasValue)
            {
                query = query.Where(x => x.CurrencyId == searchModel.CurrencyId.Value);
            }
            //7
            if (searchModel.DocumentDateFrom.HasValue)
                query = query.Where(x => x.DocumentDate >= searchModel.DocumentDateFrom.Value);
            //8
            if (searchModel.DocumentDateTo.HasValue)
                query = query.Where(x => x.DocumentDate <= searchModel.DocumentDateTo.Value);
            //9
            if (searchModel.EffectiveDateFrom.HasValue)
                query = query.Where(x => x.EffectiveDate >= searchModel.EffectiveDateFrom.Value);
            //10
            if (searchModel.EffectiveDateTo.HasValue)
                query = query.Where(x => x.EffectiveDate <= searchModel.EffectiveDateTo.Value);
            //11
            if (searchModel.AmountFrom.HasValue)
                query = query.Where(x => x.Amount >= searchModel.AmountFrom.Value);
            //12
            if (searchModel.AmountTo.HasValue)
                query = query.Where(x => x.Amount <= searchModel.AmountTo.Value);
            //13
            if (searchModel.BankId.HasValue)
            {
                query = query.Where(x => x.BankId == searchModel.BankId);
            }
            //14
            if (searchModel.BankAccountId.HasValue)
            {
                query = query.Where(x => x.BankAccountId == searchModel.BankAccountId);
            }
            //15
            if (!string.IsNullOrWhiteSpace(searchModel.RefferencesNo))
            {
                var RefNo = searchModel.RefferencesNo.Trim();
                query = query.Where(x => x.ReferenceNo != null && x.ReferenceNo.Contains(RefNo));
            }
            //16
            if (!string.IsNullOrWhiteSpace(searchModel.TrackingNo))
            {
                var TrackNo = searchModel.TrackingNo.Trim();
                query = query.Where(x => x.TrackingNo != null && x.TrackingNo.Contains(TrackNo));
            }
            //17
            if (!string.IsNullOrWhiteSpace(searchModel.Description))
            {
                var desc = searchModel.Description.Trim();
                query = query.Where(x => x.Description != null && x.Description.Contains(desc));
            }
            //18
            if (searchModel.IsAdvancedPayment.HasValue)
            {
                bool adv = searchModel.IsAdvancedPayment.Value == 1;
                query = query.Where(x => x.IsAdvancePayment == adv);
            }
            //19
            if (searchModel.CreatedAtFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= searchModel.CreatedAtFrom.Value);
            //20
            if (searchModel.CreatedAtTo.HasValue)
                query = query.Where(x => x.CreatedAt <= searchModel.CreatedAtTo.Value);
            //21
            if (searchModel.UpdatedAtFrom.HasValue)
                query = query.Where(x => x.UpdatedAt >= searchModel.UpdatedAtFrom.Value);
            //22
            if (searchModel.UpdatedAtTo.HasValue)
                query = query.Where(x => x.UpdatedAt <= searchModel.UpdatedAtTo.Value);
            //23
            if (!string.IsNullOrWhiteSpace(searchModel.CreateUserName))
                query = query.Where(x => x.CreateUser.FullName.Contains(searchModel.CreateUserName));
            //24
            if (!string.IsNullOrWhiteSpace(searchModel.UpdateUserName))
                query = query.Where(x => x.EditUser.FullName.Contains(searchModel.UpdateUserName));
            //25
            if (searchModel.BranchId.HasValue)
            {
                query = query.Where(x => x.BranchId == searchModel.BranchId);
            }
            //26
            if (searchModel.AllocatedAmountFrom.HasValue)
                query = query.Where(x => x.AllocatedAmount >= searchModel.AllocatedAmountFrom.Value);
            //27
            if (searchModel.AllocatedAmountTo.HasValue)
                query = query.Where(x => x.Amount <= searchModel.AllocatedAmountTo.Value);
            //28
            if (searchModel.RemainAmountFrom.HasValue)
                query = query.Where(x => x.UnallocatedAmount >= searchModel.RemainAmountFrom.Value);
            //29
            if (searchModel.RemainAmountTo.HasValue)
                query = query.Where(x => x.UnallocatedAmount <= searchModel.RemainAmountTo.Value);
            //30
            if (searchModel.ExchangeRate.HasValue)
                query = query.Where(x => x.ExchangeRate == searchModel.ExchangeRate);
            //31
            if (searchModel.SystemIdFrom.HasValue)
                query = query.Where(x => x.SystemId >= searchModel.SystemIdFrom.Value);
            //32
            if (searchModel.SystemIdTo.HasValue)
                query = query.Where(x => x.SystemId <= searchModel.SystemIdTo.Value);
            //33
            if (searchModel.IsThirdPartySettlement.HasValue)
            {
                bool third = searchModel.IsThirdPartySettlement.Value == 1;
                query = query.Where(x => x.IsThirdPartySettlement == third);
            }
            //34
            if (searchModel.ThirdPartyId.HasValue)
            {
                query = query.Where(x => x.ThirdPartyId == searchModel.ThirdPartyId);
            }
            //35
            if (searchModel.ThirdPartyRole.HasValue)
            {
                var filterRole = (ThirdPartyRole)searchModel.ThirdPartyRole.Value;
                query = query.Where(x => x.ThirdPartyRole == filterRole);
            }
            //Cheque
            //36
            if (searchModel.ChequeTypeId.HasValue)
            {
                query = query.Where(x => x.Cheque.ChequeTypeId == searchModel.ChequeTypeId);
            }
            //37
            if (!string.IsNullOrWhiteSpace(searchModel.ChequeNo))
                query = query.Where(x => x.Cheque.ChequeNo.Contains(searchModel.ChequeNo));
            //38
            if (searchModel.ChequeBankId.HasValue)
            {
                query = query.Where(x => x.Cheque.BankId == searchModel.ChequeBankId);
            }
            //39
            if (!string.IsNullOrWhiteSpace(searchModel.BankBranchCode))
                query = query.Where(x => x.Cheque.BankBranchCode.Contains(searchModel.BankBranchCode));
            //40
            if (!string.IsNullOrWhiteSpace(searchModel.BankBranchName))
                query = query.Where(x => x.Cheque.BankBranchName.Contains(searchModel.BankBranchName));
            //41
            if (searchModel.DueDateFrom.HasValue)
                query = query.Where(x => x.Cheque.DueDate >= searchModel.DueDateFrom.Value);
            //42
            if (searchModel.DueDateTo.HasValue)
                query = query.Where(x => x.Cheque.DueDate <= searchModel.DueDateTo.Value);
            //43
            if (!string.IsNullOrWhiteSpace(searchModel.IssuerName))
                query = query.Where(x => x.Cheque.IssuerName.Contains(searchModel.IssuerName));
            //44
            if (!string.IsNullOrWhiteSpace(searchModel.RecieverName))
                query = query.Where(x => x.Cheque.ReceiverName.Contains(searchModel.RecieverName));
            //45
            if (!string.IsNullOrWhiteSpace(searchModel.BankAccountNo))
                query = query.Where(x => x.Cheque.BankAccountNo.Contains(searchModel.BankAccountNo));
            //46
            if (!string.IsNullOrWhiteSpace(searchModel.Iban))
                query = query.Where(x => x.Cheque.IBAN.Contains(searchModel.Iban));
            //47
            if (!string.IsNullOrWhiteSpace(searchModel.SayadNo))
                query = query.Where(x => x.Cheque.SayadNo.Contains(searchModel.SayadNo));
            //48
            if (!string.IsNullOrWhiteSpace(searchModel.Remarks))
                query = query.Where(x => x.Cheque.Remarks.Contains(searchModel.Remarks));
            //49
            if (searchModel.ChequeStatusId.HasValue)
            {
                query = query.Where(x => x.Cheque.StatusId == searchModel.ChequeStatusId);
            }
            //50
            if (searchModel.ChequePurposeId.HasValue)
            {
                query = query.Where(x => x.Cheque.ChequePurposeId == searchModel.ChequePurposeId);
            }


            var totalCount = await query.CountAsync();

            var page = searchModel.Page <= 0 ? 1 : searchModel.Page;
            var pageSize = searchModel.PageSize <= 0 ? 10 : searchModel.PageSize;

            var items = await query
                .OrderByDescending(x => x.SystemId)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentDocument_ReportListItemVm
                {
                    Id = x.Id,
                    DocumentNumber = x.DocumentNo,
                    DocumentDateGregorian = x.DocumentDate,
                    DocumentDate = x.DocumentDate != null ? x.DocumentDate : null,
                    EffectiveDate = x.EffectiveDate != null ? x.EffectiveDate : null,
                    DeleteDateAt = x.DeletedAt != null ? x.DeletedAt : null,
                    PersonName = x.Party != null ? x.Party.PersonName : null,
                    CompanyName = x.Company != null ? x.Company.Person.PersonName : null,
                    CurrencyTitle = x.Currency != null ? x.Currency.CurrencyCode : null,
                    FinanceDirectionTitle = x.FinanceDirection != null ? x.FinanceDirection.PaymentDocumentContextName : null,
                    PaymentMethodTitle = x.PaymentMethod != null ? x.PaymentMethod.PaymentMethodName : null,
                    Amount = x.Amount,
                    AllocatedAmount = x.AllocatedAmount,
                    UnAllocatedAmount = x.UnallocatedAmount,
                    TrackingNumber = x.TrackingNo,
                    HasCheque = x.Cheque != null && !x.Cheque.IsDeleted,
                    SystemId = x.SystemId,
                    CreatedAt = x.CreatedAt,
                    CreateUserName = x.CreateUser.FullName,
                    UpdatedAt = x.UpdatedAt,
                    UpdateUserName = x.EditUser.FullName,
                    HasVaultTransaction = _context.VaultMovements.Any(y => y.SourceEntityId == x.Id && y.VaultMovementSourceTypeId == 1),
                    ExchangeRate = x.ExchangeRate,
                    HasThirdParty = x.IsThirdPartySettlement,
                    ThirdPartyName = x.ThirdParty == null ? string.Empty : x.ThirdParty.PersonName,
                    ThirdPartyRoleName = x.ThirdPartyRole.ToString() ?? string.Empty,
                    RelatedPaymentSystemId = x.RelatedPaymentDocument == null ? null : x.RelatedPaymentDocument.SystemId,
                    //
                    BankName = x.Bank.BankName,
                    BankAccount = x.BankAccount.BankAccountTitle,
                    ReferenceNo = x.ReferenceNo,
                    TrackingNo = x.TrackingNo,
                    Description = x.Description,
                    IsAdvancedPaymentDesc = (x.IsAdvancePayment == false ? "No" : "Yes"),
                    IsAdvancedPayment = x.IsAdvancePayment,
                    BranchName = x.Branch.BranchName,
                    IsThirdPartySettlment = (x.IsThirdPartySettlement == false ? "No" : "Yes"),
                    //Cheque
                    ChequeTypeName = x.Cheque.ChequeType.ChequeTypeName,
                    ChequeNo = x.Cheque.ChequeNo,
                    ChequeBank = x.Cheque.Bank.BankName,
                    BankBranchCode = x.Cheque.BankBranchCode,
                    BankBranchName = x.Cheque.BankBranchName,
                    DueDate = x.Cheque.DueDate,
                    IssuerName = x.Cheque.IssuerName,
                    RecieverName = x.Cheque.ReceiverName,
                    BankAccountNo = x.Cheque.BankAccountNo,
                    IBan = x.Cheque.IBAN,
                    SayadNo = x.Cheque.SayadNo,
                    ChequeStatus = x.Cheque.Status.ChequeStatusName,
                    Remarks = x.Cheque.Remarks,
                    ChequePurpose = x.Cheque.ChequePurpose.ChequePurposeName
                })
                .ToListAsync();
            return new PagedResultDto<PaymentDocument_ReportListItemVm>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }


        //----------------Monitoring Deposit (Payment Document)-------------------------
        public async Task<MonitoringDepositResultDto> GetMonitoringDepositAsync()
        {
            var cashQuery = from pd in _context.PaymentDocuments
                            where pd.SettlementItemAllocations.Any(spa => spa.SettlementItem.Invoice.InvoiceSubjectId == 10)
                            select new
                            {
                                pd.FinanceDirectionId,
                                Detail = new DepositDetailsDto
                                {
                                    DocumentNo = pd.DocumentNo,
                                    DocumentDate = pd.DocumentDate,
                                    Amount = pd.Amount,
                                    Remain = pd.Amount - pd.AllocatedAmount,
                                    PartyName = pd.Party != null ? pd.Party.PersonName : null,
                                    BankName = null
                                }
                            };

            var chequeQuery = from pd in _context.PaymentDocuments
                              where pd.PaymentMethodId == 3
                                    && pd.Cheque != null
                                    && pd.Cheque.ChequePurposeId == 2
                                    && pd.Cheque.StatusId != 90
                              select new
                              {
                                  pd.FinanceDirectionId,
                                  Detail = new DepositDetailsDto
                                  {
                                      DocumentNo = pd.DocumentNo,
                                      DocumentDate = pd.DocumentDate,
                                      Amount = pd.Amount,
                                      Remain = pd.Amount - pd.AllocatedAmount,
                                      PartyName = pd.Party != null ? pd.Party.PersonName : null,
                                      BankName = pd.Cheque.Bank.BankName
                                  }
                              };

            var cashList = await cashQuery.ToListAsync();
            var chequeList = await chequeQuery.ToListAsync();

            var cashReceivedDetails = cashList.Where(x => x.FinanceDirectionId == 1).Select(x => x.Detail).ToList();
            var cashPaidDetails = cashList.Where(x => x.FinanceDirectionId == 2).Select(x => x.Detail).ToList();

            var chequeReceivedDetails = chequeList.Where(x => x.FinanceDirectionId == 1).Select(x => x.Detail).ToList();
            var chequePaidDetails = chequeList.Where(x => x.FinanceDirectionId == 2).Select(x => x.Detail).ToList();

            return new MonitoringDepositResultDto
            {
                CashReceived = new DepositTotalDto
                {
                    Count = cashReceivedDetails.Count,
                    TotalAmount = cashReceivedDetails.Sum(x => x.Amount),
                    TotalRemain = cashReceivedDetails.Sum(x => x.Remain),
                    Details = cashReceivedDetails
                },
                CashPaid = new DepositTotalDto
                {
                    Count = cashPaidDetails.Count,
                    TotalAmount = cashPaidDetails.Sum(x => x.Amount),
                    TotalRemain = cashPaidDetails.Sum(x => x.Remain),
                    Details = cashPaidDetails
                },
                ChequeReceived = new DepositTotalDto
                {
                    Count = chequeReceivedDetails.Count,
                    TotalAmount = chequeReceivedDetails.Sum(x => x.Amount),
                    TotalRemain = chequeReceivedDetails.Sum(x => x.Remain),
                    Details = chequeReceivedDetails
                },
                ChequePaid = new DepositTotalDto
                {
                    Count = chequePaidDetails.Count,
                    TotalAmount = chequePaidDetails.Sum(x => x.Amount),
                    TotalRemain = chequePaidDetails.Sum(x => x.Remain),
                    Details = chequePaidDetails
                }
            };
        }

        public async Task<IReadOnlyList<PaymentDocumentValidationError>>
            ValidateCreateAsync(
                PaymentDocumentCreateEditVm model,
                CancellationToken cancellationToken = default)
        {
            var errors = new List<PaymentDocumentValidationError>();

            void AddError(string propertyName, string message)
            {
                errors.Add(new PaymentDocumentValidationError
                {
                    PropertyName = propertyName,
                    Message = message
                });
            }

            if (model.FinanceDirectionId != 2)
                return errors;

            if (!model.PaymentRequestId.HasValue ||
                model.PaymentRequestId.Value == Guid.Empty)
            {
                AddError(
                    nameof(model.PaymentRequestId),
                    "A payment request is required to record a payment.");

                return errors;
            }

            var request = await _context.PaymentRequests
                .AsNoTracking()
                .Where(x => x.Id == model.PaymentRequestId.Value)
                .Select(x => new
                {
                    x.Id,
                    x.StatusId,
                    x.CompanyId,
                    x.BranchId,
                    x.FinanceDirectionId,
                    x.PartyId,
                    x.CurrencyId,
                    x.TotalAmount
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (request == null)
            {
                AddError(
                    nameof(model.PaymentRequestId),
                    "The selected payment request does not exist.");

                return errors;
            }

            if (request.StatusId != 30)
            {
                AddError(
                    nameof(model.PaymentRequestId),
                    "The selected payment request is not in a valid status for payment.");
            }

            // No document may already be linked to this request during creation.
            var alreadyUsed = await _context.PaymentDocuments
                .AnyAsync(
                    x => x.PaymentRequestId == request.Id,
                    cancellationToken);

            if (alreadyUsed)
            {
                AddError(
                    nameof(model.PaymentRequestId),
                    "A payment document has already been recorded for this request.");
            }

            if (request.FinanceDirectionId != model.FinanceDirectionId)
            {
                AddError(
                    nameof(model.FinanceDirectionId),
                    "The document's finance direction does not match the payment request.");
            }

            if (request.CompanyId != model.CompanyId)
            {
                AddError(
                    nameof(model.CompanyId),
                    "The document's company does not match the payment request.");
            }

            if (request.BranchId != model.BranchId)
            {
                AddError(
                    nameof(model.BranchId),
                    "The document's branch does not match the payment request.");
            }

            if (request.PartyId != model.PersonId)
            {
                AddError(
                    nameof(model.PersonId),
                    "The document's party does not match the payment request.");
            }

            if (request.CurrencyId != model.CurrencyId)
            {
                AddError(
                    nameof(model.CurrencyId),
                    "The document's currency does not match the payment request.");
            }
            if (request.TotalAmount != model.Amount)
            {
                AddError(
                    nameof(model.CurrencyId),
                    "The document's amount does not match the payment request.");
            }
            return errors;
        }
    }
}