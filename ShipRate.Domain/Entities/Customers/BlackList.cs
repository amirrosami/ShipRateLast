using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.Generals;
using ShipRate.Domain.Entities.Security;

namespace ShipRate.Domaiun.Entities.Customers
{
    public class BlackList : BusinessEntity
    {
        public int EntityTypeId { get; set; }
        public Guid EntityId { get; set; }
        public string? EntityReference { get; set; }
        public int BlackListReasonId { get; set; }
        public DateTime BlackListEnterDateTime { get; set; }
        public Guid BlackListEnterUserId { get; set; }
        public string BlackListEnterRemarks { get; set; } = string.Empty;
        public DateTime? BlackListExitDateTime { get; set; }
        public Guid? BlackListExitUserId { get; set; }
        public string BlackListExitRemarks { get; set; } = string.Empty;

        //Relations
        public EntityType? EntityType { get; set; }
        public BlackListReason? BlackListReason { get; set; }
        public ApplicationUser? BlackListEnterUser { get; set; }
        public ApplicationUser? BlackListExitUser { get; set; }

    }
}
