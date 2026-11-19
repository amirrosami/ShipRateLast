using ShipRate.Domain.Entities.BillOfLading;

namespace ShipRate.Application.ViewModels
{

    public enum DOApprovalStage
    {
        CS = 1,
        Finance = 2,
        Final = 3
    }
    public sealed class DOChecklistInputVm
    {
        public int ChecklistDefinitionId { get; set; }
        public bool IsChecked { get; set; }
        public string? Comments { get; set; }
        public Guid? CheckedByUserId { get; set; }
        public string? CheckedByUserName { get; set; }
    }

    public sealed class DOProcessDraftInputVm
    {
        public Guid BLId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? LawyerId { get; set; }
        public DateTime? CustomerRequestDate { get; set; }
        public DateTime? LineDODate { get; set; }
        public string? Remarks { get; set; }

        public List<DOChecklistInputVm> Checklist { get; set; } = new();
    }

    public sealed class DOJobPartyOptionVm
    {
        public Guid PartyId { get; set; }
        public int RoleTypeId { get; set; }
        public string Text { get; set; } = "";
    }
    public sealed class DOReleaseTypeOptionVm
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
    }



    public sealed class DOChecklistDisplayVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsMandatory { get; set; }
    }

    public sealed class DOProcessModalVm
    {
        public Guid? DOProcessId { get; set; }
        public string BLNo { get; set; } = string.Empty;
        public int? BLReleaseTypeId { get; set; }
        public bool IsLocked { get; set; }

        public DateTime? SettlementExceptionApprovedAt { get; set; }
        public string? SettlementExceptionReason { get; set; }
        public Guid? SettlementExceptionApprovedById { get; set; }
        // مقدار این فیلد را سرویس، بر اساس وضعیت پروسه و مجوز کاربر تعیین کند.
        public bool CanApproveSettlementException { get; set; }



        public DOProcessDraftInputVm Draft { get; set; } = new();

        public List<DOJobPartyOptionVm> Parties { get; set; } = new();
        public List<DOReleaseTypeOptionVm> BLReleaseTypes { get; set; } = new();

        public List<DOChecklistDisplayVm> Definitions { get; set; } = new();

        public List<DOApprovalStageVm> ApprovalStages { get; set; } = new();

        public List<DOCustomerDebtRowVm> CustomerDebts { get; set; } = new();





    }

    public sealed class DOCustomerDebtRowVm
    {
        public string Source { get; set; } = string.Empty; // Invoice / Detention / OnlineDetention
        public string Description { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public decimal Amount { get; set; }
        public int CurrencyId { get; set; }
        public Guid? InvoiceId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;

        public Guid? DetentionId { get; set; }

        public Guid? OnlineDetentionId { get; set; }
    }



    public sealed class DOApprovalInputVm
    {
        public Guid DOProcessId { get; set; }
        public DOApprovalStage Stage { get; set; }
        public DOControlStatus Decision { get; set; }
        public string? Comments { get; set; }
    }


    public sealed class DOApprovalStageVm
    {
        public DOApprovalStage Stage { get; set; }
        public string Title { get; set; } = string.Empty;
        public DOControlStatus Status { get; set; }
        public Guid? ControllerId { get; set; }
        public DateTime? Date { get; set; }
        public string? Comments { get; set; }
        public bool CanApprove { get; set; }
    }
}
