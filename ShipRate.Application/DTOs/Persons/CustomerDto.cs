namespace ShipRate.Application.DTOs.Persons
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public PersonDto PersonDto { get; set; }
    }



    public class CustomerSelect2Dto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = "";   // نمایش در Select2 (کد/نام/کد اقتصادی)
    }

    public class CustomerProfileDto
    {
        // Customer
        public Guid CustomerId { get; set; }
        public Guid PersonId { get; set; }
        public string Remarks { get; set; } = "";

        // Person basics
        public string CustomerCode { get; set; } = ""; // اگر در BusinessEntity کد دارید، از آن پر کنید
        public string CommercialName { get; set; } = "";
        public string PersianName { get; set; } = "";
        public string EcoCode { get; set; } = "";
        public string RegNo { get; set; } = "";
        public string NationalId { get; set; } = "";

        // Contacts
        public string PrimaryContactName { get; set; } = "";
        public string PrimaryContactEmail { get; set; } = "";
        public string PrimaryContactPhone { get; set; } = "";

        public string EmailAddress { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string FaxNo { get; set; } = "";

        // Location
        public int CountryId { get; set; }
        public string CountryName { get; set; } = "";
        public Guid? CityId { get; set; }
        public string CityName { get; set; } = "";
        public string Address { get; set; } = "";
        public string PersianAddress { get; set; } = "";

        // Dates
        public DateTime? PersonRegDate { get; set; }

        // Sales/Industry
        public Guid? SalesUserId { get; set; }
        public string SalesUserFullName { get; set; } = "";
        public Guid? IndustryId { get; set; }
        public string IndustryName { get; set; } = "";

        // Derived
        public string PersonalityTypeTitle { get; set; } = "";
    }


    public class CustomerFinanceSummaryDto
    {
        public Guid CustomerPartyId { get; set; }

        // خلاصه دریافتی‌ها و پرداختی‌ها
        public decimal TotalReceivedAmount { get; set; }        // دریافتی‌ها از مشتری (Inflow)
        public decimal TotalPaidAmount { get; set; }            // پرداختی‌ها/عودت به مشتری (Outflow)
        public decimal TotalUnallocatedReceipts { get; set; }    // بستانکاری باز (دریافت‌های تخصیص‌نیافته به فاکتور)
        public decimal TotalAdvancePayments { get; set; }        // پیش‌دریافت‌ها/علی‌الحساب (IsAdvancePayment)

        public List<CustomerPaymentDocRowDto> RecentDocuments { get; set; } = new();
    }

    public class CustomerPaymentDocRowDto
    {
        public Guid Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public DateTime? EffectiveDate { get; set; }

        public int FinanceDirectionId { get; set; } // 1: Inflow/Receipt, 2: Outflow/Payment
        public string FinanceDirectionTitle { get; set; } = string.Empty;
        public string PaymentMethodTitle { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal UnallocatedAmount => Amount - AllocatedAmount;
        public bool IsFullyAllocated => UnallocatedAmount <= 0;

        public string CurrencyCode { get; set; } = string.Empty;
        public string? BankAccountTitle { get; set; }
        public string? ReferenceNo { get; set; }
        public string? TrackingNo { get; set; }
        public bool IsAdvancePayment { get; set; }
        public bool IsThirdPartySettlement { get; set; }
    }



    public class CustomerJobsSummaryDto
    {
        public Guid CustomerPartyId { get; set; }

        // آمارهای کلیدی پرونده‌ها
        public int TotalJobsCount { get; set; }
        public int ActiveJobsCount { get; set; }
        public int CompletedJobsCount { get; set; }
        public decimal TotalTeuOrContainers { get; set; }
        public decimal TotalGrossWeightKg { get; set; }

        // پرونده‌های اخیر
        public List<CustomerJobRowDto> RecentJobs { get; set; } = new();
    }

    public class CustomerJobRowDto
    {
        public Guid JobId { get; set; }
        public string JobFileNumber { get; set; } = string.Empty;
        public string JobNumber { get; set; } = string.Empty;
        public DateTime? JobDate { get; set; }

        public string JobStatusName { get; set; } = string.Empty;
        public int JobStatusId { get; set; }

        public string TransportTypeName { get; set; } = string.Empty;
        public string LoadTypeName { get; set; } = string.Empty;
        public string IncotermCode { get; set; } = string.Empty;

        // نقش مشتری در این پرونده (Customer یا Booker)
        public string CustomerRoleName { get; set; } = string.Empty;

        // مسیر حمل
        public string OriginName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public DateTime? ETD { get; set; }
        public DateTime? ETA { get; set; }

        // اوزان و کانتینر
        public decimal ContainerCount { get; set; }
        public decimal? GrossWeightKg { get; set; }
        public decimal? VolumeCBM { get; set; }

        public string SalesPersonName { get; set; } = string.Empty;
    }

    public enum DateRangePreset
    {
        OneMonth = 1,
        ThreeMonths = 3,
        SixMonths = 6,
        OneYear = 12,
        All = 0,
        Custom = 99
    }

    public class CustomerPerformanceFilterDto
    {
        public Guid CustomerPartyId { get; set; }
        public DateRangePreset Preset { get; set; } = DateRangePreset.SixMonths;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class CustomerPerformanceDto
    {
        public Guid CustomerPartyId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateRangePreset Preset { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // خلاصه شاخص‌های عملکردی کلیدی (KPIs)
        public int TotalJobsCount { get; set; }
        public int TotalTeuCount { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal TotalInvoicedAmount { get; set; }
        public decimal TotalReceivedAmount { get; set; }

        // نقاط روی نمودار (تقسیم‌بندی ماهانه یا روزانه)
        public List<MonthlyTrendPointDto> MonthlyTrends { get; set; } = new();

        // سهم انواع حمل (Sea, Air, Road, Rail) برای نمودار دونات/دایره‌ای
        public Dictionary<string, int> TransportTypeDistribution { get; set; } = new();
    }

    public class MonthlyTrendPointDto
    {
        public string PeriodLabel { get; set; } = string.Empty; // مثلا: "1403/05" یا "Aug 2024"
        public int JobsCount { get; set; }
        public int TeuCount { get; set; }
        public decimal FinancialVolume { get; set; }
    }


}
