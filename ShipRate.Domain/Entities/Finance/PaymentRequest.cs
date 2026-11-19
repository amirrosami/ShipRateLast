using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.Finance;
using ShipRate.Domain.Entities.BaseInfo.Persons;
using ShipRate.Domain.Entities.DetentionCalculation;
using ShipRate.Domain.Entities.Operations;
using ShipRate.Domain.Entities.Security;
using ShipRate.Domain.Entities.ServiceRequests;
using ShipRate.Domain.Entities.Settings;
namespace ShipRate.Domain.Entities.Finance
{
    public class PaymentRequestStatus : SystemEntity
    {
        public int Id { get; set; }
        public string PaymentRequestStatusName { get; set; }
        public string PaymentRequestStatusLocalName { get; set; }
    }
    public class PaymentRequestCategory : SystemEntity
    {
        public int Id { get; set; }
        public string PaymentRequestCategoryName { get; set; }
        public string PaymentRequestCategoryLocalName { get; set; }
    }
    public class PaymentRequest : BusinessEntity
    {
        public int CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public long RequestNo { get; set; }
        public DateTime RequestDate { get; set; }
        public int FinanceDirectionId { get; set; } = 2; // همیشه Outbound
        public int PaymentRequestCategoryId { get; set; }

        public Guid? PartyId { get; set; }
        public decimal TotalAmount { get; set; }
        public int CurrencyId { get; set; }
        public int PreferredPaymentMethodId { get; set; }
        public string? TargetIban { get; set; }
        public string? TargetAccountName { get; set; }
        public string? TargetBankName { get; set; }

        public DateTime? RequestSentDate { get; set; }   //statusid=15

        public int StatusId { get; set; }


        public Guid? VaultId { get; set; }


        //عطف به درخواست مشتری
        public Guid? ServiceRequestId { get; set; }

        // لینک‌های اجرای مالی
        public Guid? CashPaymentRequestId { get; set; } // در صورت پرداخت نقدی از صندوق
        //public Guid? PaymentDocumentId { get; set; }    // در صورت صدور فیش بانکی یا چک
        public Guid? SettlementId { get; set; }         // در صورت نیاز به تهاتر/بستن فاکتور

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


        // Relations
        public Company Company { get; set; } = null!;
        public Branch? Branch { get; set; }
        public Person? Party { get; set; }
        public Currency Currency { get; set; } = null!;
        public CashPaymentRequest? CashPaymentRequest { get; set; }
        //public PaymentDocument? PaymentDocument { get; set; }
        public Settlement? Settlement { get; set; }
        public ServiceRequest? ServiceRequest { get; set; }

        public Vault? Vault { get; set; }

        public ApplicationUser? RequestedByUser { get; set; }
        public ApplicationUser? RejectedByUser { get; set; }
        public ApplicationUser? ApprovedByUser { get; set; }
        public ApplicationUser? ExecutedByUser { get; set; }
        public ApplicationUser? FinalConfirmedByUser { get; set; }

        public PaymentRequestCategory? PaymentRequestCategory { get; set; }
        public PaymentRequestStatus? PaymentRequestStatus { get; set; }
        public PaymentMethod? PreferredPaymentMethod { get; set; }
        public virtual ICollection<PaymentRequestItem> Items { get; set; } = new List<PaymentRequestItem>();
    }
    public class PaymentRequestItem : BusinessEntity
    {
        public Guid PaymentRequestId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }

        // مراجع ردیف
        public Guid? InvoiceId { get; set; }                  // فاکتور خرید خطوط (AP)
        public Guid? SourcePaymentDocumentId { get; set; }    // سند واریزی قبلی مشتری جهت عودت
        public Guid? DetentionId { get; set; }                // پرونده دیتنشن (عودت مازاد/دیتنشن منفی)
        public Guid? JobId { get; set; }                      // پرونده عملیاتی مربوطه

        // Relations
        public PaymentRequest PaymentRequest { get; set; } = null!;
        public Invoice? Invoice { get; set; }
        public PaymentDocument? SourcePaymentDocument { get; set; }
        public Detention? Detention { get; set; }
        public Job? Job { get; set; }
    }
}
