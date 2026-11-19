namespace ShipRate.Application.ViewModels
{
    public class InvoiceSendIndexVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<TaxInvoiceRowVm> Rows { get; set; } = new();
    }

    public class TaxInvoiceRowVm
    {
        public Guid Id { get; set; }
        public string? CompanyName { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }

        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string? Uid { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
    }

    public class TaxProviderSendResult
    {
        /// <summary>
        /// وضعیت موفقیت عملیات ثبت و ارسال
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// کد رهگیری یا شناسه فاکتور در سامانه معتمد (مثلاً id فاکتور در فن‌آوا)
        /// </summary>
        public string? TrackingUid { get; set; }

        /// <summary>
        /// متن وضعیت (مثلاً SENT_TO_MOADIAN یا FAILED یا SUCCESS)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// پیغام خطای بازگشتی از سرور در صورت عدم موفقیت
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
    public class AvatspOptions
    {
        public const string SectionName = "Avatsp";

        public string BaseUrl { get; set; } = "https://api.avatsp.net"; // یا آدرس دقیق مستندات فن‌آوا
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CartableId { get; set; } = string.Empty; // حافظه مالیاتی
    }

    public class AvatspInvoicePacketDto
    {
        public AvatspInvoiceHeaderDto Header { get; set; } = new();
        public List<AvatspInvoiceBodyDto> Body { get; set; } = new();
    }

    public class AvatspInvoiceHeaderDto
    {
        public string? Taxid { get; set; }          // شماره منحصر‌به‌فرد مالیاتی صورتحساب (۲۲ کاراکتر)
        public long Indatim { get; set; }           // تاریخ و زمان صدور (Epoch میلی‌ثانیه)
        public int Inty { get; set; } = 1;          // نوع صورتحساب: ۱ (نوع اول)، ۲ (نوع دوم)
        public int Inp { get; set; } = 1;           // الگوی صورتحساب: ۱ (فروش)، ۲ (فروش ارز)، و...
        public int Ins { get; set; } = 1;           // موضوع صورتحساب: ۱ (اصلی)، ۲ (اصلاحی)، ۳ (ابطالی)
        public string? Inno { get; set; }           // شماره صورتحساب داخلی شما
        public int Tob { get; set; } = 1;           // نوع شخص خریدار: ۱ (حقوقی)، ۲ (حقیقی)، ۳ (اتباع)، ۴ (مصرف‌کننده)
        public string? Tinb { get; set; }          // شناسه ملی / کد اقتصادی خریدار
        public string? Bpc { get; set; }           // کد پستی خریدار
        public decimal Tprdis { get; set; }         // مجموع مبلغ قبل از تخفیف
        public decimal Tdis { get; set; }           // مجموع تخفیف
        public decimal Tadis { get; set; }          // مجموع پس از تخفیف
        public decimal Tvam { get; set; }           // مجموع مالیات بر ارزش افزوده
        public decimal Tbill { get; set; }          // مجموع کل مبلغ صورتحساب
        public int Setm { get; set; } = 1;          // روش تسویه: ۱ (نقدی)، ۲ (نسیه)، ۳ (نقد و نسیه)
    }

    public class AvatspInvoiceBodyDto
    {
        public string? Sstid { get; set; }          // شناسه ۱۳ رقمی کالا/خدمت سازمان مالیاتی (Stuff/Service ID)
        public string? Sstt { get; set; }          // شرح کالا یا خدمت
        public decimal Am { get; set; } = 1;        // تعداد / مقدار
        public string? Mu { get; set; }            // واحد سنجش (کد واحد در سامانه)
        public decimal Fee { get; set; }           // نرخ / مبلغ واحد
        public decimal Prdis { get; set; }         // مبلغ قبل از تخفیف (Am * Fee)
        public decimal Dis { get; set; }           // مبلغ تخفیف
        public decimal Adis { get; set; }          // مبلغ پس از تخفیف (Prdis - Dis)
        public decimal Vra { get; set; }           // نرخ مالیات ارزش افزوده (مثلاً ۱۰ درصد)
        public decimal Vam { get; set; }           // مبلغ مالیات ارزش افزوده (Adis * Vra / 100)
        public decimal Tsstam { get; set; }        // مبلغ کل سطر (Adis + Vam)
    }




}
