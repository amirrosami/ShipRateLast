using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShipRate.Application.Interfaces.Finance;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace ShipRate.Application.Services.Finance
{
    public class PaymentDocumentCashDetailsService : IPaymentDocumentCashDetailsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentDocumentCashDetailsService> _logger;

        public PaymentDocumentCashDetailsService(
            ApplicationDbContext context,
            ILogger<PaymentDocumentCashDetailsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CashDetailsModalViewModel> GetCashDetailsModalAsync(Guid paymentDocumentId, CancellationToken cancellationToken = default)
        {
            var doc = await _context.PaymentDocuments
                .AsNoTracking()
                .Where(x => x.Id == paymentDocumentId)
                .Select(x => new
                {
                    x.Id,
                    x.DocumentNo,
                    CurrencyCode = x.Currency != null ? x.Currency.CurrencyCode : "USD"
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (doc == null)
                throw new InvalidOperationException($"سند پرداخت با شناسه {paymentDocumentId} یافت نشد.");

            var items = await _context.PaymentDocumentItems
                .AsNoTracking()
                .Where(x => x.PaymentDocumentId == paymentDocumentId)
                .OrderBy(x => x.RowNo)
                .Select(x => new CashDetailItemRowViewModel
                {
                    Id = x.Id,
                    RowNo = x.RowNo,
                    ScanDateTime = x.ScanDateTime,
                    MachineSerialNumber = x.MachineSerialNumber,
                    SiteName = x.SiteName,
                    CurrencyCode = x.CurrencyCode,
                    Denomination = x.Denomination,
                    SerialNumber = x.SerialNumber,
                    FitResult = x.FitResult,
                    ErrorCode = x.ErrorCode,
                    // بدون خواندن ImageData سنگین، تنها وجود عکس را بررسی می‌کنیم
                    HasImage = _context.Set<PaymentDocumentItemImage>().Any(img => img.PaymentDocumentItemId == x.Id)
                })
                .ToListAsync(cancellationToken);

            return new CashDetailsModalViewModel
            {
                PaymentDocumentId = doc.Id,
                DocumentNo = doc.DocumentNo ?? string.Empty,
                CurrencyCode = doc.CurrencyCode,
                Items = items
            };
        }

        public async Task<SaveCashDetailsResultDto> SaveCashDetailsAsync(SaveCashDetailsRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.PaymentDocumentId == Guid.Empty)
                return SaveCashDetailsResultDto.Fail("درخواست نامعتبر است.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var doc = await _context.PaymentDocuments
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == request.PaymentDocumentId, cancellationToken);

                if (doc == null)
                    return SaveCashDetailsResultDto.Fail("سند دریافت/پرداخت یافت نشد.");

                // ۱. حذف ردیف‌های قدیمی (تصاویر متناظر به دلیل Cascade Delete به صورت خودکار حذف خواهند شد)
                if (doc.Items != null && doc.Items.Any())
                {
                    _context.PaymentDocumentItems.RemoveRange(doc.Items);
                }

                // ۲. افزودن ردیف‌های جدید
                decimal totalSum = 0;
                int rowNumber = 1;

                if (request.Items != null && request.Items.Any())
                {
                    foreach (var itemDto in request.Items)
                    {
                        var newItem = new PaymentDocumentItem
                        {
                            Id = Guid.NewGuid(),
                            PaymentDocumentId = doc.Id,
                            RowNo = rowNumber++,
                            ScanDateTime = itemDto.ScanDateTime ?? DateTime.Now,
                            MachineSerialNumber = itemDto.MachineSerialNumber,
                            SiteName = itemDto.SiteName,
                            CurrencyCode = string.IsNullOrWhiteSpace(itemDto.CurrencyCode) ? "USD" : itemDto.CurrencyCode.Trim(),
                            Denomination = itemDto.Denomination,
                            SerialNumber = itemDto.SerialNumber?.Trim() ?? string.Empty,
                            FitResult = itemDto.FitResult,
                            ErrorCode = itemDto.ErrorCode
                        };

                        // تبدیل و پیوست تصویر در صورتی که Base64 معتبر ارسال شده باشد
                        if (!string.IsNullOrWhiteSpace(itemDto.ImageBase64))
                        {
                            var (bytes, contentType) = ParseBase64Image(itemDto.ImageBase64);
                            if (bytes != null && bytes.Length > 0)
                            {
                                newItem.Image = new PaymentDocumentItemImage
                                {
                                    Id = Guid.NewGuid(),
                                    PaymentDocumentItemId = newItem.Id,
                                    ImageData = bytes,
                                    ContentType = contentType
                                };
                            }
                        }

                        totalSum += newItem.Denomination;
                        await _context.PaymentDocumentItems.AddAsync(newItem, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return SaveCashDetailsResultDto.Ok(totalSum, (request.Items?.Count ?? 0));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error while saving cash details for PaymentDocumentId: {PaymentDocumentId}", request.PaymentDocumentId);
                return SaveCashDetailsResultDto.Fail("خطای پایگاه داده در ذخیره‌سازی جزئیات پول‌شمار: " + ex.Message);
            }
        }

        public async Task<(byte[]? ImageData, string ContentType)> GetItemImageAsync(Guid paymentDocumentItemId, CancellationToken cancellationToken = default)
        {
            var image = await _context.Set<PaymentDocumentItemImage>()
                .AsNoTracking()
                .Where(x => x.PaymentDocumentItemId == paymentDocumentItemId)
                .Select(x => new { x.ImageData, x.ContentType })
                .FirstOrDefaultAsync(cancellationToken);

            if (image == null || image.ImageData == null)
                return (null, "image/png");

            return (image.ImageData, string.IsNullOrWhiteSpace(image.ContentType) ? "image/png" : image.ContentType);
        }

        #region Helper Methods

        private static (byte[]? Bytes, string ContentType) ParseBase64Image(string base64String)
        {
            try
            {
                string contentType = "image/png";

                // استخراج data:image/jpeg;base64,... در صورت وجود پیشوند Data URL
                var match = Regex.Match(base64String, @"^data:(image\/[a-zA-Z0-9\+\-]+);base64,(.+)$");
                string pureBase64 = base64String;

                if (match.Success)
                {
                    contentType = match.Groups[1].Value;
                    pureBase64 = match.Groups[2].Value;
                }

                byte[] bytes = Convert.FromBase64String(pureBase64);
                return (bytes, contentType);
            }
            catch
            {
                return (null, "image/png");
            }
        }

        public async Task<byte[]?> GetSerialImageBytesAsync(Guid paymentDocumentItemId)
        {
            if (paymentDocumentItemId == Guid.Empty)
                return null;

            // روش ۱: در صورتی که جدول تصاویر را مستقیماً به عنوان DbSet دارید (fin.PaymentDocumentItemImages)
            var imageBytes = await _context.PaymentDocumentItemImages
                .AsNoTracking()
                .Where(img => img.PaymentDocumentItemId == paymentDocumentItemId)
                .Select(img => img.ImageData)
                .FirstOrDefaultAsync();

            /* روش ۲: اگر DbSet مستقیم برای عکس ندارید و از طریق Navigation Property آیتم وصل است:
            var imageBytes = await _dbContext.PaymentDocumentItems
                .AsNoTracking()
                .Where(x => x.Id == paymentDocumentItemId && x.Image != null)
                .Select(x => x.Image.ImageData)
                .FirstOrDefaultAsync();
            */

            return imageBytes;
        }


        #endregion
    }
}
