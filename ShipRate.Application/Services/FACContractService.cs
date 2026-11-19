using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShipRate.Application.DTOs;
using ShipRate.Application.Interfaces;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.Contract;
using ShipRate.Domain.Entities.Operations;
using ShipRate.Infrastructure.Data;

namespace ShipRate.Application.Services
{
    public class FACContractService : IFACContractService
    {
        private readonly ApplicationDbContext _db;
        public FACContractService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResultDto<FACContractListItemVm>> GetPagedAsync(FACContractSearchVm filter, CancellationToken cancellationToken = default)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;

            filter.PageSize = filter.PageSize switch
            {
                < 5 => 20,
                > 100 => 100,
                _ => filter.PageSize
            };

            var query = _db.FACContracts
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();

                query = query.Where(x =>
                    x.ContractNo.Contains(search) ||
                    x.FacContractTitle.Contains(search) ||
                    (x.Carrier != null && x.Carrier.Person.PersonName.Contains(search)) ||
                    (x.Forwarder != null &&
                     (
                         x.Forwarder.FirstName.Contains(search) ||
                         x.Forwarder.LastName.Contains(search)
                     )));
            }

            if (filter.Direction.HasValue)
            {
                query = query.Where(x => x.Direction == filter.Direction);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status);
            }

            if (filter.CounterpartyRole.HasValue)
            {
                query = query.Where(
                    x => x.CounterpartyRole == filter.CounterpartyRole);
            }

            if (filter.ValidOn.HasValue)
            {
                var date = filter.ValidOn.Value.Date;

                query = query.Where(x =>
                    x.StartDate <= date &&
                    x.EndDate >= date);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.StartDate)
                .ThenBy(x => x.ContractNo)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new FACContractListItemVm
                {
                    Id = x.Id,
                    ContractNo = x.ContractNo,
                    FacContractTitle = x.FacContractTitle,
                    Direction = x.Direction,
                    CounterpartyRole = x.CounterpartyRole,
                    Status = x.Status,

                    CounterpartyName =
                        x.CounterpartyRole == FACCounterpartyRole.Carrier
                            ? (x.Carrier != null ? x.Carrier.Person.PersonName : "-")
                            : (x.Forwarder != null
                                ? (x.Forwarder.PersonName) : "-"),

                    StartDate = x.StartDate,
                    EndDate = x.EndDate,

                    DefaultCalculationBasis = x.DefaultCalculationBasis,
                    DefaultRate = x.DefaultRate,

                    CurrencyCode = x.Currency != null
                        ? x.Currency.CurrencyCode
                        : null,

                    RateLineCount = x.RateLines.Count()
                })
                .ToListAsync(cancellationToken);

            return new PagedResultDto<FACContractListItemVm>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
        }
        public async Task<FACContractFormVm> GetCreateModelAsync(CancellationToken cancellationToken = default)
        {
            var model = new FACContractFormVm
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                Status = FACContractStatus.Draft,
                Direction = FACDirection.Receivable,
                CounterpartyRole = FACCounterpartyRole.Carrier
            };

            await PopulateListsAsync(model, cancellationToken);

            return model;
        }



        public async Task<FACContractFormVm?> GetEditModelAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FACContracts
                .AsNoTracking()
                .Include(x => x.Forwarder)
                .Include(x => x.RateLines)
                    .ThenInclude(r => r.POL) // برای دریافت متن POL
                .Include(x => x.RateLines)
                    .ThenInclude(r => r.POD) // برای دریافت متن POD
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return null;

            var model = new FACContractFormVm
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                CarrierId = entity.CarrierId,
                ForwarderId = entity.ForwarderId,
                
                ForwarderText = entity.Forwarder?.PersonName ?? "",

                Direction = entity.Direction,
                CounterpartyRole = entity.CounterpartyRole,

                ContractNo = entity.ContractNo,
                FacContractTitle = entity.FacContractTitle,

                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                SignedDate = entity.SignedDate,

                Status = entity.Status,
                DefaultCalculationBasis = entity.DefaultCalculationBasis,
                DefaultRate = entity.DefaultRate,
                CurrencyId = entity.CurrencyId,

                MinimumFACAmount = entity.MinimumFACAmount,
                MaximumFACAmount = entity.MaximumFACAmount,

                POLFreeDays = entity.POLFreeDays,
                PODFreeDays = entity.PODFreeDays,

                FacContractRemarks = entity.FacContractRemarks,

                RowVersion = Convert.ToBase64String(entity.RowVersion),

                RateLines = entity.RateLines
                    .Where(x => !x.IsDeleted) // اختیاری: اگر Soft Delete دارید
                    .OrderBy(x => x.Priority)
                    .ThenBy(x => x.ValidFrom)
                    .Select(MapRateLine)
                    .ToList()
            };

            await PopulateListsAsync(model, cancellationToken);

            return model;
        }


        //public async Task<FACContractFormVm?> GetEditModelAsync(Guid id, CancellationToken cancellationToken = default)
        //{
        //    var entity = await _db.FACContracts
        //        .AsNoTracking()
        //        .Include(x => x.Forwarder)
        //        .Include(x => x.RateLines)
        //        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        //    if (entity == null)
        //        return null;

        //    var model = new FACContractFormVm
        //    {
        //        Id = entity.Id,
        //        CompanyId = entity.CompanyId,
        //        CarrierId = entity.CarrierId,
        //        ForwarderId = entity.ForwarderId,

        //        ForwarderText = entity.Forwarder == null
        //            ? null
        //            : $"{entity.Forwarder.FirstName} {entity.Forwarder.LastName}",

        //        Direction = entity.Direction,
        //        CounterpartyRole = entity.CounterpartyRole,

        //        ContractNo = entity.ContractNo,
        //        FacContractTitle = entity.FacContractTitle,

        //        StartDate = entity.StartDate,
        //        EndDate = entity.EndDate,
        //        SignedDate = entity.SignedDate,

        //        Status = entity.Status,
        //        DefaultCalculationBasis = entity.DefaultCalculationBasis,
        //        DefaultRate = entity.DefaultRate,
        //        CurrencyId = entity.CurrencyId,

        //        MinimumFACAmount = entity.MinimumFACAmount,
        //        MaximumFACAmount = entity.MaximumFACAmount,

        //        POLFreeDays = entity.POLFreeDays,
        //        PODFreeDays = entity.PODFreeDays,

        //        FacContractRemarks = entity.FacContractRemarks,

        //        RowVersion = Convert.ToBase64String(entity.RowVersion),

        //        RateLines = entity.RateLines
        //            .OrderBy(x => x.Priority)
        //            .ThenBy(x => x.ValidFrom)
        //            .Select(MapRateLine)
        //            .ToList()
        //    };

        //    await PopulateListsAsync(model, cancellationToken);

        //    return model;
        //}


        public Task<FACContractFormVm?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return GetEditModelAsync(id, cancellationToken);
        }
        public async Task<Guid> CreateAsync(FACContractFormVm model, CancellationToken cancellationToken = default)
        {
            await ValidateBusinessRulesAsync(model, currentId: null, cancellationToken);

            var entity = new FACContract
            {
                Id = Guid.NewGuid(),

                CompanyId = model.CompanyId,
                CarrierId = model.CarrierId,
                ForwarderId = model.ForwarderId,

                Direction = model.Direction,
                CounterpartyRole = model.CounterpartyRole,

                ContractNo = model.ContractNo.Trim(),
                FacContractTitle = model.FacContractTitle.Trim(),

                StartDate = model.StartDate.Date,
                EndDate = model.EndDate.Date,
                SignedDate = model.SignedDate?.Date,

                Status = model.Status,
                DefaultCalculationBasis = model.DefaultCalculationBasis,
                DefaultRate = model.DefaultRate,
                CurrencyId = model.CurrencyId,

                MinimumFACAmount = model.MinimumFACAmount,
                MaximumFACAmount = model.MaximumFACAmount,

                POLFreeDays = model.POLFreeDays,
                PODFreeDays = model.PODFreeDays,

                FacContractRemarks =
                    model.FacContractRemarks?.Trim(),
                //CreateUserId = model.CreateUserId


            };

            foreach (var lineModel in model.RateLines.Where(x => !x.IsDeleted))
            {
                entity.RateLines.Add(CreateRateLine(lineModel));
            }

            _db.FACContracts.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return entity.Id;
        }
        public async Task UpdateAsync(FACContractFormVm model, CancellationToken cancellationToken = default)
        {
            if (!model.Id.HasValue)
                throw new InvalidOperationException("Contract Id is required.");

            await ValidateBusinessRulesAsync(
                model,
                model.Id,
                cancellationToken);

            var entity = await _db.FACContracts
                .Include(x => x.RateLines)
                .FirstOrDefaultAsync(
                    x => x.Id == model.Id.Value,
                    cancellationToken);

            if (entity == null)
                throw new KeyNotFoundException("FAC contract was not found.");

            if (string.IsNullOrWhiteSpace(model.RowVersion))
            {
                throw new InvalidOperationException(
                    "RowVersion is required.");
            }

            var rowVersion = Convert.FromBase64String(model.RowVersion);

            _db.Entry(entity)
                .Property(x => x.RowVersion)
                .OriginalValue = rowVersion;

            entity.CompanyId = model.CompanyId;
            entity.CarrierId = model.CarrierId;
            entity.ForwarderId = model.ForwarderId;

            entity.Direction = model.Direction;
            entity.CounterpartyRole = model.CounterpartyRole;

            entity.ContractNo = model.ContractNo.Trim();
            entity.FacContractTitle = model.FacContractTitle.Trim();

            entity.StartDate = model.StartDate.Date;
            entity.EndDate = model.EndDate.Date;
            entity.SignedDate = model.SignedDate?.Date;

            entity.Status = model.Status;

            entity.DefaultCalculationBasis =
                model.DefaultCalculationBasis;

            entity.DefaultRate = model.DefaultRate;
            entity.CurrencyId = model.CurrencyId;

            entity.MinimumFACAmount = model.MinimumFACAmount;
            entity.MaximumFACAmount = model.MaximumFACAmount;

            entity.POLFreeDays = model.POLFreeDays;
            entity.PODFreeDays = model.PODFreeDays;

            entity.FacContractRemarks =
                model.FacContractRemarks?.Trim();

            SyncRateLines(entity, model.RateLines);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new FACConcurrencyException(
                    "This contract was changed by another user. " +
                    "Close the form, reopen it and try again.");
            }
        }
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FACContracts
                .Include(x => x.RateLines)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (entity == null)
                return;

            // اگر BusinessEntity دارای Soft Delete است:
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.Now;

            foreach (var line in entity.RateLines)
            {
                line.IsDeleted = true;
                line.DeletedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        public Task<FACContractRateLineFormVm> GetEmptyRateLineAsync(CancellationToken cancellationToken = default)
        {
            var model = new FACContractRateLineFormVm
            {
                Id = null,
                Priority = 100,
                IsActive = true,
                CalculationBasis = FACCalculationBasis.PercentOfFreight
            };

            return Task.FromResult(model);
        }
        public async Task<Select2ResultDto> SearchForwardersAsync(string? term, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = Math.Clamp(pageSize, 10, 50);

            var query = _db.Persons
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var value = term.Trim();

                query = query.Where(x =>
                    x.FirstName.Contains(value) ||
                    x.LastName.Contains(value) ||
                    (x.FirstName + " " + x.LastName).Contains(value));
            }

            var data = await query
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize + 1)
                .Select(x => new Select2ItemDto
                {
                    Id = x.Id.ToString(),
                    Text = x.FirstName + " " + x.LastName
                })
                .ToListAsync(cancellationToken);

            var hasMore = data.Count > pageSize;

            if (hasMore)
                data.RemoveAt(data.Count - 1);

            return new Select2ResultDto
            {
                Results = data,
                Pagination = new Select2PaginationDto
                {
                    More = hasMore
                }
            };
        }
        private async Task PopulateListsAsync(FACContractFormVm model, CancellationToken cancellationToken)
        {
            model.Companies = await _db.Companies
                .AsNoTracking()
                .OrderBy(x => x.CompanyAbb)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.CompanyAbb
                })
                .ToListAsync(cancellationToken);

            model.Carriers = await _db.Carriers
                .AsNoTracking()
                .OrderBy(x => x.Person.PersonName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Person.PersonName
                })
                .ToListAsync(cancellationToken);

            model.Currencies = await _db.Currencies.Where(c => c.IsActive == true)
                .AsNoTracking()
                .OrderBy(x => x.CurrencyCode)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.CurrencyCode
                })
                .ToListAsync(cancellationToken);
        }
        private async Task ValidateBusinessRulesAsync(FACContractFormVm model, Guid? currentId, CancellationToken cancellationToken)
        {
            var contractNo = model.ContractNo.Trim();

            var duplicateExists = await _db.FACContracts
                .AsNoTracking()
                .AnyAsync(x =>
                        x.CompanyId == model.CompanyId &&
                        x.ContractNo == contractNo &&
                        (!currentId.HasValue || x.Id != currentId.Value),
                    cancellationToken);

            if (duplicateExists)
            {
                throw new FACValidationException(
                    "A contract with the same Contract No. already exists.");
            }

            if (model.CounterpartyRole ==
                    FACCounterpartyRole.Carrier &&
                !model.CarrierId.HasValue)
            {
                throw new FACValidationException(
                    "Carrier is required.");
            }

            if (model.CounterpartyRole ==
                    FACCounterpartyRole.Forwarder &&
                !model.ForwarderId.HasValue)
            {
                throw new FACValidationException(
                    "Forwarder is required.");
            }

            if (model.EndDate.Date < model.StartDate.Date)
            {
                throw new FACValidationException(
                    "End Date cannot be earlier than Start Date.");
            }

            ValidateRateLineAmbiguity(model.RateLines);
        }
        private static void ValidateRateLineAmbiguity(IEnumerable<FACContractRateLineFormVm> lines)
        {
            var activeLines = lines
                .Where(x => !x.IsDeleted && x.IsActive)
                .ToList();

            var duplicates = activeLines
                .GroupBy(x => new
                {
                    x.POLId,
                    x.PODId,
                    x.TransportTypeId,
                    x.LoadTypeId,
                    x.ContainerSizeTypeId,
                    x.ValidFrom,
                    x.ValidTo,
                    x.Priority
                })
                .FirstOrDefault(x => x.Count() > 1);

            if (duplicates != null)
            {
                throw new FACValidationException(
                    "Duplicate active rate lines with the same criteria " +
                    "and priority are not allowed.");
            }
        }
        private static FACContractRateLineFormVm MapRateLine(FACContractRateLine entity)
        {
            return new FACContractRateLineFormVm
            {
                Id = entity.Id,
                POLId = entity.POLId,
                PODId = entity.PODId,
                PODText = entity.POD.LocationName,
                POLText = entity.POL.LocationName,
                TransportTypeId = entity.TransportTypeId,
                LoadTypeId = entity.LoadTypeId,
                ContainerSizeTypeId = entity.ContainerSizeTypeId,

                ValidFrom = entity.ValidFrom,
                ValidTo = entity.ValidTo,

                Priority = entity.Priority,
                CalculationBasis = entity.CalculationBasis,
                Rate = entity.Rate,
                CurrencyId = entity.CurrencyId,

                MinimumFACAmount = entity.MinimumFACAmount,
                MaximumFACAmount = entity.MaximumFACAmount,

                POLFreeDays = entity.POLFreeDays,
                PODFreeDays = entity.PODFreeDays,

                IsActive = entity.IsActive,
                Remarks = entity.Remarks
            };
        }
        private static FACContractRateLine CreateRateLine(FACContractRateLineFormVm model)
        {
            return new FACContractRateLine
            {
                Id = model.Id ?? Guid.NewGuid(),

                POLId = model.POLId,
                PODId = model.PODId,
                TransportTypeId = model.TransportTypeId,
                LoadTypeId = model.LoadTypeId,
                ContainerSizeTypeId = model.ContainerSizeTypeId,

                ValidFrom = model.ValidFrom?.Date,
                ValidTo = model.ValidTo?.Date,

                Priority = model.Priority,
                CalculationBasis = model.CalculationBasis,
                Rate = model.Rate,
                CurrencyId = model.CurrencyId,

                MinimumFACAmount = model.MinimumFACAmount,
                MaximumFACAmount = model.MaximumFACAmount,

                POLFreeDays = model.POLFreeDays,
                PODFreeDays = model.PODFreeDays,

                IsActive = model.IsActive,
                Remarks = model.Remarks?.Trim()
            };
        }
        private void SyncRateLines(FACContract contract, IReadOnlyCollection<FACContractRateLineFormVm> models)
        {
            var submittedIds = models
                .Where(x => x.Id.HasValue && !x.IsDeleted)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            foreach (var existing in contract.RateLines.ToList())
            {
                var model = models.FirstOrDefault(x => x.Id == existing.Id);

                if (model == null || model.IsDeleted)
                {
                    // در صورت وجود Soft Delete روی BusinessEntity:
                    existing.IsDeleted = true;
                    existing.DeletedAt = DateTime.Now;
                    continue;
                }

                existing.POLId = model.POLId;
                existing.PODId = model.PODId;

                existing.TransportTypeId = model.TransportTypeId;
                existing.LoadTypeId = model.LoadTypeId;
                existing.ContainerSizeTypeId = model.ContainerSizeTypeId;

                existing.ValidFrom = model.ValidFrom?.Date;
                existing.ValidTo = model.ValidTo?.Date;

                existing.Priority = model.Priority;
                existing.CalculationBasis = model.CalculationBasis;
                existing.Rate = model.Rate;
                existing.CurrencyId = model.CurrencyId;

                existing.MinimumFACAmount = model.MinimumFACAmount;
                existing.MaximumFACAmount = model.MaximumFACAmount;

                existing.POLFreeDays = model.POLFreeDays;
                existing.PODFreeDays = model.PODFreeDays;

                existing.IsActive = model.IsActive;
                existing.Remarks = model.Remarks?.Trim();
            }

            foreach (var model in models.Where(x =>
                         !x.Id.HasValue &&
                         !x.IsDeleted))
            {
                contract.RateLines.Add(CreateRateLine(model));
            }
        }
        public async Task<JobFACSectionVm> GetJobFACSectionAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            // ۱. استخراج شناسه تمام Partyهای این Job
            var jobPartyIds = await _db.JobParties
                .AsNoTracking()
                .Where(x => x.JobId == jobId && !x.IsDeleted)
                .Select(x => x.PartyId)
                .ToListAsync(cancellationToken);

            // ۲. استخراج قراردادهای از قبل وصل شده به این Job
            var attachedContracts = await _db.JobFACContracts
                .AsNoTracking()
                .Where(x => x.JobId == jobId && !x.IsDeleted)
                .Select(x => new JobFACContractVm
                {
                    JobFACContractId = x.Id,
                    FACContractId = x.FACContractId,
                    ContractNo = x.FACContract.ContractNo,
                    Title = x.FACContract.FacContractTitle,
                    Direction = x.FACContract.Direction,
                    CounterpartyRole = x.FACContract.CounterpartyRole,
                    CounterpartyName = x.FACContract.CounterpartyRole == FACCounterpartyRole.Carrier
                        ? (x.FACContract.Carrier != null ? x.FACContract.Carrier.Person.PersonName : "-")
                        : (x.FACContract.Forwarder != null ? $"{x.FACContract.Forwarder.FirstName} {x.FACContract.Forwarder.LastName}" : "-"),
                    StartDate = x.FACContract.StartDate,
                    EndDate = x.FACContract.EndDate
                })
                .ToListAsync(cancellationToken);

            var attachedContractIds = attachedContracts.Select(x => x.FACContractId).ToList();

            // ۳. فیلتر قراردادهای فعال و منطبق با JobParties که هنوز متصل نشده‌اند
            var availableContracts = await _db.FACContracts
                .AsNoTracking()
                .Where(x => x.Status == FACContractStatus.Active && !x.IsDeleted)
                .Where(x => !attachedContractIds.Contains(x.Id))
                .Where(x => (x.CounterpartyRole == FACCounterpartyRole.Carrier && x.CarrierId.HasValue && jobPartyIds.Contains(x.CarrierId.Value))
                         || (x.CounterpartyRole == FACCounterpartyRole.Forwarder && x.ForwarderId.HasValue && jobPartyIds.Contains(x.ForwarderId.Value)))
                .Select(x => new FACContractOptionDto
                {
                    Id = x.Id,
                    DisplayText = $"{x.ContractNo} - {x.FacContractTitle} ({(x.Direction == FACDirection.Receivable ? "Rec" : "Pay")})"
                })
                .ToListAsync(cancellationToken);

            return new JobFACSectionVm
            {
                JobId = jobId,
                AttachedContracts = attachedContracts,
                AvailableContracts = availableContracts
            };
        }
        public async Task AddFACContractToJobAsync(Guid jobId, Guid facContractId, CancellationToken cancellationToken = default)
        {
            // ۱. بررسی تکراری نبودن اتصال
            var exists = await _db.JobFACContracts
                .AnyAsync(x => x.JobId == jobId && x.FACContractId == facContractId && !x.IsDeleted, cancellationToken);

            if (exists)
                throw new FACValidationException("This contract is already attached to the Job.");

            // ۲. اعتبارسنجی مجدد سمت سرور
            var contract = await _db.FACContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == facContractId && x.Status == FACContractStatus.Active && !x.IsDeleted, cancellationToken);

            if (contract == null)
                throw new FACValidationException("Selected FAC Contract is not active or does not exist.");

            var jobPartyIds = await _db.JobParties
                .AsNoTracking()
                .Where(x => x.JobId == jobId && !x.IsDeleted)
                .Select(x => x.PartyId)
                .ToListAsync(cancellationToken);

            bool isValidParty = (contract.CounterpartyRole == FACCounterpartyRole.Carrier && contract.CarrierId.HasValue && jobPartyIds.Contains(contract.CarrierId.Value))
                             || (contract.CounterpartyRole == FACCounterpartyRole.Forwarder && contract.ForwarderId.HasValue && jobPartyIds.Contains(contract.ForwarderId.Value));

            if (!isValidParty)
                throw new FACValidationException("The counterparty of this contract is not present in the Job's parties.");

            // ۳. ثبت اتصال جدید
            var entity = new JobFACContract
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                FACContractId = facContractId
            };

            _db.JobFACContracts.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        public async Task RemoveFACContractFromJobAsync(Guid jobFACContractId, CancellationToken cancellationToken = default)
        {
            var entity = await _db.JobFACContracts
                .FirstOrDefaultAsync(x => x.Id == jobFACContractId, cancellationToken);

            if (entity == null)
                return;

            // Soft Delete پیوند
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);
        }
        //--------
        public async Task ChangeFACStatusAsync(Guid id, int flag, CancellationToken cancellationToken = default)
        {
            var entity = await _db.FACContracts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return;

            // تبدیل int به نوع enum
            entity.Status = (FACContractStatus)flag;

            await _db.SaveChangesAsync(cancellationToken);
        }

    }

    public class FACValidationException : Exception
    {
        public FACValidationException(string message)
            : base(message)
        {
        }
    }

    public class FACConcurrencyException : Exception
    {
        public FACConcurrencyException(string message)
            : base(message)
        {
        }
    }
}