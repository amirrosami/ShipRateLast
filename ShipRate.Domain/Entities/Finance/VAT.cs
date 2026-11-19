using ShipRate.Domain.Entities.Base;

namespace ShipRate.Domain.Entities.Finance
{
    public class VAT : SystemEntity
    {
        public string VATName { get; set; }
        public string VATLocalName { get; set; }
        public decimal VATPercent { get; set; }
    }
}

