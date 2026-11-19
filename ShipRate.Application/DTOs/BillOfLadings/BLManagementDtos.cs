namespace ShipRate.Application.DTOs.BillOfLadings
{
    public enum TransportTypeEnum
    {
        Sea = 1,
        Road = 2,
        Rail = 3,
        Air = 4,
        MultiModal = 5
    }

    public enum BLManagementOperationalStatusEnum
    {
        Unknown = 0,
        Draft = 1,
        InProgress = 2,
        Ready = 3,
        Released = 4,
        Delivered = 5,
        Closed = 6,
        Cancelled = 7,
        Delayed = 8
    }

    public enum BLManagementHealthStatusEnum
    {
        Unknown = 0,
        Complete = 1,
        MissingData = 2,
        Attention = 3
    }

    public enum BLManagementSortFieldEnum
    {
        IssueDate = 1,
        BLNo = 2,
        JobNo = 3,
        CustomerName = 4,
        ShipperName = 5,
        ConsigneeName = 6,
        TransportType = 7,
        DocumentStatus = 8,
        OperationalStatus = 9,
        CreatedDate = 10,
        ModifiedDate = 11
    }

    public class BLManagementSearchRequest
    {
        public string? GlobalSearch { get; set; }
        public int? TransportTypeId { get; set; }
        public int? BLTypeId { get; set; }
        public int? BLStatusId { get; set; }
        public string? BLNo { get; set; }
        public string? JobNo { get; set; }
        public string? CustomerName { get; set; }
        public string? ShipperName { get; set; }
        public string? ConsigneeName { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public DateTime? IssueDateFrom { get; set; }
        public DateTime? IssueDateTo { get; set; }
        public string? ContainerNo { get; set; }
        public string? VesselVoyage { get; set; }
        public string? FlightNo { get; set; }
        public string? TruckNo { get; set; }
        public string? TrainNo { get; set; }
        public string? WagonNo { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class BLManagementRowDto
    {
        public Guid BLId { get; set; }
        public Guid JobId { get; set; }
        public string BLNo { get; set; } = string.Empty;
        public string JobNo { get; set; } = string.Empty;
        public string JobFileNumber { get; set; } = string.Empty;
        public string SalesUserName { get; set; } = string.Empty;
        public int? TradeTypeId { get; set; }
        public string? TradeTypeName { get; set; }
        public int TransportTypeId { get; set; }
        public string TransportTypeTitle { get; set; } = string.Empty;
        public string BLTypeTitle { get; set; } = string.Empty;
        public string DocumentStatusTitle { get; set; } = string.Empty;
        public string ShipperName { get; set; } = string.Empty;
        public string ConsigneeName { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public string RouteText { get; set; } = string.Empty;
        public decimal? GrossWeightKg { get; set; }
        public DateTime? IssueDate { get; set; }
        public string ModeDetailTitle { get; set; } = string.Empty;
        public string ModeDetailSubTitle { get; set; } = string.Empty;
        public bool IsDeleted => BLDeletedAt.HasValue;
        public DateTime? BLDeletedAt { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; }
        public bool CanPrint { get; set; }
        public bool CanRelease { get; set; }
        public bool CanCancel { get; set; }
        public bool CanDelete { get; set; }
    }
    public class BLManagementRowActionsDto
    {
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanPrint { get; set; }
        public bool CanRelease { get; set; }
        public bool CanCancel { get; set; }
        public bool CanDelete { get; set; }
        public bool CanRestore { get; set; }
    }
    public class BLManagementSummaryDto
    {
        public int TotalBills { get; set; }
        public int InProgressCount { get; set; }
        public int IncompleteCount { get; set; }
        public int MissingCarrierReferenceCount { get; set; }
    }
    public class BLManagementActionResultDto
    {
        public bool Success { get; set; }
        public long BLId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    public class BLManagementPageViewModel
    {
        public BLManagementSearchRequest Search { get; set; } = new();
        public BLManagementSummaryDto Summary { get; set; } = new();
        public PagedResultDto<BLManagementRowDto> PagedResult { get; set; } = new();
    }
    public class BLManagementQuickFilterDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsActive { get; set; }
    }
    public class BLManagementSearchResponse
    {
        public List<BLManagementRowDto> Items { get; set; } = new();
        public int TotalCount { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }

        public BLManagementSummaryDto Summary { get; set; } = new();
        public List<BLManagementQuickFilterDto> QuickFilters { get; set; } = new();
    }


    //-----------------BLs Report-----------------------------
    public class BLManagementSearchRequest_Report
    {
        //1
        public string? BLNo { get; set; }
        public string? MasterBLNo { get; set; }
        public int? TradeTypeId { get; set; }
        public int? TotalContainerFrom { get; set; }
        public int? TotalContainerTo { get; set; }
        //2
        public int? TransportTypeId { get; set; }
        public Guid? SheeperId { get; set; }
        public string? SheeperName { get; set; }
        public string? SheeperText { get; set; }
        public Guid? ConsigneeId { get; set; }
        public string? ConsigneeName { get; set; }
        public string? ConsigneeText { get; set; }
        //3
        public Guid? NotifyId { get; set; }
        public string? NotifyName { get; set; }
        public string? NotifyText { get; set; }
        public Guid? CarrierId { get; set; }
        public int? CompanyId { get; set; }
        public string? BookingNo { get; set; }
        //4
        public string? JobNo { get; set; }
        public string? JobFileNumber { get; set; }
        public string? SalesUserName { get; set; }
        public int? BLTypeId { get; set; }
        public int? BLLevelId { get; set; }
        //5
        public DateTime? BLIssueDateFrom { get; set; }
        public DateTime? BLIssueDateTo { get; set; }
        public string? PlaceOfIssue { get; set; }
        public Guid? IssuerPartyId { get; set; }
        public string? IssuerPartyName { get; set; }
        public byte? IssuerByUs { get; set; }
        //6
        public int? BLReleasedTypeId { get; set; }
        public DateTime? ReleaseDateFrom { get; set; }
        public DateTime? ReleaseDateTo { get; set; }
        public int? LoadTypeId { get; set; }
        public int? MovementTypeId { get; set; }
        //7
        public string? ExportReference { get; set; }
        public string? SerialNo { get; set; }
        public byte? IsSwitched { get; set; }
        public Guid? PORId { get; set; }
        public string? PORName { get; set; }
        public string? PORText { get; set; }
        //8
        public Guid? POLId { get; set; }
        public string? POLName { get; set; }
        public string? POLText { get; set; }
        public Guid? PODId { get; set; }
        public string? PODName { get; set; }
        public string? PODText { get; set; }
        public Guid? PODelId { get; set; }
        public string? PODelName { get; set; }
        //9
        public string? PODelText { get; set; }
        public string? FinalDestinationText { get; set; }
        public string? GoodsDescription { get; set; }
        public string? MarksAndNumbers { get; set; }
        public Guid? FreightPaymentAtId { get; set; }
        public string? FreightPaymentAtName { get; set; }
        //10
        public string? FreightPaymentAtText { get; set; }
        public string? RemarksInBL { get; set; }
        public string? RemarksInManifest { get; set; }
        public int? PackageTypeId { get; set; }
        public int? PackageTypeName { get; set; }
        public int? BLStatusId { get; set; }
        //11
        public Guid? SalesPersonId { get; set; }
        public int? PaymentTypeId { get; set; }
        public Guid? TranshipmentPortId { get; set; }
        public int? TranshipmentPortName { get; set; }
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }
        //12
        public string? CreateUserName { get; set; }
        public DateTime? UpdatedAtFrom { get; set; }
        public DateTime? UpdatedAtTo { get; set; }
        public string? EditUserName { get; set; }
        public int? IncotermId { get; set; }
        //13
        public int? MasterBLReleaseTypeId { get; set; }
        public int? MasterPaymentTypeId { get; set; }
        public string? MasterPlaceOfIssue { get; set; }
        public string? ContainerNo { get; set; }

        /////////////////////////////////////////
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class BLManagementPageViewModel_Report
    {
        public BLManagementSearchRequest_Report Search { get; set; } = new();
        public BLManagementSummaryDto_Report Summary { get; set; } = new();
        public PagedResultDto<BLManagementRowDto_Report> PagedResult { get; set; } = new();
    }
    public class BLManagementRowDto_Report
    {
        public string? BLNo { get; set; }
        public string? MasterBLNo { get; set; }
        public Guid BLId { get; set; }
        public string? CarrierName { get; set; }
        public string? TradeTypeName { get; set; }
        //BLContainers
        public int? F20 { get; set; }
        public int? F40 { get; set; }
        public string TransportTypeTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? BookingNo { get; set; }
        public string? JobNo { get; set; }
        public string? JobFileNumber { get; set; }
        public string? SalesUserName { get; set; }
        public string? BLTypeTitle { get; set; }
        public string? BLLevelTitle { get; set; }
        public DateTime? BLIssueDate { get; set; }
        public string? PlaceOfIssue { get; set; }
        public string? IssuerPartyName { get; set; }
        public bool? IssuerByUs { get; set; }
        public string? BLReleasedTypeName { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? LoadTypeName { get; set; }
        public string? MovementTypeName { get; set; }
        public string? ExportReference { get; set; }
        public string? SerialNo { get; set; }
        public bool? IsSwitched { get; set; }
        //
        public string? PORIdName { get; set; }
        public string? PORText{ get; set; }
        public string? POLIdName { get; set; }
        public string? POLText { get; set; }
        public string? PODIdName { get; set; }
        public string? PODText { get; set; }
        public string? PODelIdName { get; set; }
        public string? PODelText { get; set; }
        public string? FinalDestinationText { get; set; }
        public string? GoodsDescription { get; set; }
        public string? MarksAndNumbers { get; set; }
        public string? FreightPaymentAtIdName { get; set; }
        public string? FreightPaymentAtText { get; set; }
        public string? RemarksInBL { get; set; }
        public string? RemarksInManifest { get; set; }
        public decimal? GrossWeightKg { get; set; }
        public decimal? NetWeightKg { get; set; }
        public decimal? VolumeCBM { get; set; }
        public decimal? ChargeableWeight { get; set; }
        public int? TotalContainers { get; set; }
        public int? TotalPackages { get; set; }
        public string? PackageTypeName { get; set; }
        public string? PackageTypeText { get; set; }
        public string? BLStatusName { get; set; }
        public string? SalesPersonName { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? TranshipmentPortName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreateUserName { get; set; }
        public string? EditUserName { get; set; }
        public string? IncotermName { get; set; }
        public DateTime? MasterBLIssueDate { get; set; }
        public string? MasterBLReleaseTypeName { get; set; }
        public string? MasterPaymentTypeName { get; set; }
        public string? MasterPlaceOfIssue { get; set; }
        public DateTime? MasterReleaseDate { get; set; }
        public string? PODDetentionTariffName { get; set; }
        public string? POLDetentionTariffName { get; set; }
        public string? MasterPODDetentionTariffName { get; set; }
        public string? MasterPOLDetentionTariffName { get; set; }
        
        //BLParty
        public string? ShipperName { get; set; }
        public string? ShipperText { get; set; }
        public string? ConsigneeName { get; set; }
        public string? ConsigneeText { get; set; }
        public string? NotifyName { get; set; }
        public string? NotifyText { get; set; }
       
    }
    public class BLManagementSummaryDto_Report
    {
        public int TotalBills { get; set; }
        public int InProgressCount { get; set; }
        public int IncompleteCount { get; set; }
        public int MissingCarrierReferenceCount { get; set; }
    }
    //--------------------------------------------------------
    //-----------------Containers BL Report-------------------
    public class BLManagementSearchRequest_ContainerReport
    {
        //1
        public string? BLNo { get; set; }
        public string? MasterBLNo { get; set; }
        public int? TradeTypeId { get; set; }
        //public int? TotalContainerFrom { get; set; }
        //public int? TotalContainerTo { get; set; }
        //2
        public int? TransportTypeId { get; set; }
        public Guid? SheeperId { get; set; }
        public string? SheeperName { get; set; }
        public string? SheeperText { get; set; }
        public Guid? ConsigneeId { get; set; }
        public string? ConsigneeName { get; set; }
        public string? ConsigneeText { get; set; }
        //3
        public Guid? NotifyId { get; set; }
        public string? NotifyName { get; set; }
        public string? NotifyText { get; set; }
        public Guid? CarrierId { get; set; }
        public int? CompanyId { get; set; }
        public string? BookingNo { get; set; }
        //4
        public string? JobNo { get; set; }
        public string? JobFileNumber { get; set; }
        public string? SalesUserName { get; set; }
        public int? BLTypeId { get; set; }
        public int? BLLevelId { get; set; }
        //5
        public DateTime? BLIssueDateFrom { get; set; }
        public DateTime? BLIssueDateTo { get; set; }
        public string? PlaceOfIssue { get; set; }
        public Guid? IssuerPartyId { get; set; }
        public string? IssuerPartyName { get; set; }
        public byte? IssuerByUs { get; set; }
        //6
        public int? BLReleasedTypeId { get; set; }
        public DateTime? ReleaseDateFrom { get; set; }
        public DateTime? ReleaseDateTo { get; set; }
        public int? LoadTypeId { get; set; }
        public int? MovementTypeId { get; set; }
        //7
        public string? ExportReference { get; set; }
        public string? SerialNo { get; set; }
        public byte? IsSwitched { get; set; }
        public Guid? PORId { get; set; }
        public string? PORName { get; set; }
        public string? PORText { get; set; }
        //8
        public Guid? POLId { get; set; }
        public string? POLName { get; set; }
        public string? POLText { get; set; }
        public Guid? PODId { get; set; }
        public string? PODName { get; set; }
        public string? PODText { get; set; }
        public Guid? PODelId { get; set; }
        public string? PODelName { get; set; }
        //9
        public string? PODelText { get; set; }
        public string? FinalDestinationText { get; set; }
        public string? GoodsDescription { get; set; }
        public string? MarksAndNumbers { get; set; }
        public Guid? FreightPaymentAtId { get; set; }
        public string? FreightPaymentAtName { get; set; }
        //10
        public string? FreightPaymentAtText { get; set; }
        public string? RemarksInBL { get; set; }
        public string? RemarksInManifest { get; set; }
        public int? PackageTypeId { get; set; }
        public int? PackageTypeName { get; set; }
        public int? BLStatusId { get; set; }
        //11
        public Guid? SalesPersonId { get; set; }
        public int? PaymentTypeId { get; set; }
        public Guid? TranshipmentPortId { get; set; }
        public int? TranshipmentPortName { get; set; }
        public DateTime? CreatedAtFrom { get; set; }
        public DateTime? CreatedAtTo { get; set; }
        //12
        public string? CreateUserName { get; set; }
        public DateTime? UpdatedAtFrom { get; set; }
        public DateTime? UpdatedAtTo { get; set; }
        public string? EditUserName { get; set; }
        public int? IncotermId { get; set; }
        //13
        public int? MasterBLReleaseTypeId { get; set; }
        public int? MasterPaymentTypeId { get; set; }
        public string? MasterPlaceOfIssue { get; set; }


        //Containrs BL
        public string? ContainerNo { get; set; }
        public int? ContainerSizeTypeId { get; set; }
        public string? SealNumber { get; set; }
        public int? CntOwnTypeId { get; set; }
        public int? FullMTId { get; set; }
        public int? ContainerLoadTypeId { get; set; }

        /////////////////////////////////////////
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class BLManagementPageViewModel_ContainerReport
    {
        public BLManagementSearchRequest_ContainerReport Search { get; set; } = new();
        //public BLManagementSummaryDto_ContainerReport Summary { get; set; } = new();
        public PagedResultDto<BLManagementRowDto_ContainerReport> PagedResult { get; set; } = new();
    }
    public class BLManagementRowDto_ContainerReport
    {
        public string? BLNo { get; set; }
        public string? MasterBLNo { get; set; }
        public Guid BLId { get; set; }
        public string? CarrierName { get; set; }
        public string? TradeTypeName { get; set; }
        //BLContainers
        //public int? F20 { get; set; }
        //public int? F40 { get; set; }
        public string TransportTypeTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? BookingNo { get; set; }
        public string? JobNo { get; set; }
        public string? JobFileNumber { get; set; }
        public string? SalesUserName { get; set; }
        public string? BLTypeTitle { get; set; }
        public string? BLLevelTitle { get; set; }
        public DateTime? BLIssueDate { get; set; }
        public string? PlaceOfIssue { get; set; }
        public string? IssuerPartyName { get; set; }
        public bool? IssuerByUs { get; set; }
        public string? BLReleasedTypeName { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? LoadTypeName { get; set; }
        public string? MovementTypeName { get; set; }
        public string? ExportReference { get; set; }
        public string? SerialNo { get; set; }
        public bool? IsSwitched { get; set; }
        //
        public string? PORIdName { get; set; }
        public string? PORText { get; set; }
        public string? POLIdName { get; set; }
        public string? POLText { get; set; }
        public string? PODIdName { get; set; }
        public string? PODText { get; set; }
        public string? PODelIdName { get; set; }
        public string? PODelText { get; set; }
        public string? FinalDestinationText { get; set; }
        public string? GoodsDescription { get; set; }
        public string? MarksAndNumbers { get; set; }
        public string? FreightPaymentAtIdName { get; set; }
        public string? FreightPaymentAtText { get; set; }
        public string? RemarksInBL { get; set; }
        public string? RemarksInManifest { get; set; }
        public decimal? GrossWeightKg { get; set; }
        public decimal? NetWeightKg { get; set; }
        public decimal? VolumeCBM { get; set; }
        public decimal? ChargeableWeight { get; set; }
        //public int? TotalContainers { get; set; }
        public int? TotalPackages { get; set; }
        public string? PackageTypeName { get; set; }
        public string? PackageTypeText { get; set; }
        public string? BLStatusName { get; set; }
        public string? SalesPersonName { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? TranshipmentPortName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreateUserName { get; set; }
        public string? EditUserName { get; set; }
        public string? IncotermName { get; set; }
        public DateTime? MasterBLIssueDate { get; set; }
        public string? MasterBLReleaseTypeName { get; set; }
        public string? MasterPaymentTypeName { get; set; }
        public string? MasterPlaceOfIssue { get; set; }
        public DateTime? MasterReleaseDate { get; set; }
        public string? PODDetentionTariffName { get; set; }
        public string? POLDetentionTariffName { get; set; }
        public string? MasterPODDetentionTariffName { get; set; }
        public string? MasterPOLDetentionTariffName { get; set; }

        //BLParty
        public string? ShipperName { get; set; }
        public string? ShipperText { get; set; }
        public string? ConsigneeName { get; set; }
        public string? ConsigneeText { get; set; }
        public string? NotifyName { get; set; }
        public string? NotifyText { get; set; }
        //BL Containers
        //1
        public string? ContainerNumber { get; set; }
        public string? ContainerSizeTypeName { get; set; }
        public string? SealNumber { get; set; }
        public int? TotalPackageCount { get; set; }
        public decimal? TotalVolumeCBM { get; set; }
        public decimal? TotalGrossWeight { get; set; }
        public decimal? TotalNetWeight { get; set; }
        public decimal? BLContainerPercent { get; set; }
        public decimal? OverLength { get; set; }
        public decimal? OverWidth { get; set; }
        public decimal? OverHeight { get; set; }
        public string? DimensionUnitName { get; set; }
        public string? CntOwnTypeName { get; set; }
        public string? ExportPermissionNo { get; set; }
        public string? FullMTName { get; set; }
        public string? LoadTypeNameContainer { get; set; }
        public decimal? TareWeight { get; set; }
        public int? Seq { get; set; }

    }
    public class BLManagementSummaryDto_ContainerReport
    {
        public int TotalBills { get; set; }
        public int InProgressCount { get; set; }
        public int IncompleteCount { get; set; }
        public int MissingCarrierReferenceCount { get; set; }
    }
    //--------------------------------------------------------
    //----------------BL StimulSoft Report--------------------
    public class BLsReportCompleteDto
    {
        public int? CompanyId { get; set; }
        public Guid? Id { get; set; }
        public string? ShipperText { get; set; }
        public string? ConsigneeText { get; set; }
        public string? NotifyText { get; set; }
        public string? FinalDestinationText { get; set; }
        public string? BLNo { get; set; }
        public string? GoodsDescription { get; set; }
        public string? JobFileNo { get; set; }
        public string? JobNo { get; set; }
        public string? CarriresAgentAtDischargePort { get; set; }
        public string? PORText { get; set; }
        public string? POLText { get; set; }
        public string? PODText { get; set; }
        public string? PODelText { get; set; }
        public string? VesseleName2 { get; set; }
        public string? VoyageNo2 { get; set; }
        public string? VesseleName1 { get; set; }
        public string? VoyageNo1 { get; set; }
        public DateTime? BLIssueDate { get; set; }
        public DateTime? MasterBLIssueDate { get; set; }
        public string? IssuerParty { get; set; }
        public string? PackageTypeText { get; set; }
        public string? RemarksInManifest { get; set; }
        public string? PaymentTypeText { get; set; }
        public string? HSCode { get; set; }
        public string? TradeType { get; set; }
        public DateTime? ArrivalDate { get; set; }
        public List<BLContainersReportDto> BLContainersDetails { get; set; }
    }

    public class BLContainersReportDto
    {
        //Container
        public string? ContainerNumber { get; set; }
        //Size - Type
        public string? ShortName { get; set; }
        //SealNo
        public string SealNumber { get; set; }
        //MoveType
        public string? LoadTypeCode { get; set; }
        //Package
        public int? TotalPackageCount { get; set; }
        //GrossWeight
        public decimal? TotalGrossWeight { get; set; }
        //TotalNetWeight
        public decimal? TotalNetWeight { get; set; }
        public decimal? TareWeight { get; set; }
    
        //SOC
        public string? CntOwnTypeName { get; set; }
        public string? SOC { get; set; }
        public string? FMT { get; set; }
        public string? Movement { get; set; }
    }

    //--------------------------------------------------------
}