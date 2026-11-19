using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ShipRate.Application.DTOs.Finance
{
    // ۱. مدل ورودی فیلتر جستجو
    public class NosaXmlInvoiceFilterDto
    {
        [Required(ErrorMessage = "انتخاب شرکت الزامی است.")]
        public int? CompanyId { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int? FinanceDirectionId { get; set; }
        public int? InvoiceFNFId { get; set; }
    }

    // ۲. مدل ارسال فرم نهایی جهت دانلود XML
    public class NosaXmlExportRequestDto : NosaXmlInvoiceFilterDto
    {
        public List<Guid> SelectedInvoiceIds { get; set; } = new();
    }

    // ۳. مدل نمایش هر ردیف در گرید
    public class NosaXmlInvoiceRowDto
    {
        public Guid Id { get; set; }
        public string? InvoiceNo { get; set; }
        public int Serial { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? PartyName { get; set; }
        public string? JobFileNo { get; set; }
        public string? BLNo { get; set; }
        public string? FinanceDirectionTitle { get; set; }
        public string? FNFTitle { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public bool IsPostedToAcc { get; set; }
    }

    // ۴. ویومدل صفحه اصلی
    public class NosaXmlPageViewModel
    {
        public NosaXmlInvoiceFilterDto Filter { get; set; } = new();
        public List<SelectListItem> Companies { get; set; } = new();
        public List<SelectListItem> FinanceDirections { get; set; } = new();
        public List<SelectListItem> InvoiceFnfs { get; set; } = new();
    }
}
