namespace ShipRate.Application.ViewModels
{
    public class ContainerUseCycleRowDto
    {
        public Guid Id { get; set; }
        public int CycleId { get; set; }
        public string ContainerNumber { get; set; } = "";
        public string? SealNumber { get; set; }
        public string? EIRNumber { get; set; }
        public string? cntOwnTypeName { get; set; }
        public DateTime CycleStartDate { get; set; }
        public bool CycleClosed { get; set; }
        public DateTime? CycleCloseDate { get; set; }
        public string FullEmpty { get; set; } = "";   // from FullMT maybe
        public string SizeType { get; set; } = "";    // from ContainerSizeType maybe

        // تاریخ مراحل شش‌گانه چرخه (فقط تاریخ و ساعت)
        public DateTime? RTSDate { get; set; } // Release To Shipper
        public DateTime? RBSDate { get; set; } // Return By Shipper
        public DateTime? LOBDate { get; set; } // Load On Board
        public DateTime? DISDate { get; set; } // Discharge
        public DateTime? DTCDate { get; set; } // Deliver To Consignee
        public DateTime? RBCDate { get; set; } // Return By Consignee


    }
    public class ContainerUseCycleQueryDto
    {
        public string? Search { get; set; }
        public bool? Closed { get; set; } // null=all, true=closed, false=open

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;

        public string? SortColumn { get; set; } = "CycleStartDate";
        public string? SortDirection { get; set; } = "desc"; // asc/desc
    }
    public class ContainerMovementDto
    {
        public Guid Id { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string ActionTypeName { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public string? SubLocationName { get; set; }
        public string? VoyageInfo { get; set; }
        public string? FullMTName { get; set; }
        public string? SourceReference { get; set; }
        public string? BookingNumber { get; set; }
        public string? ContainerCondition { get; set; }
    }
    public class ContainerCycleBLDto
    {
        public Guid BLId { get; set; }
        public string BLNo { get; set; } = string.Empty;
        public string? MasterBLNo { get; set; }
        public DateTime? BLIssueDate { get; set; }
        public string? POR { get; set; }
        public string? POL { get; set; }
        public string? POD { get; set; }
        public string? PODel { get; set; }
        public string? CarrierName { get; set; }
        public string? SealNumber { get; set; }
        public decimal? GrossWeightKg { get; set; }
        public int TotalPackages { get; set; }
        public string? BLStatusName { get; set; }
    }
    //------Container Graphic cycle

    public class GraphicCycleHeaderDto
    {
        public string? ContainerNo { get; set; }
        public int? CycleCount { get; set; }
        public DateTime? StartCycleDate { get; set; }
        public string? LastAction { get; set; }
        public string? ContainerSize { get; set; }
        public string? CntOwnTypeName { get; set; }
        //
        public List<CardDetailsDto> CardDetails { get; set; }
        public List<CardContainerListDto> ContainerList { get; set; }
    }

    public class CardDetailsDto
    {
        public DateTime? ActionDate { get; set; }
        public string? LastFullMtInCycle { get; set; }
        public string? CntActionTypeName { get; set; }
        public string? LocationName { get; set; }
        public string? CompanyName { get; set; }
        //
        public string? CreateUserName { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? EditUserName { get; set; }
        public DateTime? EditDate { get; set; }
        public string? VoyageNo { get; set; }
        public string? VesselName { get; set; }
        public string? Refference { get; set; }
        public string? EventActionSource { get; set; }
    }
    public class CardContainerListDto
    {
        public string? BLNo { get; set; }
        public string? MasterBLNo { get; set; }
        public decimal? GrossWeight { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? VolumeCBM { get; set; }
        public int? PackageCount { get; set; }
        public string? ShipperName { get; set; }
        public string? ConsigneeName { get; set; }
        public string? NotifyName { get; set; }
        public string? PORName { get; set; }
        public string? POLName { get; set; }
        public string? PODName { get; set; }
        public string? PODelName { get; set; }
    }
}