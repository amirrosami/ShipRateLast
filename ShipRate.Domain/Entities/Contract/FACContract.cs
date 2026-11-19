using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.ContainerSpec;
using ShipRate.Domain.Entities.BaseInfo.Finance;
using ShipRate.Domain.Entities.BaseInfo.Generals;
using ShipRate.Domain.Entities.BaseInfo.Locations;
using ShipRate.Domain.Entities.BaseInfo.Persons;
using ShipRate.Domain.Entities.Lines;
using ShipRate.Domain.Entities.Settings;

namespace ShipRate.Domain.Entities.Contract
{
    public enum FACCounterpartyRole
    {
        Carrier = 1,
        Forwarder = 2
    }
    public enum FACContractStatus
    {
        Draft = 10,
        Active = 20,
        Suspended = 30,
        Closed = 40
    }
    public enum FACCalculationBasis
    {
        PercentOfFreight = 1,
        PerTEU = 2,
        PerCBM = 3,
        PerContainer = 4,
        FlatPerShipment = 5
    }
    public enum FACDirection
    {
        Receivable = 1, // Our company receives FAC
        Payable = 2     // Our company pays FAC
    }
    public class FACContract : BusinessEntity
    {
        public int CompanyId { get; set; }
        public Guid? CarrierId { get; set; }
        public Guid? ForwarderId { get; set; } // FK to Customer
        public FACDirection Direction { get; set; }
        public FACCounterpartyRole CounterpartyRole { get; set; }

        public string ContractNo { get; set; } = string.Empty;
        public string FacContractTitle { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? SignedDate { get; set; }
        public FACContractStatus Status { get; set; } = FACContractStatus.Draft;
        public FACCalculationBasis? DefaultCalculationBasis { get; set; }
        public decimal? DefaultRate { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? MinimumFACAmount { get; set; }
        public decimal? MaximumFACAmount { get; set; }
        public int? POLFreeDays { get; set; }
        public int? PODFreeDays { get; set; }
        public string? FacContractRemarks { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigations
        public Company? Company { get; set; }
        public Carrier? Carrier { get; set; }
        public Person? Forwarder { get; set; }
        public Currency? Currency { get; set; }

        public ICollection<FACContractRateLine> RateLines { get; set; }
            = new List<FACContractRateLine>();
    }
    public class FACContractRateLine : BusinessEntity
    {
        public Guid FACContractId { get; set; }
        public Guid? POLId { get; set; }
        public Guid? PODId { get; set; }
        public int? TransportTypeId { get; set; }
        public int? LoadTypeId { get; set; }
        public int? ContainerSizeTypeId { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int Priority { get; set; } = 100;
        public FACCalculationBasis CalculationBasis { get; set; }
        public decimal Rate { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? MinimumFACAmount { get; set; }
        public decimal? MaximumFACAmount { get; set; }
        public int? POLFreeDays { get; set; }
        public int? PODFreeDays { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Remarks { get; set; }

        public FACContract FACContract { get; set; } = null!;
        public Currency? Currency { get; set; }

        public LoadType? LoadType { get; set; }
        public Location? POL { get; set; }
        public Location? POD { get; set; }
        public TransportType? TransportType { get; set; }
        public ContainerSizeType? ContainerSizeType { get; set; }
    }
}