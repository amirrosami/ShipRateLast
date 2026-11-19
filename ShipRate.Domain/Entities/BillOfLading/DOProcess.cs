using ShipRate.Domain.Entities.Base;
using ShipRate.Domain.Entities.BaseInfo.BLSpec;
using ShipRate.Domain.Entities.BaseInfo.Finance;
using ShipRate.Domain.Entities.BaseInfo.Persons;
using ShipRate.Domain.Entities.DetentionCalculation;
using ShipRate.Domain.Entities.Finance;
using ShipRate.Domain.Entities.Security;

namespace ShipRate.Domain.Entities.BillOfLading
{
    public enum DOControlStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
    public class DOProcess : BusinessEntity
    {
        public Guid BLId { get; set; }

        public Guid? CustomerId { get; set; }
        public Guid? LawyerId { get; set; }
        public DateTime? CustomerRequestDate { get; set; }

        public DateTime? LineDODate { get; set; }
        public int? BLReleaseTypeId { get; set; }

        public Guid? CSControllerId { get; set; }
        public string? CSControllerComments { get; set; }
        public DateTime? CSControllerDate { get; set; }
        public DOControlStatus CSControllerStatus { get; set; }

        public Guid? FinanceControllerId { get; set; }
        public string? FinanceControllerComments { get; set; }
        public DateTime? FinanceControllerDate { get; set; }
        public DOControlStatus FinanceControllerStatus { get; set; }

        public Guid? FinalControllerId { get; set; }
        public string? FinalControllerComments { get; set; }
        public DateTime? FinalControllerDate { get; set; }
        public DOControlStatus FinalControllerStatus { get; set; }


        public Guid? SettlementExceptionApprovedById { get; set; }
        public DateTime? SettlementExceptionApprovedAt { get; set; }
        public string? SettlementExceptionReason { get; set; }


        public DateTime? DODeliveryDate { get; set; }
        public Guid? DODeliverUserId { get; set; }

        public string? Remarks { get; set; }
        //Relations
        public BL BL { get; set; }
        public Person Customer { get; set; }
        public BLReleaseType BLReleaseType { get; set; }
        public ApplicationUser CSController { get; set; }
        public ApplicationUser FinanceController { get; set; }
        public ApplicationUser FinalController { get; set; }
        public ApplicationUser SettlementExceptionApprovedBy { get; set; }

        public ICollection<DOProcessChecklistItem> ChecklistItems { get; set; } = new List<DOProcessChecklistItem>();
        public ICollection<DOProcessCustomerDebtSnapshot> CustomerDebtSnapshots { get; set; } = new List<DOProcessCustomerDebtSnapshot>();

    }
    public class DOChecklistDefinition : SystemEntity
    {
        public string CheckListName { get; set; } = null!;
        public string CheckListLocalName { get; set; } = null!;
        public bool IsMandatory { get; set; }
        public int SortOrder { get; set; }
    }
    public class DOProcessChecklistItem : BusinessEntity
    {
        public Guid DOProcessId { get; set; }
        public int ChecklistDefinitionId { get; set; }
        public bool IsChecked { get; set; }
        public Guid? CheckedByUserId { get; set; }
        public DateTime? CheckedAt { get; set; }
        public string? Comments { get; set; }

        //Relations
        public DOProcess DOProcess { get; set; } = null!;
        public DOChecklistDefinition ChecklistDefinition { get; set; } = null!;
        public ApplicationUser? CheckedByUser { get; set; }
    }
    public class DOProcessCustomerDebtSnapshot : BusinessEntity
    {
        public Guid DOProcessId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? InvoiceId { get; set; }
        public Guid? DetentionId { get; set; }
        public Guid? OnlineDetentionId { get; set; }
        public decimal Amount { get; set; }
        public int? CurrencyId { get; set; } // یا CurrencyId وابسته به سیستم شما

        public DateTime? IssueDate { get; set; }
        public string? Description { get; set; }

        public DateTime SnapshotAt { get; set; }
        public Guid? SnapshotByUserId { get; set; }

        // Relations
        public DOProcess DOProcess { get; set; } = null!;
        public Person Customer { get; set; } = null!;
        public Invoice Invoice { get; set; }
        public Detention Detention { get; set; }
        public OnlineDetention OnlineDetention { get; set; }
        public ApplicationUser? SnapshotByUser { get; set; }
        public Currency Currency { get; set; } = null!;
    }
}