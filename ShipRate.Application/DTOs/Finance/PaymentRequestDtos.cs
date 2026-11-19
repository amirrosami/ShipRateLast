namespace ShipRate.Application.DTOs.Finance
{
    public class PaymentRequestFilterDto
    {
        public long? RequestNo { get; set; }
        public int? StatusId { get; set; }
        public int? PaymentRequestCategoryId { get; set; }
        public Guid? PartyId { get; set; }
        public int? CurrencyId { get; set; }
        public Guid? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class PaymentRequestListDto
    {
        public Guid Id { get; set; }
        public long RequestNo { get; set; }
        public DateTime RequestDate { get; set; }

        public string? CategoryName { get; set; }
        public string? StatusName { get; set; }
        public int StatusId { get; set; }

        public string? PartyName { get; set; }
        public decimal TotalAmount { get; set; }
        public string? CurrencyName { get; set; }

        public string? PreferredPaymentMethodName { get; set; }

        public string? BranchName { get; set; }
        public string? Description { get; set; }

        public string? RequestedByUserName { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public string? ApprovedByUserName { get; set; }
        public string? ExecutedByUserName { get; set; }
        public string? RejectedByUserName { get; set; }

        public DateTime? ApprovalDate { get; set; }
        public DateTime? ExecutionDate { get; set; }
    }
    public class PaymentRequestItemEditDto
    {
        public Guid? Id { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }

        public Guid? InvoiceId { get; set; }
        public Guid? SourcePaymentDocumentId { get; set; }
        public Guid? DetentionId { get; set; }
        public Guid? JobId { get; set; }
    }
    public class PaymentRequestEditDto
    {
        public Guid? Id { get; set; }
        public Guid? ServiceRequestId { get; set; }

        public int CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public Guid? VaultId { get; set; }
        public long RequestNo { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Today;

        public int FinanceDirectionId { get; set; } = 2;
        public int PaymentRequestCategoryId { get; set; }

        public Guid? PartyId { get; set; }
        public decimal TotalAmount { get; set; }
        public int CurrencyId { get; set; }
        public int PreferredPaymentMethodId { get; set; }

        public string? TargetIban { get; set; }
        public string? TargetAccountName { get; set; }
        public string? TargetBankName { get; set; }

        public string? PartyName { get; set; }
        public string? ServiceRequestTitle { get; set; }




        public int StatusId { get; set; }

        public string? Description { get; set; }
        public Guid? RequestedByUserId { get; set; }

        public Guid? RejectedByUserId { get; set; }
        public DateTime? RejectDate { get; set; }
        public string? RejectionReason { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovalDate { get; set; }

        public Guid? FinalConfirmedByUserId { get; set; }
        public DateTime? FinanlConfirmDate { get; set; }

        public Guid? ExecutedByUserId { get; set; }
        public DateTime? ExecutionDate { get; set; }

        public List<PaymentRequestItemEditDto> Items { get; set; } = new();
        public List<Guid> SelectedPaymentDocumentIds { get; set; } = new();
        public List<Guid>? SelectedInvoiceIds { get; set; } = new();

    }
    public sealed class PaymentDocumentChoiceDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
    }
    public class VendorInvoiceChoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string Text { get; set; } = string.Empty;
    }
    //------Stimul Report--------
    public sealed class PaymentRequestDetailsViewModel
    {
        public PaymentRequestHeaderViewModel? Header { get; set; }

        public List<PaymentRequestItemViewModel> Items { get; set; } = [];
    }

    public sealed class PaymentRequestHeaderViewModel
    {
        public Guid Id { get; set; }
        public long? RequestNo { get; set; }
        public string? RequestDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Description { get; set; }
        public int? CompanyId { get; set; }
        //
        public string? CompanyName { get; set; }
        public string? BranchName { get; set; }
        public string? FinanceDirectionName { get; set; }
        public string? PaymentRequestCategoryName { get; set; }
        public string? PartyName { get; set; }
        //
        public string? ApprovalDate { get; set; }
        public string? ApprovedName { get; set; }
        //
        public string? FinalConfirmDate { get; set; }
        public string? FinalConfirmName { get; set; }
        //
        public string? ExecutionDate { get; set; }
        public string? ExecutedName { get; set; }
        public string? TargetAccountName { get; set; }
    }

    public sealed class PaymentRequestItemViewModel
    {
        public Guid Id { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }

        public string? DocumentNo { get; set; }
        public Guid? SourcePaymentDocumentId { get; set; }
        public decimal? MainAmount { get; set; }
        public decimal? AllocatedAmount { get; set; }

        public Guid? JobId { get; set; }
        public string? JobNumber { get; set; }
        public string? JobFileNumber { get; set; }
    }

}