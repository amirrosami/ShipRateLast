using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.Generals;
using ShipRate.Domain.Entities.Settings;

namespace ShipRate.Domain.Entities.BaseInfo.Finance
{
    public class ChargeTypeCategory : SystemEntity
    {
        public string ChargeTypeCategoryName { get; set; } = string.Empty;
        public string ChargeTypeCategoryLocalName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        //Relations
        public List<ChargeType>? ChargeTypes { get; set; }
    }
    public class ChargeType : SystemEntity
    {
        public string? ChargeTypeCode { get; set; } = string.Empty;
        public string? ChargeTypeCode2 { get; set; } = string.Empty;
        public string ChargeTypeName { get; set; } = string.Empty;
        public string ChargeTypeLocalName { get; set; } = string.Empty;
        public int ChargeTypeCategoryId { get; set; }


        // Financial Behavior
        public bool? IsTaxable { get; set; }
        public bool? AllowManualAmount { get; set; }
        public bool? AllowRateBased { get; set; }


        // Currency & Unit
        public int? CurrencyId { get; set; }
        public int? CalculationById { get; set; }
        public int? CalculationTypeId { get; set; }

        // Accounting / Reporting
        public bool? AffectProfit { get; set; }

        // وابستگی به Charge دیگر (پایه)
        public int? DependsOnChargeTypeId { get; set; }
        // در صورت DependencyType == Multiplier، این فیلد مقدار ضریب را نگه می‌دارد (مثلاً 0.15 برای ۱۵٪)
        public decimal? MultiplierRate { get; set; }

        public bool SystemDefined { get; set; }

        public DateTime? CreatedAt { get; set; }
        public Guid? CreateUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public Guid? EditUserId { get; set; }

        public string? Remarks { get; set; } = string.Empty;

        //Relations
        public ChargeTypeCategory? ChargeTypeCategory { get; set; }
        public CalculationBy CalculationBy { get; set; }
        public CalculationType CalculationType { get; set; }
        public ChargeType DependsOnChargeType { get; set; }
        public Currency? Currency { get; set; }
    }

    public class ChargeTypeNosaSetting : BusinessEntity
    {
        public int CompanyId { get; set; }
        public int ChargeTypeId { get; set; }

        public string SellDebitAccountCode { get; set; } = string.Empty;
        public string SellDebitDetailCode1 { get; set; } = string.Empty;
        public string SellDebitDetailCode2 { get; set; } = string.Empty;
        public string SellDebitDetailCode3 { get; set; } = string.Empty;
        public string SellDebitDetailCode4 { get; set; } = string.Empty;
        public string SellDebitDetailCode5 { get; set; } = string.Empty;
        public string SellDebitDetailCode6 { get; set; } = string.Empty;

        public string SellCreditAccountCode { get; set; } = string.Empty;
        public string SellCreditDetailCode1 { get; set; } = string.Empty;
        public string SellCreditDetailCode2 { get; set; } = string.Empty;
        public string SellCreditDetailCode3 { get; set; } = string.Empty;
        public string SellCreditDetailCode4 { get; set; } = string.Empty;
        public string SellCreditDetailCode5 { get; set; } = string.Empty;
        public string SellCreditDetailCode6 { get; set; } = string.Empty;


        public string BuyDebitAccountCode { get; set; } = string.Empty;
        public string BuyDebitDetailCode1 { get; set; } = string.Empty;
        public string BuyDebitDetailCode2 { get; set; } = string.Empty;
        public string BuyDebitDetailCode3 { get; set; } = string.Empty;
        public string BuyDebitDetailCode4 { get; set; } = string.Empty;
        public string BuyDebitDetailCode5 { get; set; } = string.Empty;
        public string BuyDebitDetailCode6 { get; set; } = string.Empty;

        public string BuyCreditAccountCode { get; set; } = string.Empty;
        public string BuyCreditDetailCode1 { get; set; } = string.Empty;
        public string BuyCreditDetailCode2 { get; set; } = string.Empty;
        public string BuyCreditDetailCode3 { get; set; } = string.Empty;
        public string BuyCreditDetailCode4 { get; set; } = string.Empty;
        public string BuyCreditDetailCode5 { get; set; } = string.Empty;
        public string BuyCreditDetailCode6 { get; set; } = string.Empty;

        //Relations
        public Company Company { get; set; }
        public ChargeType ChargeType { get; set; }
    }
}
