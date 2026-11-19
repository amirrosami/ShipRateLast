namespace ShipRate.Application.DTOs.EDI
{
    public class EDIMessageTypeRowDto
    {
        public int Id { get; set; }
        public string EDIMessageTypeCode { get; set; } = string.Empty;
        public string EDIMessageTypeName { get; set; } = string.Empty;
        public string EDIMessageTypeStandard { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class EDIMessageRowDto
    {
        public Guid Id { get; set; }
        public int MessageTypeId { get; set; }
        public string MessageTypeCode { get; set; } = string.Empty;
        public string InOutName { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime? ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EDIMessageSearchInput
    {
        public int MessageTypeId { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class EDIMessageListViewModel
    {
        public int MessageTypeId { get; set; }
        public string MessageTypeCode { get; set; } = string.Empty;
        public string MessageTypeName { get; set; } = string.Empty;

        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public PagedResultDto<EDIMessageRowDto> Result { get; set; } = new();
    }

    public class EDIPageViewModel
    {
        public List<EDIMessageTypeRowDto> MessageTypes { get; set; } = new();
    }

    public class EDIActionModalViewModel
    {
        public int MessageTypeId { get; set; }
        public string MessageTypeCode { get; set; } = string.Empty;
        public string MessageTypeName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
    }

    public class EplBlItemDto
    {
        public Guid BLId { get; set; }
        public string MasterBLNo { get; set; } = string.Empty;
        public string BLNo { get; set; } = string.Empty;
        public int ContainerCount { get; set; }
        public int TotalPackages { get; set; }
        public decimal? GrossWeightKg { get; set; }
        public decimal? VolumeCBM { get; set; }
        public string ShipperName { get; set; } = string.Empty;
        public string ConsigneeName { get; set; } = string.Empty;
        public string POLName { get; set; } = string.Empty;
        public string PODName { get; set; } = string.Empty;
        public string PODelName { get; set; } = string.Empty;
    }

    public class EplPageHeaderDto
    {
        public Guid? VoyagePortId { get; set; }
        public Guid? VoyageId { get; set; }
        public Guid? PortId { get; set; }
        public string VoyageNo { get; set; } = string.Empty;
        public string VesselName { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
    }

    public class EplPageViewModel
    {
        public EplPageHeaderDto Header { get; set; } = new();
        public List<EplBlItemDto> BLs { get; set; } = new();
        public List<Guid> SelectedBLIds { get; set; } = new();
    }


    public class CoparnPageViewModel
    {
        public EplPageHeaderDto Header { get; set; } = new();
        public List<EplBlItemDto> BLs { get; set; } = new();
        public List<Guid> SelectedBLIds { get; set; } = new();
    }



    public class GenerateEplRequestDto
    {
        public Guid VoyageId { get; set; }
        public Guid PortId { get; set; }
        public List<Guid> BlIds { get; set; } = new();
    }



}
