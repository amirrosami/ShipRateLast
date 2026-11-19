using ShipRate.Application.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace ShipRate.Application.DTOs.Finance
{
    public class JobInvoicesFilterVm
    {
        public Guid JobId { get; set; }
    }
    public class LookupItemVm
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
    public class PagedResultVm<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
    public class InvoiceListFilterVm
    {
        public string? InvoiceNo { get; set; }
        public string? InvoicePNo { get; set; }
        public string? PartyName { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int? InvoiceStatusId { get; set; }
        public int? FinanceDirectionId { get; set; }
        public int? CurrencyId { get; set; }

        public DateTime? InvoiceDateFrom { get; set; }
        public DateTime? InvoiceDateTo { get; set; }

        public Guid? JobId { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class InvoiceListItemVm
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public int InvoiceModelId { get; set; }
        public int DirectionId { get; set; }
        public string InvoiceModelName { get; set; }
        public string CompanyName { get; set; }


        public int InvoiceSubjectId { get; set; }
        public string InvoiceSubjectName { get; set; } = string.Empty;

        public string InvoiceSubjectTitle { get; set; } = string.Empty;

        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoicePNo { get; set; } = string.Empty;

        public string BLNo { get; set; } = string.Empty;
        public string BookingNo { get; set; } = string.Empty;
        public string JobFileNo { get; set; } = string.Empty;




        public string FNFName { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string InvoiceTypeName { get; set; } = string.Empty;
        public string InvoiceStatusName { get; set; } = string.Empty;
        public string FinanceDirectionName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;

        public decimal ExchangeRate { get; set; }


        public decimal TotalAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsFullySettled { get; set; }


        public DateTime CreateDateTime { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateDateTime { get; set; }
        public string? UpdateUserName { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainPayment { get; set; }
        public int PaymentStatusId { get; set; }

        public int Serial { get; set; }
        public string SerialPrefix { get; set; }
        public string SerialDisplay => Serial <= 0
            ? "-"
            : (string.IsNullOrWhiteSpace(SerialPrefix) ? Serial.ToString() : $"{SerialPrefix} {Serial}");
    }
    public class InvoiceListPageVm
    {
        public InvoiceListFilterVm Filter { get; set; } = new();
        public PagedResultVm<InvoiceListItemVm> Result { get; set; } = new();

        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
    }
    public class OnlinePaymentListPageVm
    {
        public OnlinePaymentFilterVm Filter { get; set; } = new();
        public PagedResultVm<OnlinePaymentListVM> Result { get; set; } = new();
    }
    public class InvoiceTotalsVm
    {
        public Guid InvoiceId { get; set; }
        public decimal FirstTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class InvoiceEditVm
    {
        public Guid? Id { get; set; }
        public Guid? Id2 { get; set; }
        public Guid? Id3 { get; set; }

        public Guid? JobId { get; set; }
        public Guid? BLId { get; set; }
        public Guid? BookingId { get; set; }


        public int? CalculateDOInvoiceMode { get; set; }

        public int Serial { get; set; }
        public string SerialPrefix { get; set; } = string.Empty;

        public int? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public int InvoiceModelId { get; set; }

        public bool IsReturn { get; set; }

        [Required]
        public int InvoiceSubjectId { get; set; }

        public string InvoiceSubjectTitle { get; set; } = string.Empty;

        public int? ClearanceTypeId { get; set; }

        [Required]
        public int FinanceDirectionId { get; set; }

        [Required]
        public int FNFId { get; set; }

        [Required]
        public int InvoiceTypeId { get; set; }

        [Required]
        public int InvoiceStatusId { get; set; }

        [StringLength(30)]
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? ParentInvoiceId { get; set; }
        public Guid? DetentionId { get; set; }

        public Guid? PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;

        [Required]
        public int CurrencyId { get; set; }

        public bool IsPreInvoice { get; set; }

        [StringLength(30)]
        public string? InvoicePNo { get; set; } = string.Empty;
        public DateTime? InvoicePDate { get; set; }


        public decimal? ExchangeRate { get; set; }

        [StringLength(200)]
        public string Remarks { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public decimal FirstTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal SettledAmount { get; set; }

        public decimal? DefaultTaxPercent { get; set; }

        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        public List<LookupItemVm> FNFS { get; set; } = new();
        public List<LookupItemVm> Companies { get; set; } = new();
        public List<LookupItemVm> InvoiceSubjects { get; set; } = new();
        public List<LookupItemVm> Jobs { get; set; } = new();
        public List<LookupItemVm> BLs { get; set; } = new();
        public List<LookupItemVm> Bookings { get; set; } = new();
        public List<LookupItemVm> InvoiceModels { get; set; } = new();
        public List<LookupItemVm> Branches { get; set; } = new();
        public List<LookupItemVm> ClearanceTypes { get; set; } = new();
    }
    public class InvoiceItemEditVm
    {
        public Guid? Id { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        public int SequenceNumber { get; set; }

        [Required]
        public int ChargeTypeId { get; set; }

        public Guid? ChargeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal QTY { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TaxPercent { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [StringLength(50)]
        public string? ChargeTypeSupplementRemarks { get; set; } = string.Empty;

        public decimal AmountBeforeDiscount { get; set; }
        public decimal AmountAfterDiscount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public List<LookupItemVm> ChargeTypes { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        public List<LookupItemVm> Charges { get; set; } = new();
    }
    public class InvoiceItemRowVm
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public int SequenceNumber { get; set; }
        public string ChargeTypeName { get; set; } = string.Empty;
        public string ChargeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal QTY { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxPercent { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal AmountBeforeDiscount { get; set; }
        public decimal AmountAfterDiscount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime CreateDateTime { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateDateTime { get; set; }
        public string? UpdateUserName { get; set; }

    }
    public class PartyLookupFilterVm
    {
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class PartyLookupRowVm
    {
        public Guid Id { get; set; }
        public string PersonName { get; set; } = string.Empty;
    }
    public class PersonReportSearchVm
    {
        public Guid? Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    //Invoice Report
    public class InvoiceListReportFilterVm
    {
        public string? InvoiceNo { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int? CompanyId { get; set; }
        public int? FinanceDirectionId { get; set; }
        public int? CurrencyId { get; set; }
        public int? InvoiceModelId { get; set; }
        public int? FNFId { get; set; }
        public int? InvoiceSubjectId { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? FirstTotalFrom { get; set; }
        public decimal? FirstTotalTo { get; set; }
        public DateTime? InvoiceDateFrom { get; set; }
        public DateTime? InvoiceDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public decimal? DiscountFrom { get; set; }
        public decimal? DiscountTo { get; set; }
        public decimal? SubTotalFrom { get; set; }
        public decimal? SubTotalTo { get; set; }
        public string? Remarks { get; set; }
        public decimal? TaxAmountFrom { get; set; }
        public decimal? TaxAmountTo { get; set; }
        public decimal? TotalAmountFrom { get; set; }
        public decimal? TotalAmountTo { get; set; }
        public int? InvoiceStatusId { get; set; }
        public decimal? RemainPaymentFrom { get; set; }
        public decimal? RemainPaymentTo { get; set; }
        public string? Notes { get; set; }
        public string? BookingNo { get; set; }
        public string? JobNumber { get; set; }
        //
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateAtFrom { get; set; }
        public DateTime? UpdateAtTo { get; set; }
        public string? UpdateUserName { get; set; }
        public string? BLNO { get; set; }
        public byte? IsPostedToAcc { get; set; }
        public DateTime? PostedToAccFrom { get; set; }
        public DateTime? PostedToAccTo { get; set; }
        public Guid? BranchId { get; set; }
        public string? SerialPrefix { get; set; }
        public int? SerialFrom { get; set; }
        public int? SerialTo { get; set; }
        public byte? IsReturn { get; set; }
        public Guid? PartyId { get; set; }
        public Guid? POLID { get; set; }
        public Guid? PORID { get; set; }
        public Guid? PODID { get; set; }
        public Guid? PODEID { get; set; }
        public string? FinalDestination { get; set; }
        public string? Shipper { get; set; }
        public string? Consignee { get; set; }
        public string? Notify { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public InvoiceReportSearchDropDownVm DropBind { get; set; }
    }

    public class InvoiceListItemReportVm
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public int InvoiceModelId { get; set; }
        public int DirectionId { get; set; }
        public string InvoiceModelName { get; set; }
        public string CompanyName { get; set; }


        public int InvoiceSubjectId { get; set; }
        public string InvoiceSubjectName { get; set; } = string.Empty;

        public string InvoiceSubjectTitle { get; set; } = string.Empty;

        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoicePNo { get; set; } = string.Empty;

        public string BLNo { get; set; } = string.Empty;
        public string BookingNo { get; set; } = string.Empty;
        public string JobFileNo { get; set; } = string.Empty;


        public Guid? BranchId { get; set; }
        public string BranchDesc { get; set; }

        public bool? IsReturn { get; set; }
        public string IsReturnDesc => IsReturn == true ? "Returns" : "No Returns";

        public string FNFName { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string InvoiceTypeName { get; set; } = string.Empty;
        public string InvoiceStatusName { get; set; } = string.Empty;
        public string FinanceDirectionName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        //
        public decimal FirstTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SettledAmount { get; set; }
        //public decimal RemainingAmount { get; set; }
        public bool IsFullySettled { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public bool IsPostedToAcc { get; set; }
        public string IsPostedToAccDesc => IsPostedToAcc == false ? "Not Posted" : "Posted";
        public DateTime? PostedToAcc { get; set; }
        //
        public DateTime CreateDateTime { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateDateTime { get; set; }
        public string? UpdateUserName { get; set; }
        public decimal? DefaultTaxPercent { get; set; }
        public DateTime? DeleteDateTime { get; set; }
        public string? DeleteUserName { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainPayment { get; set; }
        public int PaymentStatusId { get; set; }

        public int Serial { get; set; }
        public string SerialPrefix { get; set; }
        public string SerialDisplay => Serial <= 0
            ? "-"
            : (string.IsNullOrWhiteSpace(SerialPrefix) ? Serial.ToString() : $"{SerialPrefix} {Serial}");

        public decimal? ExchangeRate { get; set; }


        //
        public string? POL { get; set; }
        public Guid? POLID { get; set; }
        public string? POR { get; set; }
        public Guid? PORID { get; set; }
        public string? POD { get; set; }
        public Guid? PODID { get; set; }
        public string? PODE { get; set; }
        public Guid? PODEID { get; set; }
        //
        public string? Shipper { get; set; }
        public string? Consignee { get; set; }
        public string? Notify { get; set; }
        public string? FinalDestination { get; set; }
    }
    public class InvoiceListReportPageVm
    {
        public InvoiceListReportFilterVm Filter { get; set; } = new();
        public PagedResultVm<InvoiceListItemReportVm> Result { get; set; } = new();

        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        //
        public Guid? PartyId { get; set; }
        public string? PartyName { get; set; }
        //
        public Guid? POLId { get; set; }
        public string? POLName { get; set; }
        public Guid? PODId { get; set; }
        public string? PODName { get; set; }
        public Guid? PORId { get; set; }
        public string? PORName { get; set; }
        public Guid? PODEId { get; set; }
        public string? PODEName { get; set; }
    }

    public class InvoiceReportSearchDropDownVm
    {
        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        public List<LookupItemVm> FNFS { get; set; } = new();
        public List<LookupItemVm> Companies { get; set; } = new();
        public List<LookupItemVm> InvoiceSubjects { get; set; } = new();
        public List<LookupItemVm> Jobs { get; set; } = new();
        public List<LookupItemVm> BLs { get; set; } = new();
        public List<LookupItemVm> Bookings { get; set; } = new();
        public List<LookupItemVm> InvoiceModels { get; set; } = new();
        public List<LookupItemVm> Branches { get; set; } = new();
        public List<LookupItemVm> ClearanceTypes { get; set; } = new();
        public List<PersonReportSearchVm> Parties { get; set; } = new();

    }

    public class TotalInvoiceReportModelVm
    {
        public InvoiceListReportPageVm InvoiceInfo { get; set; }
        public InvoiceReportSearchDropDownVm DropDownBinding { get; set; }
    }
    //Invoice Report
    //Invoice Item Report
    public class InvoiceItemListReportFilterVm
    {
        public string? InvoiceNo { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int? CompanyId { get; set; }
        public int? FinanceDirectionId { get; set; }
        public int? CurrencyId { get; set; }
        public int? InvoiceModelId { get; set; }
        public int? FNFId { get; set; }
        public int? InvoiceSubjectId { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? FirstTotalFrom { get; set; }
        public decimal? FirstTotalTo { get; set; }
        public DateTime? InvoiceDateFrom { get; set; }
        public DateTime? InvoiceDateTo { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public decimal? DiscountFrom { get; set; }
        public decimal? DiscountTo { get; set; }
        public decimal? SubTotalFrom { get; set; }
        public decimal? SubTotalTo { get; set; }
        public string? Remarks { get; set; }
        public decimal? TaxAmountFrom { get; set; }
        public decimal? TaxAmountTo { get; set; }
        public decimal? TotalAmountFrom { get; set; }
        public decimal? TotalAmountTo { get; set; }
        public int? InvoiceStatusId { get; set; }
        public decimal? RemainPaymentFrom { get; set; }
        public decimal? RemainPaymentTo { get; set; }
        public string? Notes { get; set; }
        public string? BookingNo { get; set; }
        public string? JobNumber { get; set; }
        //
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateAtFrom { get; set; }
        public DateTime? UpdateAtTo { get; set; }
        public string? UpdateUserName { get; set; }
        public string? BLNO { get; set; }
        public byte? IsPostedToAcc { get; set; }
        public DateTime? PostedToAccFrom { get; set; }
        public DateTime? PostedToAccTo { get; set; }
        public Guid? BranchId { get; set; }
        public string? SerialPrefix { get; set; }
        public int? SerialFrom { get; set; }
        public int? SerialTo { get; set; }
        public byte? IsReturn { get; set; }
        public Guid? PartyId { get; set; }
        public Guid? POLID { get; set; }
        public Guid? PORID { get; set; }
        public Guid? PODID { get; set; }
        public Guid? PODEID { get; set; }
        public string? FinalDestination { get; set; }
        public string? Shipper { get; set; }
        public string? Consignee { get; set; }
        public string? Notify { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? ChargeTypeId { get; set; }
        public InvoiceReportSearchDropDownVm DropBind { get; set; }
    }

    public class InvoiceItemListItemReportVm
    {
        public Guid Id { get; set; }
        public Guid PartyId { get; set; }
        public int InvoiceModelId { get; set; }
        public int DirectionId { get; set; }
        public string InvoiceModelName { get; set; }
        public string CompanyName { get; set; }


        public int InvoiceSubjectId { get; set; }
        public string InvoiceSubjectName { get; set; } = string.Empty;

        public string InvoiceSubjectTitle { get; set; } = string.Empty;

        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoicePNo { get; set; } = string.Empty;

        public string BLNo { get; set; } = string.Empty;
        public string BookingNo { get; set; } = string.Empty;
        public string JobFileNo { get; set; } = string.Empty;


        public Guid? BranchId { get; set; }
        public string BranchDesc { get; set; }

        public bool? IsReturn { get; set; }
        public string IsReturnDesc => IsReturn == true ? "Returns" : "No Returns";

        public string FNFName { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string InvoiceTypeName { get; set; } = string.Empty;
        public string InvoiceStatusName { get; set; } = string.Empty;
        public string FinanceDirectionName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        //
        public decimal FirstTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SettledAmount { get; set; }
        //public decimal RemainingAmount { get; set; }
        public bool IsFullySettled { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public bool IsPostedToAcc { get; set; }
        public string IsPostedToAccDesc => IsPostedToAcc == false ? "Not Posted" : "Posted";
        public DateTime? PostedToAcc { get; set; }
        //
        public DateTime CreateDateTime { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime? UpdateDateTime { get; set; }
        public string? UpdateUserName { get; set; }
        public decimal? DefaultTaxPercent { get; set; }
        public DateTime? DeleteDateTime { get; set; }
        public string? DeleteUserName { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal RemainPayment { get; set; }
        public int PaymentStatusId { get; set; }

        public int Serial { get; set; }
        public string SerialPrefix { get; set; }
        public string SerialDisplay => Serial <= 0
            ? "-"
            : (string.IsNullOrWhiteSpace(SerialPrefix) ? Serial.ToString() : $"{SerialPrefix} {Serial}");

        public decimal? ExchangeRate { get; set; }


        //
        public string? POL { get; set; }
        public Guid? POLID { get; set; }
        public string? POR { get; set; }
        public Guid? PORID { get; set; }
        public string? POD { get; set; }
        public Guid? PODID { get; set; }
        public string? PODE { get; set; }
        public Guid? PODEID { get; set; }
        //
        public string? Shipper { get; set; }
        public string? Consignee { get; set; }
        public string? Notify { get; set; }
        public string? FinalDestination { get; set; }

        //Items
        public int? ChargeTypeId { get; set; }
        public string? ChargeTypeName { get; set; }
        public string? Descriptsion { get; set; }
        public decimal QTY { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AmountBeforeDiscount { get; set; }
        public decimal ItemDiscount { get; set; }
        public decimal AmountAfterDiscount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal ItemTaxAmount { get; set; }
        public decimal ItemTotalAmount { get; set; }
        public string? ChargeTypeSupplementRemarks { get; set; }

    }
    public class InvoiceItemListReportPageVm
    {
        public InvoiceItemListReportFilterVm Filter { get; set; } = new();
        public PagedResultVm<InvoiceItemListItemReportVm> Result { get; set; } = new();

        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        //
        public Guid? PartyId { get; set; }
        public string? PartyName { get; set; }
        //
        public Guid? POLId { get; set; }
        public string? POLName { get; set; }
        public Guid? PODId { get; set; }
        public string? PODName { get; set; }
        public Guid? PORId { get; set; }
        public string? PORName { get; set; }
        public Guid? PODEId { get; set; }
        public string? PODEName { get; set; }
    }

    public class InvoiceItemReportSearchDropDownVm
    {
        public List<LookupItemVm> InvoiceTypes { get; set; } = new();
        public List<LookupItemVm> InvoiceStatuses { get; set; } = new();
        public List<LookupItemVm> FinanceDirections { get; set; } = new();
        public List<LookupItemVm> Currencies { get; set; } = new();
        public List<LookupItemVm> FNFS { get; set; } = new();
        public List<LookupItemVm> Companies { get; set; } = new();
        public List<LookupItemVm> InvoiceSubjects { get; set; } = new();
        public List<LookupItemVm> Jobs { get; set; } = new();
        public List<LookupItemVm> BLs { get; set; } = new();
        public List<LookupItemVm> Bookings { get; set; } = new();
        public List<LookupItemVm> InvoiceModels { get; set; } = new();
        public List<LookupItemVm> Branches { get; set; } = new();
        public List<LookupItemVm> ClearanceTypes { get; set; } = new();
        public List<LookupItemVm> ChargeTypes { get; set; } = new();
        public List<PersonReportSearchVm> Parties { get; set; } = new();

    }

    public class TotalInvoiceItemReportModelVm
    {
        public InvoiceItemListReportPageVm InvoiceItemInfo { get; set; }
        public InvoiceItemReportSearchDropDownVm DropDownBinding { get; set; }
    }
    //Invoice Item Report




    public sealed class InvoiceSoaRowVm
    {
        public int RowNo { get; set; }
        public DateTime? Date { get; set; }
        public string Method { get; set; } = "-";
        public string BankReference { get; set; } = "-";
        public string SettlementNo { get; set; } = "-";
        public string PaymentDocumentNo { get; set; } = "-";
        public string PaymentMethod { get; set; } = "-";
        public string Reference { get; set; } = "-";
        public string Description { get; set; } = string.Empty;

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        // Debit minus credit. Positive is DR; negative is CR.
        public decimal SignedBalance { get; set; }

        public decimal DisplayBalance => Math.Abs(SignedBalance);

        public string BalanceSide =>
            SignedBalance > 0 ? "DR" :
            SignedBalance < 0 ? "CR" : "—";
    }

    public sealed class InvoiceSoaVm
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNo { get; set; } = "-";
        public DateTime? InvoiceDate { get; set; }
        public Guid? PartyId { get; set; }
        public int CurrencyId { get; set; }
        public int FinanceDirectionId { get; set; }

        public decimal Roe { get; set; }

        public decimal InvoiceTotal { get; set; }
        public decimal RecordedSettledAmount { get; set; }

        public decimal? InvoiceExchangeRate { get; set; }
        public decimal? ReportExchangeRate { get; set; }
        public string ExchangeRateSource { get; set; } = "Missing";

        public List<InvoiceSoaRowVm> Rows { get; set; } = new();

        public decimal TotalDebit => Rows.Sum(x => x.Debit);
        public decimal TotalCredit => Rows.Sum(x => x.Credit);

        public decimal SignedBalance => TotalDebit - TotalCredit;
        public decimal DisplayBalance => Math.Abs(SignedBalance);

        public string BalanceSide =>
            SignedBalance > 0 ? "DR" :
            SignedBalance < 0 ? "CR" : "—";

        // Excludes the first row, which represents the invoice itself.
        // Reversal rows reduce this amount.
        public decimal AllocatedToInvoice =>
            Rows.Skip(1).Sum(row =>
                FinanceDirectionId == 1
                    ? row.Credit - row.Debit
                    : row.Debit - row.Credit);

        public decimal ReconciliationDifference =>
            RecordedSettledAmount - AllocatedToInvoice;
    }














}