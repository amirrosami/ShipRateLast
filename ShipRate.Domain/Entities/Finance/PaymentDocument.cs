using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.Finance;
using ShipRate.Domain.Entities.BaseInfo.Persons;
using ShipRate.Domain.Entities.Security;
using ShipRate.Domain.Entities.Settings;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShipRate.Domain.Entities.Finance
{
    public enum ThirdPartyRole
    {
        None = 0,
        Trustee = 1,        // معتمد محلی / صرافی امین (موجودی امانی)
        VendorOffset = 2    // تأمین‌کننده طرف تهاتر (تسویه بدهی فاکتور خرید)
    }

    public class PaymentDocument : BusinessEntity
    {
        public int CompanyId { get; set; }
        public Guid? BranchId { get; set; }

        public string DocumentNo { get; set; } = default!;
        public int PaymentMethodId { get; set; }
        public int FinanceDirectionId { get; set; }
        public Guid? PartyId { get; set; }

        public long SystemId { get; set; }

        public decimal Amount { get; set; }
        public decimal AllocatedAmount { get; set; }




        public int CurrencyId { get; set; }

        public DateTime DocumentDate { get; set; }
        public DateTime? EffectiveDate { get; set; }

        public Guid? BankId { get; set; }
        public Guid? BankAccountId { get; set; }

        public string? ReferenceNo { get; set; }
        public string? TrackingNo { get; set; }
        public string? Description { get; set; }

        // اگر این سند علی‌الحساب است
        public bool IsAdvancePayment { get; set; }

        public decimal? ExchangeRate { get; set; }



        public Guid? ThirdPartyId { get; set; }
        public bool IsThirdPartySettlement { get; set; } = false;
        public ThirdPartyRole ThirdPartyRole { get; set; } = ThirdPartyRole.None;
        public Guid? RelatedPaymentDocumentId { get; set; }

        public Guid? PaymentRequestId { get; set; }

        //Relations
        public Company Company { get; set; }
        public Branch Branch { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public FinanceDirection FinanceDirection { get; set; }
        public Currency Currency { get; set; }
        public Person? Party { get; set; }
        public Person? ThirdParty { get; set; }
        public Bank? Bank { get; set; }
        public BankAccount? BankAccount { get; set; }
        public Cheque? Cheque { get; set; }
        public PaymentRequest? PaymentRequest { get; set; }
        public ApplicationUser? CreateUser { get; set; }
        public ApplicationUser? EditUser { get; set; }
        public PaymentDocument? RelatedPaymentDocument { get; set; }



        public ICollection<SettlementItemPaymentAllocation> SettlementItemAllocations { get; set; }
            = new List<SettlementItemPaymentAllocation>();

        [NotMapped]
        public decimal UnallocatedAmount => Amount - AllocatedAmount;

        [NotMapped]
        public bool IsFullyAllocated => UnallocatedAmount <= 0;
        public virtual ICollection<PaymentDocumentItem> Items { get; set; } = new List<PaymentDocumentItem>();


    }
    public class PaymentDocumentItem : BusinessEntity
    {
        public Guid PaymentDocumentId { get; set; }
        public PaymentDocument PaymentDocument { get; set; } = default!;

        public int RowNo { get; set; }                      // شماره ردیف (1, 2, 3, ...)
        public DateTime ScanDateTime { get; set; }          // تاریخ و زمان اسکن دستگاه
        public string? MachineSerialNumber { get; set; }    // سریال دستگاه پول‌شمار
        public string? SiteName { get; set; }               // نام سایت / شعبه دستگاه
        public string CurrencyCode { get; set; } = "USD";   // کد ارز
        public decimal Denomination { get; set; }           // مبلغ اسمی (100, 50, 20)
        public string SerialNumber { get; set; } = default!;// سریال اسکناس (ایندکس شده)
        public string? FitResult { get; set; }              // وضعیت فیزیکی/کیفی
        public string? ErrorCode { get; set; }               // خطای شمارش/احراز اصالت

        // رابطه یک‌به‌یک با جدول نگهدارنده باینری عکس
        public PaymentDocumentItemImage? Image { get; set; }
    }
    public class PaymentDocumentItemImage : BusinessEntity
    {
        public Guid PaymentDocumentItemId { get; set; }
        public PaymentDocumentItem PaymentDocumentItem { get; set; } = default!;

        /// <summary>
        /// ذخیره تصویر در قالب باینری در دیتابیس (varbinary(max))
        /// </summary>
        public byte[] ImageData { get; set; } = default!;

        public string ContentType { get; set; } = "image/jpeg";
    }
}
