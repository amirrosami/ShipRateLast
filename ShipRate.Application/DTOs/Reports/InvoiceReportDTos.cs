namespace ShipRate.Application.DTOs.Reports
{
    public class InvoicePrintDto
    {
        // هدر فاکتور

        public int InvoiceModelId { get; set; }
        public int FinanceDirectionId { get; set; }
        public int FNFId { get; set; }

        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceDatePersian { get; set; } = string.Empty;
        public string InvoiceModelName { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;

        // مشخصات فروشنده (اگر Direction=1 باشد، Company فروشنده است)
        public string SellerName { get; set; } = string.Empty;
        public string SellerEconomicCode { get; set; } = string.Empty;
        public string SellerNationalId { get; set; } = string.Empty;
        public string SellerAddress { get; set; } = string.Empty;
        public string SellerPhone { get; set; } = string.Empty;

        // مشخصات خریدار (اگر Direction=1 باشد، Party خریدار است)
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEconomicCode { get; set; } = string.Empty;
        public string BuyerNationalId { get; set; } = string.Empty;
        public string BuyerAddress { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;

        // مبالغ نهایی
        public decimal TotalFirst { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal FinalAmount { get; set; }

        public List<InvoiceItemPrintDto> Items { get; set; } = new();
    }

    public class InvoiceItemPrintDto
    {
        public int Row { get; set; }
        public string ChargeTitle { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AmountBeforeDiscount { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }




    //Stimul
    public class InvoiceReportDto
    {
        public Guid Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string FullSerial { get; set; } = string.Empty;
        public string? PartyName { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal FirstTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // لیست اقلام داخل خود شیء هدر
        public List<InvoiceItemReportDto> InvoiceItems { get; set; } = new();
    }

    public class InvoiceItemReportDto
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public int SequenceNumber { get; set; }
        public string? ChargeTypeName { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal QTY { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AmountBeforeDiscount { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }





}
