using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.DTOs.EDI;
using ShipRate.Application.Interfaces;
using ShipRate.Common.Utilities;
using ShipRate.Domain.Entities.Operations;
using ShipRate.Infrastructure.Data;
using System.Globalization;
using System.Text;
namespace ShipRate.Application.Services
{
    public class EDIService : IEDIService
    {
        private readonly ApplicationDbContext _db;
        public EDIService(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<List<EdiMcntDto>> GetDistinctContainersAsync(
            List<Guid> blIds,
            CancellationToken cancellationToken = default)
        {
            if (blIds == null || blIds.Count == 0)
                return new List<EdiMcntDto>();

            var containers = await _db.BLs
                .AsNoTracking()
                .Where(bl => blIds.Contains(bl.Id))
                // پاس دادن همزمان والد (bl) و فرزند (c) به مراحل بعد
                .SelectMany(bl => bl.BLContainers, (bl, c) => new { bl, c })
                .Where(x => !string.IsNullOrEmpty(x.c.ContainerNumber))
                .Select(x => new
                {
                    ContainerNumber = x.c.ContainerNumber.Length >= 11
                        ? x.c.ContainerNumber.Substring(0, 10) + "-" + x.c.ContainerNumber.Substring(10, 1)
                        : x.c.ContainerNumber,
                    IsoCode = x.c.ContainerSizeType != null ? x.c.ContainerSizeType.IsoCode : string.Empty,
                    FullEmpty = x.c.FullMT != null ? x.c.FullMT.FullMTCode : string.Empty,
                    SealNumber = x.c.SealNumber ?? string.Empty,
                    TareWeight = x.c.TareWeight.HasValue ? x.c.TareWeight.Value.ToString("0") : string.Empty,
                    Owner = x.c.CntOwnType != null ? x.c.CntOwnType.CntOwnTypeName : string.Empty,
                    LoadPort = x.bl.POL.LocationCode,
                    DischargePort = x.bl.POD.LocationCode,
                    Destination = x.bl.PODel.LocationCode,
                    TransStatus = x.bl.TradeType.CoparnCode,
                    FCLLCLType = x.c.LoadType.CoparnCode
                })
                .GroupBy(x => x.ContainerNumber)
                .Select(g => g.First())
                .Select(x => new EdiMcntDto
                {
                    MCntNumber = x.ContainerNumber,
                    MCNTIsoCode = x.IsoCode,
                    MCntFullEmpty = x.FullEmpty,
                    MCntTransStatus = x.TransStatus,
                    MCntFCLLCLType = x.FCLLCLType,

                    // لوکیشن‌های خوانده‌شده از BL
                    MCNTLoadPort = x.LoadPort ?? string.Empty,
                    MCNTDischargePort = x.DischargePort ?? string.Empty,
                    MCNTDestination = x.Destination ?? string.Empty,

                    MCntTaireWeight = x.TareWeight,
                    MCntTransportDegree = 0,
                    MCntSealNumber = x.SealNumber,
                    MCntSealingPartyCode = "SH",
                    MCntSealConditionCode = "1",
                    MCntHazardCode = string.Empty,
                    MCNTUNDG = string.Empty,
                    MCntOwner = x.Owner
                })
                .ToListAsync(cancellationToken);
            return containers;
        }

        public async Task<List<EDIMessageTypeRowDto>> GetMessageTypesAsync()
        {
            return await _db.EDIMessageTypes
                .Where(x => x.IsActive == true)
                .Select(x => new EDIMessageTypeRowDto
                {
                    Id = x.Id,
                    EDIMessageTypeCode = x.EDIMessageTypeCode,
                    EDIMessageTypeName = x.EDIMessageTypeName,
                    EDIMessageTypeStandard = x.EDIMessageTypeStandard,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }

        public async Task<EDIMessageListViewModel> GetMessagesAsync(EDIMessageSearchInput input)
        {
            var messageType = await _db.EDIMessageTypes
                .AsNoTracking()
                .Where(x => x.Id == input.MessageTypeId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.EDIMessageTypeCode,
                    x.EDIMessageTypeName
                })
                .FirstOrDefaultAsync();

            if (messageType == null)
            {
                return new EDIMessageListViewModel
                {
                    MessageTypeId = input.MessageTypeId,
                    SearchTerm = input.SearchTerm,
                    PageNumber = input.PageNumber,
                    PageSize = input.PageSize,
                    Result = new PagedResultDto<EDIMessageRowDto>()
                };
            }

            var query = from m in _db.EDIMessages.AsNoTracking()
                        join mt in _db.EDIMessageTypes.AsNoTracking() on m.MessageTypeId equals mt.Id
                        join io in _db.InOut.AsNoTracking() on m.InOutId equals io.Id
                        where !m.IsDeleted && m.MessageTypeId == input.MessageTypeId
                        select new EDIMessageRowDto
                        {
                            Id = m.Id,
                            MessageTypeId = m.MessageTypeId,
                            MessageTypeCode = mt.EDIMessageTypeCode,
                            InOutName = io.InOutName,
                            ReferenceNumber = m.ReferenceNumber,
                            ReceivedAt = m.ReceivedAt,
                            ProcessedAt = m.ProcessedAt,
                            ErrorMessage = m.ErrorMessage,
                            Remarks = m.Remarks,
                            //IsActive = m.IsActive,
                            CreatedAt = m.CreatedAt
                        };

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x =>
                    x.ReferenceNumber.Contains(term) ||
                    x.InOutName.Contains(term) ||
                    x.ErrorMessage.Contains(term) ||
                    x.Remarks.Contains(term));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((input.PageNumber - 1) * input.PageSize)
                .Take(input.PageSize)
                .ToListAsync();

            return new EDIMessageListViewModel
            {
                MessageTypeId = messageType.Id,
                MessageTypeCode = messageType.EDIMessageTypeCode,
                MessageTypeName = messageType.EDIMessageTypeName,
                SearchTerm = input.SearchTerm,
                PageNumber = input.PageNumber,
                PageSize = input.PageSize,
                Result = new PagedResultDto<EDIMessageRowDto>
                {
                    Items = items,
                    TotalCount = totalCount
                }
            };
        }

        public async Task<(byte[] Content, string FileName, string ContentType)?> DownloadPayloadAsync(Guid id)
        {
            var item = await _db.Set<EDIMessage>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.ReferenceNumber,
                    x.Payload
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return null;

            var content = System.Text.Encoding.UTF8.GetBytes(item.Payload ?? string.Empty);
            var fileName = $"EDI_{item.ReferenceNumber}_{item.Id:N}.txt";

            return (content, fileName, "text/plain");
        }

        public async Task<EDIActionModalViewModel?> GetActionModalInfoAsync(int messageTypeId, string actionName)
        {
            var item = await _db.EDIMessageTypes
                .AsNoTracking()
                .Where(x => x.Id == messageTypeId && !x.IsDeleted)
                .Select(x => new EDIActionModalViewModel
                {
                    MessageTypeId = x.Id,
                    MessageTypeCode = x.EDIMessageTypeCode,
                    MessageTypeName = x.EDIMessageTypeName,
                    ActionName = actionName
                })
                .FirstOrDefaultAsync();

            return item;
        }
        public async Task<string> GenerateEPLAsync(Guid voyageId, Guid portId, List<Guid> blIds, int companyId)
        {
            string output = "VOY".DoubleQuoted();
            var voyage = await _db.Voyages
                .Include(x => x.Vessel)
                .AsNoTracking()
                .Where(x => x.Id == voyageId).FirstOrDefaultAsync();
            var dash = "-".DoubleQuoted();
            var space = "".DoubleQuoted();
            var MFI = "MFI".DoubleQuoted();
            var Zero = "0".DoubleQuoted();
            var BOL = "BOL".DoubleQuoted();

            var person = await _db.Companies
                .AsNoTracking()
                .Include(x => x.Person)
                .Where(x => x.Id == companyId).FirstOrDefaultAsync();

            output += $",{person.Person.NationalID.DoubleQuoted()},{person.Person.NationalID.DoubleQuoted()},{voyage.Vessel.VesselName.DoubleQuoted()}";
            output += $",{voyage.VoyageNo.DoubleQuoted()}";
            output += $"{dash},{dash},{space},{MFI},{Zero},{voyage.VoyageNo.DoubleQuoted()}";
            output += Environment.NewLine;

            foreach (var blId in blIds)
            {
                output += GetEPLForBL(blId);
                output += Environment.NewLine;
            }
            return output;
        }
        private async Task<string> GetEPLForBL(Guid blId)
        {
            var bl = await _db.BLs.AsNoTracking().Where(x => x.Id == blId).FirstOrDefaultAsync();
            var output = "BOL".DoubleQuoted();
            output += $",{bl.MasterBLNo.DoubleQuoted()},{bl.Carrier.Person.NationalID.DoubleQuoted()},{bl.Carrier.Person.NationalID.DoubleQuoted()},{bl.POR.LocationCode.DoubleQuoted()},{bl.POL.LocationCode.DoubleQuoted()}";
            output += $",{bl.POD.LocationCode.DoubleQuoted()},{bl.PODel.LocationCode.DoubleQuoted()},{string.Empty.DoubleQuoted()},{string.Empty.DoubleQuoted()}";

            if (bl.TradeTypeId == 1)
                output += $",{'I'.ToString().DoubleQuoted()}";
            else
                output += $",{'T'.ToString().DoubleQuoted()}";

            output += $",{'S'.ToString().DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{'F'.ToString().DoubleQuoted()}";
            output += $",{'N'.ToString().DoubleQuoted()}";
            output += $",{'Y'.ToString().DoubleQuoted()}";

            output += $",{bl.LoadType.LoadTypeCode.DoubleQuoted()}";  //FCL/FCL
            output += $",{bl.POR.Country.CountryIsoCode2.DoubleQuoted()}";

            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";

            //Shipper
            output += $",{bl.BLParties.Where(x => x.RoleTypeId == 51).FirstOrDefault().PartyTextInBL.DoubleQuoted()}";
            output += $",{'-'.ToString().DoubleQuoted()}";
            output += $",{bl.POR.Country.CountryIsoCode2.DoubleQuoted()}";


            //Consignee
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{bl.BLParties.Where(x => x.RoleTypeId == 52).FirstOrDefault().PartyTextInBL.DoubleQuoted()}";
            output += $",{'-'.ToString().DoubleQuoted()}";

            //Notify
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{bl.BLParties.Where(x => x.RoleTypeId == 53).FirstOrDefault().PartyTextInBL.DoubleQuoted()}";
            output += $",{'-'.ToString().DoubleQuoted()}";




            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";



            output += $",{bl.CargoDetails.FirstOrDefault().HSCodeToSave.DoubleQuoted()}";
            output += $",{string.Empty.DoubleQuoted()}";

            output += $",{bl.TotalPackages.ToString().DoubleQuoted()}";
            output += $",{bl.PackageType.PackageTypeName.DoubleQuoted()}";
            output += $",{bl.PackageType.PackageTypeCode.DoubleQuoted()}";



            output += $",{bl.BLContainers.FirstOrDefault().ContainerNumber.DoubleQuoted()}";
            output += $",{'1'.ToString().DoubleQuoted()}";
            output += $",{bl.BLContainers.Count().ToString().DoubleQuoted()}";

            var teus = bl.BLContainers.Where(x => x.ContainerSizeType.ContainerSizeId == 20).Count() + bl.BLContainers.Where(x => x.ContainerSizeType.ContainerSizeId == 40).Count() * 2;
            output += $",{teus.ToString().DoubleQuoted()}";


            output += $",{bl.BLContainers.Sum(x => x.TareWeight).ToString().DoubleQuoted()}";
            output += $",{bl.NetWeightKg.ToString().DoubleQuoted()}";
            output += $",{bl.GrossWeightKg.ToString().DoubleQuoted()}";
            output += $",{bl.VolumeCBM.ToString().DoubleQuoted()}";

            output += $",{bl.TotalPackages.ToString().DoubleQuoted()}";
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{'0'.ToString().DoubleQuoted()}";
            output += $",{'0'.ToString().DoubleQuoted()}";

            output += Environment.NewLine;

            if (bl.BLContainers.Count > 0)
            {
                foreach (var item in bl.BLContainers)
                {
                    output += "CTR".DoubleQuoted();
                    output += $",{item.ContainerNumber.DoubleQuoted()},{1.ToString().DoubleQuoted()},{item.TareWeight.ToString().DoubleQuoted()},{item.SealNumber.DoubleQuoted()} ";
                    //ادامه دارد - کانتینر خالی و ...
                    output += Environment.NewLine;

                    foreach (var item2 in item.CargoAllocations)
                    {
                        output += "CON".DoubleQuoted();
                        output += $",{item.Id.ToString().DoubleQuoted()}";
                        output += $",{string.Empty.DoubleQuoted()}";
                        output += $",{'0'.ToString().DoubleQuoted()}";
                        output += $",{'N'.ToString().DoubleQuoted()}";
                        output += $",{item2.CargoDetail.HSCodeToSave.DoubleQuoted()}";
                        output += $",{item2.PackageCount.ToString().DoubleQuoted()}";
                        output += $",{item2.CargoDetail.PackageType.PackageTypeName.ToString().DoubleQuoted()}";
                        output += $",{item2.CargoDetail.PackageType.PackageTypeCode.ToString().DoubleQuoted()}";
                        output += $",{item2.PackageCount.ToString().DoubleQuoted()}";
                        output += $",{item2.GrossWeight.ToString().DoubleQuoted()}";
                        output += $",{item2.Volume.ToString().DoubleQuoted()}";
                    }
                }
            }
            else
            {
                output += "CTR".DoubleQuoted();
                var cntno = "BULK123456-7";
                output += $",{cntno.DoubleQuoted()},{'1'.ToString().DoubleQuoted()},{'1'.ToString().DoubleQuoted()},{'1'.ToString().DoubleQuoted()},{'0'.ToString().DoubleQuoted()}";
            }

































            return output;
        }



        //*********************************************************************************************************************************************************************************************
        //*********************************************************************************************************************************************************************************************
        public async Task<string> GenerateCoparnAsync(Guid voyageId, Guid portId, List<Guid> blIds, int companyId)
        {
            var voyage = await _db.Voyages
                .Include(x => x.Vessel)
                .Where(x => x.Id == voyageId).FirstOrDefaultAsync();

            var port = await _db.Locations.FindAsync(portId);
            string portCode = port.LocationCode;


            var goods = await _db.BLs.Where(x => blIds.Contains(x.Id))
                .Select(x => new EdiGoodsDto
                {
                    BLId = x.Id,
                    Blno = x.MasterBLNo,
                    BLDate = x.MasterBLIssueDate.Value.ToString("yyyyMMdd"),
                    Shipper = x.BLParties.Where(x => x.RoleTypeId == 51).FirstOrDefault().PartyTextInBL,
                    Consignee = x.BLParties.Where(x => x.RoleTypeId == 52).FirstOrDefault().PartyTextInBL,
                    Notify = x.BLParties.Where(x => x.RoleTypeId == 53).FirstOrDefault().PartyTextInBL,
                    ConsigneeNationalCode = x.BLParties.Where(x => x.RoleTypeId == 52).FirstOrDefault().Party.NationalID,
                    Descript = x.GoodsDescription,
                    HsCode = x.CargoDetails.FirstOrDefault().HSCodeToSave,
                    NetWeight = x.NetWeightKg.ToString(),
                    NumberOfPackages = x.TotalPackages,
                    OrderRegistrationNo = x.OrderRegistrationNo,
                    PackageDescript = x.PackageType.PackageTypeName,
                    TypeOfPackages = x.PackageType.PackageTypeCode
                })
                .ToListAsync();








            var edi = new EDICoparnSegment(
                sender: "50105",
                receiver: "PSO",
                interchangeReference: "1287");

            var lines = new List<string>
                        {
                            edi.UNB(),
                            edi.UNH("1"),
                            edi.BGM(),
                            edi.RFF("ZZZ",string.Empty,string.Empty),
                            edi.TDT(voyage.VoyageNo,"",voyage.Vessel.VesselName,""),
                            edi.LOC("", 9),
                            edi.LOC(portCode, 11),
                            edi.DTM(DateTime.Now.ToString("yyyyMMdd",CultureInfo.InvariantCulture),132),
                            edi.DTM(DateTime.Now.ToString("yyyyMMdd",CultureInfo.InvariantCulture),133),
                            edi.NAD("CA","STA"),   //Carrier
                            edi.NAD("CG","Saeei Tarabar")   //Carrier Agent
                        };
            string coparn = string.Join(Environment.NewLine, lines);

            var goodsCounter = 0;
            foreach (var bl in goods)
            {
                goodsCounter++;
                coparn += Environment.NewLine;
                coparn += MakeGoods(edi, bl, goodsCounter);
            }




            var mcntList = await GetDistinctContainersAsync(blIds);
            foreach (var mcnt in mcntList)
            {
                MakeMCnt(edi, mcnt);
            }

            var endlines = new List<string>
                        {
                            edi.cnt(1),
                            edi.UNT(),
                            edi.UNZ()
                        };
            coparn += string.Join(Environment.NewLine, endlines);
            return coparn;
        }

        private string MakeGoods(EDICoparnSegment edi, EdiGoodsDto goods, int sequence)
        {
            var sb = new StringBuilder();

            // GID - مشخصات بسته‌بندی
            sb.AppendLine(edi.gID(sequence, goods.NumberOfPackages, goods.TypeOfPackages, goods.PackageDescript));

            // FTX - شرح کالا
            sb.AppendLine(edi.FTX("AAA", goods.Descript));

            // RFF - شماره بارنامه
            sb.AppendLine(edi.RFF("BM", goods.Blno, (goods.OrderRegistrationNo ?? string.Empty).Trim()));

            // DTM - تاریخ بارنامه (در صورت وجود)
            if (!string.IsNullOrWhiteSpace(goods.BLDate) && goods.BLDate.Length > 1)
            {
                sb.AppendLine(edi.DTMBLDate(goods.BLDate));
            }

            // PIA - کد تعرفه گمرکی (HS Code)
            sb.AppendLine(edi.PIA(goods.HsCode));

            // NAD - گیرنده کالا (Consignee) همراه با شناسه ملی و آدرس
            sb.AppendLine(edi.NADNew(
                "CN",
                (goods.Consignee ?? string.Empty).Trim(),
                goods.ConsigneeNationalCode ?? string.Empty,
                (goods.SUPCONSIGNEEADDRESS ?? string.Empty).Trim()
            ));

            // DTM - تاریخ تولد / تأسیس (در صورت وجود)
            if (!string.IsNullOrWhiteSpace(goods.BirthDate) && goods.BirthDate.Length > 1)
            {
                sb.AppendLine(edi.CNBirthDate(goods.BirthDate));
            }

            // NAD - فرستنده کالا (Shipper)
            sb.AppendLine(edi.NAD("CZ", goods.Shipper ?? string.Empty));

            // NAD - مطلع شونده (Notify Party)
            sb.AppendLine(edi.NAD("NI", goods.Notify ?? string.Empty));

            // MEA - وزن خالص یا ناخالص کالا
            sb.AppendLine(edi.MEA("G", goods.NetWeight));

            var goodsCnt = _db.BLContainers
                            .Where(x => x.BLId == goods.BLId)
                            .Select(x => new EdiGoodsCntDto
                            {
                                BLContainerId = x.Id,
                                ContainerNumber = x.ContainerNumber.Substring(0, 10) + "-" + x.ContainerNumber.Substring(10, 1),
                                MeasureValue = (double)x.TotalGrossWeight,
                                NumberOfPackages = x.TotalPackageCount
                            }).ToList();

            foreach (var cnt in goodsCnt)
            {
                sb.AppendLine(edi.SGP(cnt.ContainerNumber, cnt.NumberOfPackages));
                sb.AppendLine(edi.MEA("G", cnt.MeasureValue.ToString()));


                var hsCodes = _db.BLContainerCargoAllocations
                                .Where(x => x.BLContainerId == cnt.BLContainerId)
                                .Select(x => new EdiGoodsCntHsCodeDto
                                {
                                    CargoInfoHSCode = x.CargoDetail.HSCodeToSave,
                                    CargoInfoMeasureValue = (double)x.GrossWeight,
                                    CargoInfoNumberOfPackages = (long)x.PackageCount
                                })
                                .ToList();
                sb.AppendLine("CPID");
                foreach (var item in hsCodes)
                {
                    sb.Append("+" + edi.GoodsCntHsCode(item.CargoInfoHSCode, item.CargoInfoMeasureValue, item.CargoInfoNumberOfPackages));
                }
            }
            return sb.ToString();
        }

        public string MakeMCnt(EDICoparnSegment edi, EdiMcntDto mCnt)
        {
            var sb = new StringBuilder();

            // EQD - اطلاعات کانتینر
            sb.Append(edi.EQD(mCnt.MCntNumber, mCnt.MCNTIsoCode, mCnt.MCntFullEmpty, mCnt.MCntTransStatus));

            // RFF - شماره کانتینر (برش ۴ کاراکتر اول)
            string rffRef = mCnt.MCntNumber.Length >= 4 ? mCnt.MCntNumber.Substring(0, 4) : mCnt.MCntNumber;
            sb.Append(edi.RFF("CPR", rffRef, ""));

            // TMD - نوع FCL/LCL
            sb.Append(edi.TMD(mCnt.MCntFCLLCLType));

            sb.Append(edi.LOC(mCnt.MCNTLoadPort, 9));   //loadPort
            sb.Append(edi.LOC(mCnt.MCNTDischargePort, 11));  //dischargePort
            sb.Append(edi.LOC(mCnt.MCNTDestination, 13)); //deliveryPlace

            // MEA - وزن کانتینر
            sb.Append(edi.MEA("T", mCnt.MCntTaireWeight));

            if (mCnt.MCNTIsoCode.Length >= 3 && mCnt.MCNTIsoCode[2] == '3')
            {
                sb.Append(edi.tmp(mCnt.MCntTransportDegree));
            }
            // SEL - پلمپ
            sb.Append(edi.SEL(mCnt.MCntSealNumber, mCnt.MCntSealingPartyCode, mCnt.MCntSealConditionCode));

            // DGS - کالای خطرناک (در صورت وجود کد خطر)
            if (int.TryParse(mCnt.MCntHazardCode, out int hazardVal) && hazardVal > 0)
            {
                sb.Append(edi.DGS(mCnt.MCntHazardCode, mCnt.MCNTUNDG));
            }
            sb.Append(edi.NAD("EO", mCnt.MCntOwner));
            sb.Append(edi.NAD("AG", "STA"));
            return sb.ToString();
        }
        //*********************************************************************************************************************************************************************************************
        //*********************************************************************************************************************************************************************************************
        public async Task<string> GenerateTCTSFile(Guid voyageId, Guid portId, List<Guid> blIds, int companyId)
        {
            var voyage = _db.Voyages.Where(x => x.Id == voyageId).FirstOrDefault();

            var output = "MYear|KeshtiCode|TripNo|POD|POL|ArrivalDate|DepartureDate|ManiFestCode|BarnamehNoIn|";
            output += Environment.NewLine;
            output += $"85|0001|{voyage.VoyageNo}|||2005/01/01|2005/07/01|{voyage.VoyageNo}|83";
            output += Environment.NewLine;
            output += Environment.NewLine;
            output += "TbExpMBL";
            output += Environment.NewLine;
            output += $"MYear|KeshtiCode|TripNo|BLNumber|GoodDescription|NoOfPackageBL|TypeOfPackageBL|GrossWeight|Consignee|Consignor|Notify|BlCode|Remark|Stime|";

            foreach (var blId in blIds)
            {
                var bl = _db.BLs.Where(x => x.Id == blId).FirstOrDefault();
                output += Environment.NewLine;
                output += $"85|0001|{voyage.VoyageNo}|{bl.MasterBLNo}|--|{bl.TotalPackages}|{bl.PackageType.PackageTypeCode}|{bl.GrossWeightKg}|{bl.BLParties.Where(x => x.RoleTypeId == 52).FirstOrDefault().PartyTextInBL}|-|{bl.BLParties.Where(x => x.RoleTypeId == 53).FirstOrDefault().PartyTextInBL}|{bl.CargoDetails.FirstOrDefault().HSCodeToSave.Substring(0, 8)}|--|11/29/2025 1:21:48 PM";
            }
            output += Environment.NewLine;
            output += "TbExpMBContainer";
            output += Environment.NewLine;
            output += "MYear|KeshtiCode|TripNo|ConFlag|SealNumber|CSize|CType|NoeInput|F_E|MovementType|POD|Des|TareWeight|TxtJavazWei|ContainerOwner|TxtPlanSto|IMDGCode|UNNumber|";
            output += "HazardPageNumber|EquipmentFlashPoint|TxtHumm|TxtVentilation|TxtRfTem|TxtTemJavazNo|HazardVersion|Remark|Stime|Line|TIP|ExpPermission|";

            var cnts = _db.BLContainers
                .Where(c => blIds.Contains(c.BLId))
                .GroupBy(c => c.ContainerNumber)
                .Select(group => group.FirstOrDefault())
                .Select(c => new
                {
                    // اطلاعات از جدول کانتینر
                    ContainerNumber = $"{c.ContainerNumber.Substring(0, 10)}-{c.ContainerNumber.Substring(11, 1)}",
                    SealNumber = c.SealNumber,
                    CSize = c.ContainerSizeType.ContainerSizeId.ToString(),
                    CType = c.ContainerSizeType.ContainerType.TCTSCode.ToString(),
                    NoeInput = c.BL.TradeType.TCTSCode,
                    F_E = c.FullMT.TCTSCode,
                    MovementType = c.LoadType.TCTSCode,
                    POD = $"{c.BL.POD.LocationCode.Substring(0, 2)} {c.BL.POD.LocationCode.Substring(2, 3)}",
                    Des = $"{c.BL.PODel.LocationCode.Substring(0, 2)} {c.BL.PODel.LocationCode.Substring(2, 3)}",
                    TareWeight = c.TareWeight,
                    TxtJavazWei = "",
                    ContainerOwner = "",
                    TxtPlanSto = "",
                    IMDGCode = "",//?????
                    UNNumber = "",//?????
                    HazardPageNumber = "", //?????
                    EquipmentFlashPoint = "",
                    TxtHumm = "",
                    TxtVentilation = "",
                    TxtRfTem = "",
                    TxtTemJavazNo = "",
                    HazardVersion = "",
                    Remark = "",
                    STime = "2005/01/01",
                    Line = "",
                    TIP = "",
                    ExpPermission = c.ExportPermissionNo
                }).ToList();
            foreach (var c in cnts)
            {
                output += Environment.NewLine;
                output += $"85|0001|{voyage.VoyageNo}|{c.ContainerNumber}|{c.SealNumber}|{c.CSize}|{c.CType}|{c.NoeInput}|{c.POD}|{c.Des}|{c.TareWeight}|{c.TxtJavazWei}|{c.ContainerOwner}|{c.TxtPlanSto}|{c.IMDGCode}|{c.UNNumber}|";
                output += $"{c.HazardPageNumber}|{c.EquipmentFlashPoint}|{c.TxtHumm}|{c.TxtVentilation}|{c.TxtRfTem}|{c.TxtTemJavazNo}|{c.HazardVersion}|{c.Remark}|{c.STime}|{c.Line}|{c.TIP}|{c.ExpPermission}|";
            }

            output += Environment.NewLine;
            output += "TbExpMBLContainer";
            output += Environment.NewLine;
            output += "MYear|KeshtiCode|TripNo|BLNumber|ConFlag|NoOfPackage|TxtPackType|NetWeight|Strip|Stime|";

            var blcnts = _db.BLContainers
                .Where(c => blIds.Contains(c.BLId))
                .Select(c => new
                {
                    // اطلاعات از جدول کانتینر
                    ContainerNumber = $"{c.ContainerNumber.Substring(0, 10)}-{c.ContainerNumber.Substring(11, 1)}",
                    NOP = c.TotalPackageCount,
                    NetWeight = c.TotalNetWeight,
                    Strip = "",
                    Stime = ""
                })
                .ToList();
            foreach (var c in blcnts)
            {
                output += Environment.NewLine;
                output += $"85|0001|{voyage.VoyageNo}|{c.ContainerNumber}|{c.NOP}|{c.NetWeight}|{c.Strip}|{c.Stime}|";
            }
            output += Environment.NewLine;
            output += "End";
            return output;
        }
        //*********************************************************************************************************************************************************************************************
        //*********************************************************************************************************************************************************************************************
        public async Task<EplPageViewModel> GetEplPageDataAsync(Guid voyagePortId)
        {
            var vp = await _db.VoyagePorts
                .AsNoTracking()
                .Include(x => x.InboundVoyage).ThenInclude(v => v.Vessel)
                .Include(x => x.Port)
                .FirstOrDefaultAsync(x => x.Id == voyagePortId);

            if (vp == null)
                throw new KeyNotFoundException($"VoyagePort with Id '{voyagePortId}' was not found.");

            var voyageId = vp.InboundVoyageId;
            var portId = vp.PortId;

            // استخراج BLIdهایی که سفر نهایی (IsFinal) آن‌ها مربوط به این سفر است
            var finalBlIdsQuery = _db.BLVoyages
                .AsNoTracking()
                .Where(bv => bv.IsFinal && bv.VoyageId == voyageId)
                .Select(bv => bv.BLId);

            // استخراج مستقیم فیلدها در قالب DTO بدون لود دامین انتیتی
            var bls = await (
                from bl in _db.BLs.AsNoTracking()
                where finalBlIdsQuery.Contains(bl.Id)
                      && (bl.PODId == portId || bl.PODelId == portId)
                      && bl.BLDeletedAt == null
                select new EplBlItemDto
                {
                    BLId = bl.Id,
                    MasterBLNo = bl.MasterBLNo ?? string.Empty,
                    BLNo = bl.BLNo,
                    ContainerCount = bl.BLContainers.Count(),
                    TotalPackages = bl.TotalPackages,
                    GrossWeightKg = bl.GrossWeightKg,
                    VolumeCBM = bl.VolumeCBM,
                    ShipperName = bl.BLParties
                        .Where(p => p.RoleTypeId == 51)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    ConsigneeName = bl.BLParties
                        .Where(p => p.RoleTypeId == 52)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    POLName = bl.POL != null ? bl.POL.LocationName : bl.POLText,
                    PODName = bl.POD != null ? bl.POD.LocationName : bl.PODText,
                    PODelName = bl.PODel != null ? bl.PODel.LocationName : bl.PODelText
                })
                .OrderBy(x => x.MasterBLNo)
                .ThenBy(x => x.BLNo)
                .ToListAsync();

            return new EplPageViewModel
            {
                Header = new EplPageHeaderDto
                {
                    VoyagePortId = vp.Id,
                    VoyageId = voyageId,
                    PortId = portId,
                    VoyageNo = vp.InboundVoyage?.VoyageNo ?? string.Empty,
                    VesselName = vp.InboundVoyage?.Vessel?.VesselName ?? string.Empty,
                    PortName = vp.Port?.LocationName ?? string.Empty
                },
                BLs = bls
            };
        }
        public async Task<CoparnPageViewModel> GetCoparnPageDataAsync(Guid voyagePortId)
        {
            var vp = await _db.VoyagePorts
                .AsNoTracking()
                .Include(x => x.InboundVoyage).ThenInclude(v => v.Vessel)
                .Include(x => x.Port)
                .FirstOrDefaultAsync(x => x.Id == voyagePortId);

            if (vp == null)
                throw new KeyNotFoundException($"VoyagePort with Id '{voyagePortId}' was not found.");

            var voyageId = vp.InboundVoyageId;
            var portId = vp.PortId;

            // استخراج BLIdهایی که سفر نهایی (IsFinal) آن‌ها مربوط به این سفر است
            var finalBlIdsQuery = _db.BLVoyages
                .AsNoTracking()
                .Where(bv => bv.IsFinal && bv.VoyageId == voyageId)
                .Select(bv => bv.BLId);

            // استخراج مستقیم فیلدها در قالب DTO بدون لود دامین انتیتی
            var bls = await (
                from bl in _db.BLs.AsNoTracking()
                where finalBlIdsQuery.Contains(bl.Id)
                      && (bl.PODId == portId || bl.PODelId == portId)
                      && bl.BLDeletedAt == null
                select new EplBlItemDto
                {
                    BLId = bl.Id,
                    MasterBLNo = bl.MasterBLNo ?? string.Empty,
                    BLNo = bl.BLNo,
                    ContainerCount = bl.BLContainers.Count(),
                    TotalPackages = bl.TotalPackages,
                    GrossWeightKg = bl.GrossWeightKg,
                    VolumeCBM = bl.VolumeCBM,
                    ShipperName = bl.BLParties
                        .Where(p => p.RoleTypeId == 51)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    ConsigneeName = bl.BLParties
                        .Where(p => p.RoleTypeId == 52)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    POLName = bl.POL != null ? bl.POL.LocationName : bl.POLText,
                    PODName = bl.POD != null ? bl.POD.LocationName : bl.PODText,
                    PODelName = bl.PODel != null ? bl.PODel.LocationName : bl.PODelText
                })
                .OrderBy(x => x.MasterBLNo)
                .ThenBy(x => x.BLNo)
                .ToListAsync();

            return new CoparnPageViewModel
            {
                Header = new EplPageHeaderDto
                {
                    VoyagePortId = vp.Id,
                    VoyageId = voyageId,
                    PortId = portId,
                    VoyageNo = vp.InboundVoyage?.VoyageNo ?? string.Empty,
                    VesselName = vp.InboundVoyage?.Vessel?.VesselName ?? string.Empty,
                    PortName = vp.Port?.LocationName ?? string.Empty
                },
                BLs = bls
            };
        }

        public async Task<EplPageViewModel> GetTCTSPageDataAsync(Guid voyagePortId)
        {
            var vp = await _db.VoyagePorts
                .AsNoTracking()
                .Include(x => x.OutboundVoyage).ThenInclude(v => v.Vessel)
                .Include(x => x.Port)
                .FirstOrDefaultAsync(x => x.Id == voyagePortId);

            if (vp == null)
                throw new KeyNotFoundException($"VoyagePort with Id '{voyagePortId}' was not found.");

            var voyageId = vp.OutboundVoyageId;
            var portId = vp.PortId;

            // استخراج BLIdهایی که سفر نهایی (IsFinal) آن‌ها مربوط به این سفر است
            var finalBlIdsQuery = _db.BLVoyages
                .AsNoTracking()
                .Where(bv => bv.VoyageId == voyageId)
                .Select(bv => bv.BLId);

            // استخراج مستقیم فیلدها در قالب DTO بدون لود دامین انتیتی
            var bls = await (
                from bl in _db.BLs.AsNoTracking()
                where finalBlIdsQuery.Contains(bl.Id)
                      && (bl.POLId == portId || bl.PORId == portId)
                      && bl.BLDeletedAt == null
                select new EplBlItemDto
                {
                    BLId = bl.Id,
                    MasterBLNo = bl.MasterBLNo ?? string.Empty,
                    BLNo = bl.BLNo,
                    ContainerCount = bl.BLContainers.Count(),
                    TotalPackages = bl.TotalPackages,
                    GrossWeightKg = bl.GrossWeightKg,
                    VolumeCBM = bl.VolumeCBM,
                    ShipperName = bl.BLParties
                        .Where(p => p.RoleTypeId == 51)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    ConsigneeName = bl.BLParties
                        .Where(p => p.RoleTypeId == 52)
                        .Select(p => p.PartyTextInBL).FirstOrDefault() ?? string.Empty,
                    POLName = bl.POL != null ? bl.POL.LocationName : bl.POLText,
                    PODName = bl.POD != null ? bl.POD.LocationName : bl.PODText,
                    PODelName = bl.PODel != null ? bl.PODel.LocationName : bl.PODelText
                })
                .OrderBy(x => x.MasterBLNo)
                .ThenBy(x => x.BLNo)
                .ToListAsync();

            return new EplPageViewModel
            {
                Header = new EplPageHeaderDto
                {
                    VoyagePortId = vp.Id,
                    VoyageId = voyageId,
                    PortId = portId,
                    VoyageNo = vp.OutboundVoyage?.VoyageNo ?? string.Empty,
                    VesselName = vp.OutboundVoyage?.Vessel?.VesselName ?? string.Empty,
                    PortName = vp.Port?.LocationName ?? string.Empty
                },
                BLs = bls
            };
        }
    }
}



public sealed class EDICoparnSegment
{
    // شمارش از UNH تا UNT؛ شامل هر دو، بدون UNB و UNZ
    private int _numberOfSegments;
    private int _numberOfMessages;
    private string? _messageReference;
    private bool _messageOpen;

    private readonly string _sender;
    private readonly string _receiver;
    private readonly string _interchangeReference;

    public DateTime PreparedAt { get; set; } = DateTime.Now;

    public EDICoparnSegment(
        string sender = "ShipRate",
        string receiver = "PSO",
        string interchangeReference = "11999")
    {
        _sender = sender;
        _receiver = receiver;
        _interchangeReference = interchangeReference;
    }

    // Interchange header: در UNT شمرده نمی‌شود.
    public string UNB()
    {
        string dateTime = PreparedAt.ToString(
            "yyMMdd:HHmm",
            CultureInfo.InvariantCulture);

        return $"UNB+UNOA:2+{E(_sender)}+{E(_receiver)}+" +
               $"{dateTime}+{E(_interchangeReference)}+++H'";
    }

    public string UNH(string messageReference)
    {
        if (_messageOpen)
            throw new InvalidOperationException(
                "پیش از UNH جدید، پیام قبلی را با UNT ببندید.");

        _messageReference = messageReference;
        _numberOfSegments = 0;
        _messageOpen = true;

        return Segment($"UNH+{E(messageReference)}+COPARN:D:00A:UN");
    }

    public string BGM()
        => Segment($"BGM+27+{_interchangeReference}+9+AB");

    public string FTX(string ftxType, string descript)
        => Segment($"FTX+{E(ftxType)}+++{E(descript)}");

    // callSign در کد VB6 ورودی بود، ولی در خروجی استفاده نمی‌شد.
    public string TDT(
        string voyageNo,
        string callSign,
        string vesselName,
        string carrierName)
        => Segment(
            $"TDT+20+{E(voyageNo)}+1+11++++:103::" +
            $"{E(vesselName)}:{E(carrierName)}");

    // برای enumهای موجود پروژه: (int)placeType
    public string LOC(string portPlace, int placeType)
        => Segment(
            $"LOC+{placeType.ToString(CultureInfo.InvariantCulture)}+" +
            $"{E(portPlace)}:139:6");

    // برای enumهای موجود پروژه: (int)dateTimeG1TypeVal
    public string DTM(string dateTimeVal, int dateTimeG1TypeVal)
        => Segment(
            $"DTM+{dateTimeG1TypeVal.ToString(CultureInfo.InvariantCulture)}:" +
            $"{E(dateTimeVal)}:102");

    // نام gID برای سازگاری با کد قبلی حفظ شده است.
    public string gID(
        int sequence,
        long noOfPackage,
        string typeOfPackage,
        string packageDescript)
        => Segment(
            $"GID+{sequence.ToString(CultureInfo.InvariantCulture)}+" +
            $"{noOfPackage.ToString(CultureInfo.InvariantCulture)}:" +
            $"{E(typeOfPackage)}::9:{E(packageDescript)}");

    public string MEA(string meaType, string measurementVal)
        => Segment($"MEA+AAE+{E(meaType)}+KGM:{E(measurementVal)}");

    public string MEAPackage(string meaType, string measurementVal)
        => Segment($"MEA+AAE+{E(meaType)}+PK:{E(measurementVal)}");

    // منطق علامت‌گذاری دمای VB6 عیناً حفظ شده است.
    public string tmp(double temperature)
    {
        string prefix = temperature > 0 ? "1" : "0";
        string absoluteValue = Math.Abs(temperature)
            .ToString("0.################", CultureInfo.InvariantCulture);

        return Segment($"TMP+2+{prefix}{absoluteValue}:CEL");
    }

    public string SGP(string cntNo, long noOfPackage)
        => Segment(
            $"SGP+{E(cntNo)}+" +
            noOfPackage.ToString(CultureInfo.InvariantCulture));

    // orderRegistrationNo در پیاده‌سازی VB6 نیز در خروجی استفاده نمی‌شد.
    public string RFF(
        string rffType,
        string descript,
        string orderRegistrationNo)
        => Segment($"RFF+{E(rffType)}:{E(descript)}");

    public string DTMBLDate(string blDate)
        => Segment($"DTM+199:{E(blDate)}:201");

    public string PIA(string descript)
    {
        if (descript == "*")
            descript = "86090000";

        return Segment($"PIA+1+{E(descript)}:HS");
    }

    public string EQD(
        string containerNumber,
        string containerType,
        string containerFillType,
        string transStaus)
        => Segment(
            $"EQD+CN+{E(containerNumber)}+:::{E(containerType)}++" +
            $"{E(transStaus)}+{E(containerFillType)}");

    public string TMD(string tmdValue)
        => Segment($"TMD+{E(tmdValue)}");

    public string SEL(
        string sealNumber,
        string sealParty,
        string sealCondition)
        => Segment(
            $"SEL+{E(sealNumber)}+{E(sealParty)}+{E(sealCondition)}");

    public string NAD(string nadType, string party)
        => Segment($"NAD+{E(nadType)}+++{E(party)}");

    public string NADNew(
        string nadType,
        string party,
        string nationalCode,
        string supConsigneeAddress)
        => Segment(
            $"NAD+{E(nadType)}++{E(nationalCode)}+" +
            $"{E(party)}+{E(supConsigneeAddress)}+");

    public string CNBirthDate(string birthDate)
        => Segment($"DTM+200:{E(birthDate)}:201");

    public string DGS(string dgsCode, string dgUnNo)
        => Segment($"DGS+IMD+{E(dgsCode)}+{E(dgUnNo)}");

    // در VB6 به‌جای «16:2» خروجی «16 2» تولید می‌شد.
    public string cnt(long no)
        => Segment(
            $"CNT+16:{no.ToString(CultureInfo.InvariantCulture)}");

    public string UNT()
    {
        if (!_messageOpen || _messageReference is null)
            throw new InvalidOperationException("ابتدا UNH را فراخوانی کنید.");

        // خود UNT هم جزو تعداد قطعات پیام است.
        string result = Segment(
            $"UNT+{_numberOfSegments + 1}+{E(_messageReference)}");

        _messageOpen = false;
        _numberOfMessages++;

        return result;
    }

    // Interchange trailer: در UNT شمرده نمی‌شود.
    public string UNZ()
    {
        if (_messageOpen)
            throw new InvalidOperationException(
                "پیش از UNZ، پیام را با UNT ببندید.");

        return $"UNZ+{_numberOfMessages}+" +
               $"{E(_interchangeReference)}'";
    }

    // این متد فقط یک جزء از دادهٔ CPID می‌سازد، نه یک segment کامل؛
    // بنابراین برخلاف VB6 شمارندهٔ قطعات را افزایش نمی‌دهد.
    public string GoodsCntHsCode(
        string hsCode,
        double weight,
        long countPkg)
        => $"{E(hsCode)}:" +
           $"{weight.ToString(CultureInfo.InvariantCulture)}:" +
           $"{countPkg.ToString(CultureInfo.InvariantCulture)}+";

    private string Segment(string content)
    {
        if (!_messageOpen)
            throw new InvalidOperationException(
                "برای تولید قطعات پیام ابتدا UNH را فراخوانی کنید.");

        _numberOfSegments++;
        return content + "'";
    }


    public string MakeFooter(long cntNo)
    {
        // ساخت سه سگمنت انتهایی پیام به ترتیب
        string s = cnt(cntNo) + Environment.NewLine +
                   UNT() + Environment.NewLine +
                   UNZ();

        return s;
    }



    // Escape کردن کاراکترهای ساختاری EDIFACT در داده‌های ورودی
    private static string E(string? value)
        => (value ?? string.Empty)
            .Trim()
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("?", "??")
            .Replace("+", "?+")
            .Replace(":", "?:")
            .Replace("'", "?'");
}




public class EdiGoodsDto
{
    public Guid BLId { get; set; }
    public long NumberOfPackages { get; set; }
    public string TypeOfPackages { get; set; } = string.Empty;
    public string PackageDescript { get; set; } = string.Empty;
    public string Descript { get; set; } = string.Empty;
    public string Blno { get; set; } = string.Empty;
    public string? OrderRegistrationNo { get; set; }
    public string? BLDate { get; set; }
    public string HsCode { get; set; } = string.Empty;
    public string? Consignee { get; set; }
    public string ConsigneeNationalCode { get; set; } = string.Empty;
    public string? SUPCONSIGNEEADDRESS { get; set; }
    public string? BirthDate { get; set; }
    public string? Shipper { get; set; }
    public string? Notify { get; set; }
    public string NetWeight { get; set; } = string.Empty;
}


public class EdiGoodsCntDto
{
    public Guid BLContainerId { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public double MeasureValue { get; set; }
    public long NumberOfPackages { get; set; }
}



public class EdiGoodsCntHsCodeDto
{
    public string CargoInfoHSCode { get; set; } = string.Empty;
    public double CargoInfoMeasureValue { get; set; }
    public long CargoInfoNumberOfPackages { get; set; }
}


public class EdiMcntDto
{
    public string MCntNumber { get; set; } = string.Empty;
    public string MCNTIsoCode { get; set; } = string.Empty;
    public string MCntFullEmpty { get; set; } = string.Empty; // F یا E
    public string MCntTransStatus { get; set; } = string.Empty;
    public string MCntFCLLCLType { get; set; } = string.Empty;


    // پورت‌ها
    public string MCNTLoadPort { get; set; } = string.Empty;
    public string MCNTDischargePort { get; set; } = string.Empty;
    public string MCNTDestination { get; set; } = string.Empty;

    public string MCntTaireWeight { get; set; } = string.Empty;
    public double MCntTransportDegree { get; set; }
    public string MCntSealNumber { get; set; } = string.Empty;
    public string MCntSealingPartyCode { get; set; } = string.Empty;
    public string MCntSealConditionCode { get; set; } = string.Empty;
    public string MCntHazardCode { get; set; } = string.Empty;
    public string MCNTUNDG { get; set; } = string.Empty;
    public string MCntOwner { get; set; } = string.Empty;
}