using Microsoft.AspNetCore.Mvc.Rendering;
using ShipRate.Application.DTOs;
using ShipRate.Domain.Entities.Contract;
using System.ComponentModel.DataAnnotations;

namespace ShipRate.Application.ViewModels
{
    public class FACContractSearchVm
    {
        public string? Search { get; set; }
        public FACDirection? Direction { get; set; }
        public FACContractStatus? Status { get; set; }
        public FACCounterpartyRole? CounterpartyRole { get; set; }

        public DateTime? ValidOn { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class FACContractListItemVm
    {
        public Guid Id { get; set; }

        public string ContractNo { get; set; } = string.Empty;
        public string FacContractTitle { get; set; } = string.Empty;

        public FACDirection Direction { get; set; }
        public FACCounterpartyRole CounterpartyRole { get; set; }
        public FACContractStatus Status { get; set; }

        public string CounterpartyName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public FACCalculationBasis? DefaultCalculationBasis { get; set; }
        public decimal? DefaultRate { get; set; }
        public string? CurrencyCode { get; set; }

        public int RateLineCount { get; set; }
    }
    public class FACContractIndexVm
    {
        public FACContractSearchVm Filter { get; set; } = new();

        public PagedResultDto<FACContractListItemVm> Result { get; set; }
            = new();
    }


    public class FACContractFormVm : IValidatableObject
    {
        public Guid? Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Display(Name = "Carrier")]
        public Guid? CarrierId { get; set; }

        [Display(Name = "Forwarder")]
        public Guid? ForwarderId { get; set; }
        public string? ForwarderText { get; set; }


        [Required]
        [Display(Name = "Direction")]
        public FACDirection Direction { get; set; } = FACDirection.Receivable;

        [Required]
        [Display(Name = "Counterparty")]
        public FACCounterpartyRole CounterpartyRole { get; set; }
            = FACCounterpartyRole.Carrier;

        [Required]
        [StringLength(50)]
        [Display(Name = "Contract No.")]
        public string ContractNo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Contract Title")]
        public string FacContractTitle { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

        [DataType(DataType.Date)]
        [Display(Name = "Signed Date")]
        public DateTime? SignedDate { get; set; }

        [Required]
        public FACContractStatus Status { get; set; } = FACContractStatus.Draft;

        [Display(Name = "Default Basis")]
        public FACCalculationBasis? DefaultCalculationBasis { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        [Display(Name = "Default Rate")]
        public decimal? DefaultRate { get; set; }

        [Display(Name = "Currency")]
        public int? CurrencyId { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        [Display(Name = "Minimum FAC")]
        public decimal? MinimumFACAmount { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        [Display(Name = "Maximum FAC")]
        public decimal? MaximumFACAmount { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "POL Free Days")]
        public int? POLFreeDays { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "POD Free Days")]
        public int? PODFreeDays { get; set; }

        [StringLength(1000)]
        [Display(Name = "Remarks")]
        public string? FacContractRemarks { get; set; }

        public string? RowVersion { get; set; }

        public Guid CreateUserId { get; set; }

        public List<FACContractRateLineFormVm> RateLines { get; set; } = new();

        public IEnumerable<SelectListItem> Companies { get; set; }
            = Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> Carriers { get; set; }
            = Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> Currencies { get; set; }
            = Enumerable.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate.Date < StartDate.Date)
            {
                yield return new ValidationResult(
                    "End Date must be greater than or equal to Start Date.",
                    new[] { nameof(EndDate) });
            }

            if (CounterpartyRole == FACCounterpartyRole.Carrier &&
                !CarrierId.HasValue)
            {
                yield return new ValidationResult(
                    "Carrier is required when Counterparty is Carrier.",
                    new[] { nameof(CarrierId) });
            }

            if (CounterpartyRole == FACCounterpartyRole.Forwarder &&
                !ForwarderId.HasValue)
            {
                yield return new ValidationResult(
                    "Forwarder is required when Counterparty is Forwarder.",
                    new[] { nameof(ForwarderId) });
            }

            if (DefaultCalculationBasis.HasValue != DefaultRate.HasValue)
            {
                yield return new ValidationResult(
                    "Default Basis and Default Rate must be entered together.",
                    new[]
                    {
                    nameof(DefaultCalculationBasis),
                    nameof(DefaultRate)
                    });
            }

            if (MinimumFACAmount.HasValue &&
                MaximumFACAmount.HasValue &&
                MinimumFACAmount > MaximumFACAmount)
            {
                yield return new ValidationResult(
                    "Minimum FAC cannot be greater than Maximum FAC.",
                    new[]
                    {
                    nameof(MinimumFACAmount),
                    nameof(MaximumFACAmount)
                    });
            }

            if (Status == FACContractStatus.Active &&
                !DefaultRate.HasValue &&
                !RateLines.Any(x => !x.IsDeleted && x.IsActive))
            {
                yield return new ValidationResult(
                    "An active contract must have a default rate or at least one active rate line.");
            }
        }
    }


    public class FACContractRateLineFormVm : IValidatableObject
    {
        public Guid? Id { get; set; }

        public Guid? POLId { get; set; }
        public string? POLText { get; set; }
        public Guid? PODId { get; set; }
        public string? PODText { get; set; }

        public int? TransportTypeId { get; set; }
        public int? LoadTypeId { get; set; }
        public int? ContainerSizeTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ValidFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ValidTo { get; set; }

        [Range(0, int.MaxValue)]
        public int Priority { get; set; } = 100;

        [Required]
        [Display(Name = "Basis")]
        public FACCalculationBasis CalculationBasis { get; set; }

        [Required]
        [Range(typeof(decimal), "0", "999999999999999")]
        public decimal Rate { get; set; }

        public int? CurrencyId { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        public decimal? MinimumFACAmount { get; set; }

        [Range(typeof(decimal), "0", "999999999999999")]
        public decimal? MaximumFACAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int? POLFreeDays { get; set; }

        [Range(0, int.MaxValue)]
        public int? PODFreeDays { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (ValidFrom.HasValue &&
                ValidTo.HasValue &&
                ValidTo.Value.Date < ValidFrom.Value.Date)
            {
                yield return new ValidationResult(
                    "Valid To must be greater than or equal to Valid From.",
                    new[] { nameof(ValidTo) });
            }

            if (MinimumFACAmount.HasValue &&
                MaximumFACAmount.HasValue &&
                MinimumFACAmount > MaximumFACAmount)
            {
                yield return new ValidationResult(
                    "Minimum FAC cannot be greater than Maximum FAC.",
                    new[]
                    {
                    nameof(MinimumFACAmount),
                    nameof(MaximumFACAmount)
                    });
            }
        }
    }
    public class Select2ItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class Select2ResultDto
    {
        public List<Select2ItemDto> Results { get; set; } = new();

        public Select2PaginationDto Pagination { get; set; } = new();
    }

    public class Select2PaginationDto
    {
        public bool More { get; set; }
    }

    public class JobFACContractVm
    {
        public Guid JobFACContractId { get; set; } // ID موجودیت واسط جهت حذف پیوند
        public Guid FACContractId { get; set; }

        public string ContractNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public FACDirection Direction { get; set; }
        public FACCounterpartyRole CounterpartyRole { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class JobFACSectionVm
    {
        public Guid JobId { get; set; }
        public List<JobFACContractVm> AttachedContracts { get; set; } = new();
        public List<FACContractOptionDto> AvailableContracts { get; set; } = new();
    }

    public class FACContractOptionDto
    {
        public Guid Id { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }

    public class AddJobFACContractDto
    {
        public Guid JobId { get; set; }
        public Guid FACContractId { get; set; }
    }

}