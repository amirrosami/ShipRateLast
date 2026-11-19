using FastReport;
using FastReport.Export.PdfSimple;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.Finance;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Infrastructure.Data;
using System.Data;

namespace ShipRate.Application.Services.Finance
{
    public class TreasuryDocumentService : ITreasuryDocumentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webEnv;

        public TreasuryDocumentService(ApplicationDbContext db, IWebHostEnvironment webEnv)
        {
            _db = db;
            _webEnv = webEnv;
        }

        public async Task<PagedResultDto<TreasuryDocumentListItemDto>> SearchAsync(TreasuryDocumentSearchDto input)
        {
            input.PageNumber = input.PageNumber <= 0 ? 1 : input.PageNumber;
            input.PageSize = input.PageSize is 10 or 20 or 50 ? input.PageSize : 10;

            var query = _db.TreasuryDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(input.DocumentNo))
            {
                var documentNo = input.DocumentNo.Trim();
                query = query.Where(x => x.DocumentNo != null && x.DocumentNo.Contains(documentNo));
            }

            if (input.FromDocumentDate.HasValue)
            {
                var fromDate = input.FromDocumentDate.Value.Date;
                query = query.Where(x => x.DocumentDate >= fromDate);
            }

            if (input.ToDocumentDate.HasValue)
            {
                var toDate = input.ToDocumentDate.Value.Date.AddDays(1);
                query = query.Where(x => x.DocumentDate < toDate);
            }

            if (!string.IsNullOrWhiteSpace(input.PartyName))
            {
                var partyName = input.PartyName.Trim();
                query = query.Where(x =>
                    x.Party != null &&
                    (x.Party.PersonName.Contains(partyName) ||
                     x.Party.PersonPersianName.Contains(partyName)));
            }

            if (input.TreasuryDocumentStatusId.HasValue)
            {
                query = query.Where(x => x.TreasuryDocumentStatusId == input.TreasuryDocumentStatusId.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.SystemId)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((input.PageNumber - 1) * input.PageSize)
                .Take(input.PageSize)
                .Select(x => new TreasuryDocumentListItemDto
                {
                    Id = x.Id,
                    SystemId = x.SystemId,
                    DocumentNo = x.DocumentNo,
                    DocumentDate = x.DocumentDate,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Person.PersonName,

                    PartyId = x.PartyId,
                    PartyName = x.Party == null ? "" : x.Party.PersonName,

                    TreasuryDocumentTypeId = x.TreasuryDocumentTypeId,
                    TreasuryDocumentTypeName = x.TreasuryDocumentType.TreasuryDocumentTypeLocalName,

                    TreasuryDocumentStatusId = x.TreasuryDocumentStatusId,
                    TreasuryDocumentStatusName = x.Status.TreasuryDocumentStatusLocalName,

                    TotalAmount = x.TotalAmount,

                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency.CurrencyCode
                })
                .ToListAsync();

            return new PagedResultDto<TreasuryDocumentListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = input.PageNumber,
                PageSize = input.PageSize
            };
        }

        public Task<TreasuryDocumentFormDto> GetCreateModelAsync()
        {
            return Task.FromResult(new TreasuryDocumentFormDto
            {
                DocumentDate = DateTime.Today,
                PaymentItems = new List<TreasuryDocumentPaymentSelectorDto>()
            });
        }

        public async Task<TreasuryDocumentFormDto?> GetEditModelAsync(Guid id)
        {
            var entity = await _db.TreasuryDocuments
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new TreasuryDocumentFormDto
                {
                    Id = x.Id,
                    CompanyId = x.CompanyId,
                    DocumentNo = x.DocumentNo,
                    TreasuryDocumentTypeId = x.TreasuryDocumentTypeId,
                    DocumentDate = x.DocumentDate,
                    PartyId = x.PartyId,
                    PartyName = x.Party == null ? "" : x.Party.PersonName,
                    CurrencyId = x.CurrencyId,
                    Description = x.Description,
                    TreasuryDocumentStatusId = x.TreasuryDocumentStatusId
                })
                .FirstOrDefaultAsync();

            if (entity == null)
                return null;

            if (entity.PartyId.HasValue)
            {
                entity.PaymentItems = await GetAvailablePaymentDocumentsByPartyAsync(entity.PartyId.Value, entity.Id);
            }

            return entity;
        }

        public async Task<List<PersonLookupDto>> SearchPersonsAsync(string term)
        {
            term = term?.Trim() ?? "";

            if (term.Length < 2)
                return new List<PersonLookupDto>();

            return await _db.Persons
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x =>
                    x.PersonName.Contains(term) ||
                    x.PersonPersianName.Contains(term) ||
                    x.FirstName.Contains(term) ||
                    x.LastName.Contains(term))
                .OrderBy(x => x.PersonName)
                .Take(10)
                .Select(x => new PersonLookupDto
                {
                    Id = x.Id,
                    Name = x.PersonName
                })
                .ToListAsync();
        }

        public async Task<List<TreasuryDocumentPaymentSelectorDto>> GetAvailablePaymentDocumentsByPartyAsync(
            Guid partyId,
            Guid? treasuryDocumentId = null)
        {
            var currentDocumentPaymentIds = new List<Guid>();

            if (treasuryDocumentId.HasValue)
            {
                currentDocumentPaymentIds = await _db.TreasuryDocumentItems
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.TreasuryDocumentId == treasuryDocumentId.Value)
                    .Select(x => x.PaymentDocumentId)
                    .ToListAsync();
            }

            var items = await _db.PaymentDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Where(x => x.PartyId == partyId)
                .Where(x =>
                    currentDocumentPaymentIds.Contains(x.Id) ||
                    !_db.TreasuryDocumentItems.Any(i =>
                        !i.IsDeleted &&
                        i.PaymentDocumentId == x.Id))
                .OrderByDescending(x => x.DocumentDate)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => new TreasuryDocumentPaymentSelectorDto
                {
                    PaymentDocumentId = x.Id,
                    DocumentNo = x.DocumentNo,
                    DocumentDate = x.DocumentDate,
                    Amount = x.Amount,
                    CurrencyId = x.CurrencyId,
                    CurrencyCode = x.Currency.CurrencyCode,
                    PaymentMethodName = x.PaymentMethod.PaymentMethodLocalName,
                    IsSelected = currentDocumentPaymentIds.Contains(x.Id),
                    IsCurrentDocumentItem = currentDocumentPaymentIds.Contains(x.Id)
                })
                .ToListAsync();

            return items;
        }

        public async Task<Guid> CreateAsync(TreasuryDocumentSaveDto input, Guid currentUserId)
        {
            ValidateSaveInput(input);

            var selectedPaymentIds = input.SelectedPaymentDocumentIds.Distinct().ToList();

            var paymentDocs = await _db.PaymentDocuments
                .Where(x =>
                    selectedPaymentIds.Contains(x.Id) &&
                    !x.IsDeleted)
                .ToListAsync();

            if (paymentDocs.Any(x => x.CurrencyId != input.CurrencyId))
                throw new InvalidOperationException("All selected payment documents must have the same currency as the treasury document.");

            if (paymentDocs.Count != selectedPaymentIds.Count)
                throw new InvalidOperationException("Some selected payment documents are invalid.");

            if (paymentDocs.Any(x => x.PartyId != input.PartyId))
                throw new InvalidOperationException("All selected payment documents must belong to the selected party.");

            var alreadyUsed = await _db.TreasuryDocumentItems
                .AnyAsync(x =>
                    !x.IsDeleted &&
                    selectedPaymentIds.Contains(x.PaymentDocumentId));

            if (alreadyUsed)
                throw new InvalidOperationException("One or more selected payment documents already have a treasury document.");

            var documentId = Guid.CreateVersion7();

            var document = new TreasuryDocument
            {
                Id = documentId,
                CompanyId = input.CompanyId,
                DocumentNo = input.DocumentNo,
                TreasuryDocumentTypeId = 1,
                DocumentDate = input.DocumentDate,
                PartyId = input.PartyId,
                TotalAmount = paymentDocs.Sum(x => x.Amount),
                CurrencyId = input.CurrencyId,
                Description = input.Description,
                TreasuryDocumentStatusId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                CreateUserId = currentUserId
            };

            foreach (var payment in paymentDocs)
            {
                document.TreasuryDocumentItems.Add(new TreasuryDocumentItem
                {
                    Id = Guid.CreateVersion7(),
                    TreasuryDocumentId = documentId,
                    PaymentDocumentId = payment.Id,
                    Amount = payment.Amount,
                    CurrencyId = payment.CurrencyId,
                    Description = payment.Description,
                    //IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreateUserId = currentUserId
                });
            }

            _db.TreasuryDocuments.Add(document);
            await _db.SaveChangesAsync();
            return documentId;
        }

        public async Task UpdateAsync(TreasuryDocumentSaveDto input, Guid currentUserId)
        {
            if (!input.Id.HasValue)
                throw new InvalidOperationException("Treasury document id is required.");

            ValidateSaveInput(input);

            var selectedPaymentIds = input.SelectedPaymentDocumentIds.Distinct().ToList();

            var document = await _db.TreasuryDocuments
                .Include(x => x.TreasuryDocumentItems)
                .FirstOrDefaultAsync(x => x.Id == input.Id.Value && !x.IsDeleted);

            if (document == null)
                throw new InvalidOperationException("Treasury document not found.");

            var paymentDocs = await _db.PaymentDocuments
                .Where(x =>
                    selectedPaymentIds.Contains(x.Id) &&
                    !x.IsDeleted)
                .ToListAsync();



            if (paymentDocs.Count != selectedPaymentIds.Count)
                throw new InvalidOperationException("Some selected payment documents are invalid.");
            if (paymentDocs.Any(x => x.CurrencyId != input.CurrencyId))
                throw new InvalidOperationException("All selected payment documents must have the same currency as the treasury document.");


            if (paymentDocs.Any(x => x.PartyId != input.PartyId))
                throw new InvalidOperationException("All selected payment documents must belong to the selected party.");

            var usedByOtherDocument = await _db.TreasuryDocumentItems
                .AnyAsync(x =>
                    !x.IsDeleted &&
                    x.TreasuryDocumentId != document.Id &&
                    selectedPaymentIds.Contains(x.PaymentDocumentId));

            if (usedByOtherDocument)
                throw new InvalidOperationException("One or more selected payment documents already belong to another treasury document.");

            document.CompanyId = input.CompanyId;
            document.DocumentNo = input.DocumentNo;
            //document.TreasuryDocumentTypeId = input.TreasuryDocumentTypeId;
            document.DocumentDate = input.DocumentDate;
            document.PartyId = input.PartyId;
            document.CurrencyId = input.CurrencyId;
            document.Description = input.Description;
            document.TotalAmount = paymentDocs.Sum(x => x.Amount);
            document.UpdatedAt = DateTime.Now;
            document.EditUserId = currentUserId;

            var activeItems = document.TreasuryDocumentItems
                .Where(x => !x.IsDeleted)
                .ToList();
            foreach (var item in activeItems)
            {
                item.IsDeleted = true;
                //item.IsActive = false;
                item.UpdatedAt = DateTime.Now;
                item.EditUserId = currentUserId;
            }
            foreach (var payment in paymentDocs)
            {

                var tdoc = new TreasuryDocumentItem
                {
                    Id = Guid.CreateVersion7(),
                    TreasuryDocumentId = document.Id,
                    PaymentDocumentId = payment.Id,
                    Amount = payment.Amount,
                    CurrencyId = payment.CurrencyId,
                    Description = payment.Description,
                    //IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now,
                    CreateUserId = currentUserId
                };

                document.TreasuryDocumentItems.Add(tdoc);
                _db.Entry(tdoc).State = EntityState.Added;
            }
            await _db.SaveChangesAsync();
        }
        public async Task<List<SelectListItem>> GetCompanySelectListAsync()
        {
            return await _db.Companies
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Person.PersonName
                })
                .ToListAsync();
        }
        public async Task<List<SelectListItem>> GetCurrencySelectListAsync()
        {
            return await _db.Currencies
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.CurrencyCode
                })
                .ToListAsync();
        }
        public async Task<List<SelectListItem>> GetTreasuryDocumentTypeSelectListAsync()
        {
            return await _db.TreasuryDocumentTypes
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TreasuryDocumentTypeLocalName
                })
                .ToListAsync();
        }

        public async Task<List<SelectListItem>> GetTreasuryDocumentStatusSelectListAsync()
        {
            return await _db.TreasuryDocumentStatuses
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Id)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TreasuryDocumentStatusLocalName
                })
                .ToListAsync();
        }

        private static void ValidateSaveInput(TreasuryDocumentSaveDto input)
        {


            if (input.CompanyId <= 0)
                throw new InvalidOperationException("Company is required.");

            //if (input.TreasuryDocumentTypeId <= 0)
            //    throw new InvalidOperationException("Document type is required.");

            //if (input.TreasuryDocumentStatusId <= 0)
            //    throw new InvalidOperationException("Status is required.");

            if (input.CurrencyId <= 0)
                throw new InvalidOperationException("Currency is required.");

            if (!input.PartyId.HasValue)
                throw new InvalidOperationException("Party is required.");

            if (input.SelectedPaymentDocumentIds == null || !input.SelectedPaymentDocumentIds.Any())
                throw new InvalidOperationException("At least one payment document must be selected.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(Guid id)
        {
            var entity = await _db.TreasuryDocuments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return (false, "Not Found!!!");

            if (entity.TreasuryDocumentStatusId != 1)
                return (false, "Only Draft Status is deletable.");

            entity.IsDeleted = true;

            await _db.SaveChangesAsync();

            return (true, "Receipt has been deleted successfully.");
        }

        public async Task ConfirmAsync(Guid id, Guid currentUserId)
        {
            var entity = await _db.TreasuryDocuments
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                throw new Exception("Treasury document not found.");

            if (entity.TreasuryDocumentStatusId != 1)
                throw new Exception("Only documents with status 1 can be confirmed.");

            entity.TreasuryDocumentStatusId = 2;
            entity.UpdatedAt = DateTime.Now;
            entity.EditUserId = currentUserId;

            await _db.SaveChangesAsync();
        }

        public async Task<byte[]> GenerateReceiptReportPdfAsync(Guid treasuryDocumentId)
        {
            var doc = await _db.TreasuryDocuments
                .AsNoTracking()
                .Include(x => x.Company)
                    .ThenInclude(x => x.Person)
                .Include(x => x.Party)
                .Include(x => x.Currency)
                .Include(x => x.TreasuryDocumentType)
                .Include(x => x.Status)
                .Include(x => x.TreasuryDocumentItems)
                    .ThenInclude(i => i.PaymentDocument)
                        .ThenInclude(p => p.PaymentMethod)
                .Include(x => x.TreasuryDocumentItems)
                    .ThenInclude(i => i.PaymentDocument)
                        .ThenInclude(p => p.Bank)
                .Include(x => x.TreasuryDocumentItems)
                    .ThenInclude(i => i.Currency)
                .FirstOrDefaultAsync(x => x.Id == treasuryDocumentId);

            if (doc == null)
                throw new KeyNotFoundException("سند خزانه یافت نشد.");

            // تبدیل داده‌ها به DataTable جهت استفاده مطمئن در قالب TableDataSource فست‌ریپورت
            var ds = new DataSet("TreasuryReceiptData");

            // ۱. جدول Header
            var dtHeader = new DataTable("Header");
            dtHeader.Columns.Add("SystemId", typeof(long));
            dtHeader.Columns.Add("DocumentNo", typeof(string));
            dtHeader.Columns.Add("DocumentDate", typeof(string));
            dtHeader.Columns.Add("CompanyName", typeof(string));
            dtHeader.Columns.Add("PartyName", typeof(string));
            dtHeader.Columns.Add("TreasuryDocumentTypeName", typeof(string));
            dtHeader.Columns.Add("StatusName", typeof(string));
            dtHeader.Columns.Add("CurrencyCode", typeof(string));
            dtHeader.Columns.Add("TotalAmount", typeof(decimal));
            dtHeader.Columns.Add("Description", typeof(string));

            dtHeader.Rows.Add(
                doc.SystemId,
                doc.DocumentNo ?? string.Empty,
                doc.DocumentDate.ToString("yyyy-MM-dd"),
                doc.Company?.Person.PersonName ?? string.Empty,
                doc.Party?.PersonName ?? string.Empty,
                doc.TreasuryDocumentType?.TreasuryDocumentTypeName ?? string.Empty,
                doc.Status?.TreasuryDocumentStatusName ?? string.Empty,
                doc.Currency?.CurrencyCode ?? string.Empty,
                doc.TotalAmount,
                doc.Description ?? string.Empty
            );
            // ۲. جدول Items
            var dtItems = new DataTable("Items");
            dtItems.Columns.Add("RowIndex", typeof(int));
            dtItems.Columns.Add("PaymentSystemId", typeof(long));
            dtItems.Columns.Add("PaymentDocumentNo", typeof(string));
            dtItems.Columns.Add("PaymentMethodName", typeof(string));
            dtItems.Columns.Add("DocumentDate", typeof(string));
            dtItems.Columns.Add("BankName", typeof(string));
            dtItems.Columns.Add("TrackingNo", typeof(string));
            dtItems.Columns.Add("Amount", typeof(decimal));
            dtItems.Columns.Add("CurrencyCode", typeof(string));
            dtItems.Columns.Add("Description", typeof(string));

            int rIndex = 1;
            foreach (var item in doc.TreasuryDocumentItems)
            {
                dtItems.Rows.Add(
                    rIndex++,
                    item.PaymentDocument?.SystemId ?? 0,
                    item.PaymentDocument?.DocumentNo ?? string.Empty,
                    item.PaymentDocument?.PaymentMethod?.PaymentMethodName ?? string.Empty,
                    item.PaymentDocument?.DocumentDate.ToString("yyyy-MM-dd") ?? string.Empty,
                    item.PaymentDocument?.Bank?.BankName ?? string.Empty,
                    item.PaymentDocument?.TrackingNo ?? string.Empty,
                    item.Amount,
                    item.Currency?.CurrencyCode ?? doc.Currency?.CurrencyCode ?? string.Empty,
                    item.Description ?? item.PaymentDocument?.Description ?? string.Empty
                );
            }

            ds.Tables.Add(dtHeader);
            ds.Tables.Add(dtItems);

            // اجرای گزارش در FastReport
            var reportPath = Path.Combine(_webEnv.ContentRootPath, "Reports", "Finance", "TreasuryReceipt.frx");
            if (!File.Exists(reportPath))
                throw new FileNotFoundException($"فایل قالب گزارش در مسیر یافت نشد: {reportPath}");

            using var report = new Report();
            report.Load(reportPath);
            report.RegisterData(ds, "TreasuryReceiptData");

            // فعال‌سازی Business/Table DataSources در درخت دیتای فست‌ریپورت
            report.GetDataSource("Header").Enabled = true;
            report.GetDataSource("Items").Enabled = true;

            report.Prepare();

            using var pdfStream = new MemoryStream();
            var pdfExport = new PDFSimpleExport(); // یا new PDFExport() بسته به پکیج شما
            report.Export(pdfExport, pdfStream);

            return pdfStream.ToArray();
        }
    }

}
