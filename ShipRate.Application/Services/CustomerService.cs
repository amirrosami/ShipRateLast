using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs.Persons;
using ShipRate.Application.Interfaces;
using ShipRate.Infrastructure.Data;

namespace ShipRate.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _db;

        public CustomerService(ApplicationDbContext ctx)
        {
            _db = ctx;
        }

        public async Task<List<CustomerSelect2Dto>> SearchCustomersAsync(string term, int take = 20)
        {
            term = term?.Trim() ?? "";
            var q = _db.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.Person });

            if (!string.IsNullOrEmpty(term))
            {
                q = q.Where(x =>
                    (x.Person.PersonName ?? "").Contains(term) ||
                    (x.Person.EcoCode ?? "").Contains(term) ||
                    (x.Person.RegNo ?? "").Contains(term) ||
                    (x.Person.NationalID ?? "").Contains(term));
            }

            var list = await q
                .OrderBy(x => x.Person.PersonName)
                .Take(take)
                .ToListAsync();

            return list.Select(x => new CustomerSelect2Dto
            {
                Id = x.Id,
                Text = $"{x.Person.PersonName}"
            }).ToList();
        }

        public async Task<CustomerProfileDto?> GetCustomerProfileAsync(Guid customerId)
        {
            var c = await _db.Customers
                .AsNoTracking()
                .Where(c => c.Id == customerId && !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Remarks,
                    c.SalesUserId,
                    c.IndustryId,
                    Person = c.Person,
                    SalesUser = c.SalesUser,
                    Industry = c.Industry,

                })
                .FirstOrDefaultAsync();

            if (c == null) return null;

            var countryName = c.Person.Country != null ? c.Person.Country.CountryName : "";
            var cityName = c.Person.City != null ? c.Person.City.LocationName : "";
            var personalityTypeTitle = c.Person.PersonalityType != null ? c.Person.PersonalityType.PersonalityTypeName : "";

            return new CustomerProfileDto
            {
                CustomerId = c.Id,
                PersonId = c.Id,
                Remarks = c.Remarks,

                CustomerCode = c.Person.NationalID, // اگر BusinessEntity.Code دارید
                CommercialName = c.Person.PersonName,
                PersianName = c.Person.PersonPersianName,
                EcoCode = c.Person.EcoCode,
                RegNo = c.Person.RegNo,
                NationalId = c.Person.NationalID,

                PrimaryContactName = c.Person.MainContactName ?? "",
                PrimaryContactEmail = c.Person.MainContactEmail ?? "",
                PrimaryContactPhone = c.Person.MainContactPhone ?? "",

                EmailAddress = c.Person.EmailAddress,
                Phone = c.Person.Phone,
                Mobile = c.Person.Mobile,
                FaxNo = c.Person.FaxNo,

                CountryId = c.Person.CountryId,
                CountryName = countryName,

                CityId = c.Person.CityId,
                CityName = cityName,
                Address = c.Person.Address,
                PersianAddress = c.Person.PersianAddress ?? "",

                PersonRegDate = c.Person.PersonRegDate,

                SalesUserId = c.SalesUserId,
                SalesUserFullName = c.SalesUser != null ? c.SalesUser.FullName : "",
                IndustryId = c.IndustryId,
                IndustryName = c.Industry != null ? c.Industry.IndustryName : "",

                PersonalityTypeTitle = personalityTypeTitle
            };
        }


        public async Task<CustomerFinanceSummaryDto> GetCustomerFinanceSummaryAsync(Guid customerPartyId, CancellationToken ct = default)
        {
            // دریافت اسناد مالی مرتبط با این مشتری (PartyId یا ThirdPartyId)
            var query = _db.PaymentDocuments
                .AsNoTracking()
                .Where(x => !x.IsDeleted && (x.PartyId == customerPartyId || x.ThirdPartyId == customerPartyId))
                .Include(x => x.Currency)
                .Include(x => x.PaymentMethod)
                .Include(x => x.FinanceDirection)
                .Include(x => x.BankAccount);

            var docs = await query
                .OrderByDescending(x => x.DocumentDate)
                .Take(10) // ۵۰ سند اخیر
                .ToListAsync(ct);

            var summary = new CustomerFinanceSummaryDto
            {
                CustomerPartyId = customerPartyId,
                // جهت‌های مالی: ۱ = دریافتی (Receipt/Inflow)، ۲ = پرداختی (Payment/Outflow)
                TotalReceivedAmount = docs.Where(d => d.FinanceDirectionId == 1 && d.PartyId == customerPartyId).Sum(d => d.Amount),
                TotalPaidAmount = docs.Where(d => d.FinanceDirectionId == 2 && d.PartyId == customerPartyId).Sum(d => d.Amount),
                TotalUnallocatedReceipts = docs.Where(d => d.FinanceDirectionId == 1 && d.PartyId == customerPartyId).Sum(d => d.Amount - d.AllocatedAmount),
                TotalAdvancePayments = docs.Where(d => d.IsAdvancePayment && d.PartyId == customerPartyId).Sum(d => d.Amount),
                RecentDocuments = docs.Select(d => new CustomerPaymentDocRowDto
                {
                    Id = d.Id,
                    DocumentNo = d.DocumentNo,
                    DocumentDate = d.DocumentDate,
                    EffectiveDate = d.EffectiveDate,
                    FinanceDirectionId = d.FinanceDirectionId,
                    FinanceDirectionTitle = d.FinanceDirection?.FinanceDirectionName ?? (d.FinanceDirectionId == 1 ? "Inflow" : "Outflow"),
                    PaymentMethodTitle = d.PaymentMethod?.PaymentMethodName ?? "-",
                    Amount = d.Amount,
                    AllocatedAmount = d.AllocatedAmount,
                    CurrencyCode = d.Currency?.CurrencyCode ?? "IRR",
                    BankAccountTitle = d.BankAccount != null ? $"{d.BankAccount.BankAccountTitle} - {d.BankAccount.AccountNumber}" : "-",
                    ReferenceNo = d.ReferenceNo,
                    TrackingNo = d.TrackingNo,
                    IsAdvancePayment = d.IsAdvancePayment,
                    IsThirdPartySettlement = d.IsThirdPartySettlement
                }).ToList()
            };

            return summary;
        }


        public async Task<CustomerJobsSummaryDto> GetCustomerJobsSummaryAsync(Guid customerPartyId, CancellationToken ct = default)
        {
            var customerRoleIds = new[] { 2, 31 }; // 2: Customer, 31: Booker Client

            // واکشی JobIdهایی که این مشتری در آنها نقش دارد
            var partyAssignments = await _db.JobParties
                .AsNoTracking()
                .Where(jp => !jp.IsDeleted && jp.PartyId == customerPartyId && customerRoleIds.Contains(jp.RoleTypeId))
                .Select(jp => new { jp.JobId, jp.RoleTypeId })
                .ToListAsync(ct);

            var jobIds = partyAssignments.Select(x => x.JobId).Distinct().ToList();

            if (!jobIds.Any())
            {
                return new CustomerJobsSummaryDto { CustomerPartyId = customerPartyId };
            }

            // واکشی کل رکوردهای مربوط به این Jobها
            var jobsQuery = _db.Jobs
                .AsNoTracking()
                .Where(j => !j.IsDeleted && jobIds.Contains(j.Id))
                .Include(j => j.JobStatus)
                .Include(j => j.TransportType)
                .Include(j => j.LoadType)
                .Include(j => j.Incoterm)
                .Include(j => j.SalesPerson)
                .Include(j => j.Routing.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.OriginLocation)
                .Include(j => j.Routing.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.DestinationLocation);

            var allJobs = await jobsQuery.ToListAsync(ct);

            // محاسبه شاخص‌ها (فرض وضعیت‌های نهایی مثل Completed/Closed بر اساس JobStatusId یا ClosedDate)
            var summary = new CustomerJobsSummaryDto
            {
                CustomerPartyId = customerPartyId,
                TotalJobsCount = allJobs.Count,
                ActiveJobsCount = allJobs.Count(j => j.ClosedDate == null && j.JobStatusId != 99 /* مثال: Cancelled */),
                CompletedJobsCount = allJobs.Count(j => j.ClosedDate != null),
                TotalTeuOrContainers = allJobs.Sum(j => j.ContainerCount ?? 0),
                TotalGrossWeightKg = allJobs.Sum(j => j.GrossWeightKg ?? 0),

                RecentJobs = allJobs
                    .OrderByDescending(j => j.JobDate ?? j.CreatedAt)
                    .Take(10)
                    .Select(j =>
                    {
                        var role = partyAssignments.FirstOrDefault(p => p.JobId == j.Id);
                        var roleName = role?.RoleTypeId == 31 ? "Booker" : "Customer";
                        var mainRouting = j.Routing.OrderBy(r => r.CreatedAt).FirstOrDefault();

                        return new CustomerJobRowDto
                        {
                            JobId = j.Id,
                            JobFileNumber = j.JobFileNumber,
                            JobNumber = j.JobNumber,
                            JobDate = j.JobDate,
                            JobStatusId = j.JobStatusId,
                            JobStatusName = j.JobStatus?.JobStatusName ?? "Open",
                            TransportTypeName = j.TransportType?.TransportTypeName ?? "-",
                            LoadTypeName = j.LoadType?.LoadTypeCode ?? "-",
                            IncotermCode = j.Incoterm?.IncotermCode ?? "-",
                            CustomerRoleName = roleName,
                            OriginName = mainRouting?.OriginLocation?.LocationName ?? mainRouting?.PlaceOfReceiptName ?? "-",
                            DestinationName = mainRouting?.DestinationLocation?.LocationName ?? mainRouting?.PlaceOfDeliveryName ?? "-",
                            ETD = mainRouting?.ETD,
                            ETA = mainRouting?.ETA,
                            ContainerCount = j.ContainerCount ?? 0,
                            GrossWeightKg = j.GrossWeightKg,
                            VolumeCBM = j.VolumeCBM,
                            SalesPersonName = j.SalesPerson != null ? $"{j.SalesPerson.FirstName} {j.SalesPerson.LastName}".Trim() : "-"
                        };
                    }).ToList()
            };
            return summary;
        }


        public async Task<CustomerPerformanceDto> GetCustomerPerformanceTrendAsync(CustomerPerformanceFilterDto filter)
        {
            DateTime now = DateTime.Now;
            DateTime fromDate;
            DateTime toDate = filter.ToDate ?? now;

            // تعیین بازه تاریخی بر اساس Preset
            switch (filter.Preset)
            {
                case DateRangePreset.OneMonth:
                    fromDate = now.AddMonths(-1);
                    break;
                case DateRangePreset.ThreeMonths:
                    fromDate = now.AddMonths(-3);
                    break;
                case DateRangePreset.SixMonths:
                    fromDate = now.AddMonths(-6);
                    break;
                case DateRangePreset.OneYear:
                    fromDate = now.AddYears(-1);
                    break;
                case DateRangePreset.Custom:
                    fromDate = filter.FromDate ?? now.AddMonths(-6);
                    break;
                case DateRangePreset.All:
                default:
                    fromDate = DateTime.MinValue;
                    break;
            }

            var result = new CustomerPerformanceDto
            {
                CustomerPartyId = filter.CustomerPartyId,
                Preset = filter.Preset,
                FromDate = fromDate,
                ToDate = toDate
            };

            // کوئری روی پرونده‌های مشتری (RoleType 2 = Customer, 31 = Booker Client)
            var jobsQuery = _db.Jobs
                .AsNoTracking()
                .Where(j => _db.JobParties.Any(jp =>
                    jp.JobId == j.Id &&
                    jp.PartyId == filter.CustomerPartyId &&
                    (jp.RoleTypeId == 2 || jp.RoleTypeId == 31)));

            if (fromDate != DateTime.MinValue)
            {
                jobsQuery = jobsQuery.Where(j => j.JobDate >= fromDate && j.JobDate <= toDate);
            }

            var jobsList = await jobsQuery
                .Select(j => new
                {
                    j.Id,
                    j.JobDate,
                    j.TransportTypeId,
                    ContainersCount = _db.RateRequestContainers.Count(c => c.JobId == j.Id)
                })
                .ToListAsync();

            result.TotalJobsCount = jobsList.Count;
            result.TotalTeuCount = jobsList.Sum(x => x.ContainersCount);

            // سهم شیوه‌های حمل
            result.TransportTypeDistribution = jobsList
                .GroupBy(x => x.TransportTypeId)
                .ToDictionary(
                    g => GetTransportTypeName(g.Key),
                    g => g.Count()
                );

            // دسته‌بندی و روند زمانی (Trend Points)
            if (jobsList.Any())
            {
                result.MonthlyTrends = jobsList
                    .Where(x => x.JobDate.HasValue)
                    .GroupBy(x => new { x.JobDate!.Value.Year, x.JobDate!.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new MonthlyTrendPointDto
                    {
                        PeriodLabel = $"{g.Key.Year}/{g.Key.Month:00}",
                        JobsCount = g.Count(),
                        TeuCount = g.Sum(c => c.ContainersCount)
                    })
                    .ToList();
            }

            return result;
        }

        private static string GetTransportTypeName(int typeId) => typeId switch
        {
            1 => "دریایی (Sea)",
            2 => "جاده‌ای (Road)",
            3 => "ریلی (Rail)",
            4 => "هوایی (Air)",
            _ => "MultiModal(چندوجهی)"
        };
    }
}
