using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShipRate.Application.DTOs;
using ShipRate.Application.Interfaces;
using ShipRate.Application.Services;
using ShipRate.Application.Services.BillOfLadings;
using ShipRate.Application.ViewModels;
using ShipRate.Domain.Entities.ServiceRequests;
using System.Threading.Tasks;

namespace ShipRate.WebAppCustomers.Controllers;

[Authorize]
public class ServiceRequestController : ApplicationController
{
    private readonly CustomerServiceRequestService _requestService;
    private readonly ICurrentUserService _currentUser;
    public ServiceRequestController(CustomerServiceRequestService requestService,ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
        _requestService = requestService;
    }

    public async Task<IActionResult> Index() {
        var currrentPerson = await _currentUser.GetPersonIdAsync();
        var data = await _requestService.GetIndexDataAsync(new ServiceRequestSearchDto {PartyId = currrentPerson.Value});
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> List(ServiceRequestSearchDto input)
    {
        var currrentPerson = await _currentUser.GetPersonIdAsync();
        input.PartyId = currrentPerson.Value;
        var data = await _requestService.GetIndexDataAsync(input);
        return PartialView("_List", data);
    }


    [HttpGet]
    public async Task<IActionResult> NewRequestMenu()
    {
        var requestTypes = await _requestService.GetRequestTypesAsync();
        return PartialView("_NewRequestMenu",requestTypes);
    }


   
    public  async Task<IActionResult> HandleNewRequest(int requestTypeId)
    {
        string partialViewName = "";
        object dto = null;
        switch (requestTypeId)
        {
            case 1:
                partialViewName = "_BLAmendment";
                dto = await _requestService.GetCreateBLAmendmentRequestFormforCustomerAsync(Guid.Empty);
                break;

            case 2:
                partialViewName = "_Consolidation";
                dto = await _requestService.GetCreateConsolidationAsync();
                break;

            case 3:
                dto = await _requestService.GetCreateSplitAsync();
                partialViewName = "_Split";
                break;

            case 4:
                partialViewName = "_DocumentCorrection";
                break;

            case 5:
                partialViewName = "_CustomsRelatedRequest";
                break;
            case 10:
                partialViewName = "_DepositCreditReturnDtlForm";
                dto = await GetDetailDto(Guid.Empty,requestTypeId);
                break;

            case 99:
                partialViewName = "_Other";
                break;

            default:
                break;
        }

        if (string.IsNullOrEmpty(partialViewName))
        {
            return NotFound();
        }

        var formDto = await _requestService.GetCreateFormForCustomerAsync(requestTypeId);
        return PartialView("_RequestForm", new ServiceRequestDto { 
            ServiceRequestFormDto = formDto,
            DetailDto = dto,
            PartialName = partialViewName
        });
    }


    public async Task<IActionResult> EditRequest(Guid id)
    {
        string partialViewName = "";
        object dto = null;
        var formDto = await _requestService.GetEditFormAsync(id);
        switch (formDto.Model.RequestTypeId)
        {
            case 1:
                partialViewName = "_BLAmendment";
                dto = await GetDetailDto(id, formDto.Model.RequestTypeId);
                break;

            case 2:
                partialViewName = "_Consolidation";
                dto = await _requestService.GetCreateConsolidationAsync();
                break;

            case 3:
                dto = await _requestService.GetCreateSplitAsync();
                partialViewName = "_Split";
                break;

            case 4:
                partialViewName = "_DocumentCorrection";
                break;

            case 5:
                partialViewName = "_CustomsRelatedRequest";
                break;

            case 10:
                partialViewName = GetViewName(10);
                dto = await GetDetailDto(id,formDto.Model.RequestTypeId);
                break;


            case 99:
                partialViewName = "_Other";
                break;

            default:
                break;
        }

        if (string.IsNullOrEmpty(partialViewName))
        {
            return NotFound();
        }
        return PartialView("_RequestForm", new ServiceRequestDto
        {
            ServiceRequestFormDto = formDto,
            DetailDto = dto,
            PartialName = partialViewName
        });
    }

    private async Task<object> GetDetailDto(Guid? masterId, int serviceRequestType)
    {
        object dto = null;
        switch (serviceRequestType)
        {
            case 1:
                if (masterId.HasValue)
                {
                    dto = await _requestService.GetEditBLAmendmentRequestFormByServiceRequestAsync(masterId.Value);
                }
                else
                {
                dto = await _requestService.GetCreateBLAmendmentRequestFormforCustomerAsync(Guid.Empty);
                    
                }
                break;

            case 2:
                dto = await _requestService.GetCreateConsolidationAsync();
                break;

            case 3:
                dto = await _requestService.GetCreateSplitAsync();
                break;

            case 4:
                break;

            case 5:
                break;

            case 10:
                if (masterId.HasValue && masterId != Guid.Empty)
                {
                    dto = await GetDepositCreditReturnDtl(masterId.Value);
                }
                else
                {
                    dto = new DepositCreditReturnDtl { SelectedDocuments = new List<SelectedPaymentDocumentVm>(), HeaderSaved = false };
                };
                break;

            case 99:
                break;

            default:
                break;
        }

        return dto;
    }


    private string GetViewName(int serviceRequestType)
    {
        string partialViewName = "";
        switch (serviceRequestType)
        {
            case 1:
                partialViewName = "_BLAmendment";
                break;

            case 2:
                partialViewName = "_Consolidation";
                break;

            case 3:
                partialViewName = "_Split";
                break;

            case 4:
                partialViewName = "_DocumentCorrection";
                break;

            case 5:
                partialViewName = "_CustomsRelatedRequest";
                break;

            case 10:
                partialViewName = "_DepositCreditReturnDtlForm";
                break;

            case 99:
                partialViewName = "_Other";
                break;

            default:
                break;
        }

        return partialViewName;
    }
    [HttpPost]
    public async Task<IActionResult> CreateMaster(ServiceRequestFormDto dto)
    {

        var currentParty = await _currentUser.GetPersonIdAsync();
        dto.Model.PartyId = currentParty;
        dto.Model.InOutId = 1;
        var id = await _requestService.CreateAsync(dto.Model);
        if (id == null || id == Guid.Empty)
        {
            return NotFound();
        }
        dto.Model.Id = id;
        HxTrigger(new { toastSuccess = "Saved Succefully" });
        var srDto = new ServiceRequestDto
        {
            ServiceRequestFormDto = dto,
            DetailDto = await GetDetailDto(id, dto.Model.RequestTypeId),
            PartialName = GetViewName(dto.Model.RequestTypeId)
        };
        return PartialView("_RequestForm", srDto);
    }

    [HttpGet]
    public async Task<IActionResult> AddAmendmentLine()
    {
        // دریافت دراپ‌داون نوع فیلدها از سرویس
        var fieldTypes = await _requestService.GetBLAmendmentFieldTypeSelectListAsync();

        var lineDto = new BLAmendmentRequestLineFormDto
        {
            FieldTypes = fieldTypes
        };
        return PartialView("_BLAmendmentRequestLine", lineDto);
    }

    [HttpPost]
    public async Task<IActionResult> SaveBLAmendment(BLAmendmentRequestFormDto dto)
    {
           var result = await _requestService.SaveBLAmendmentRequestForCustomerAsync(dto);
        if (!result.Succeeded)
        {
               AddModelStateErrors(result.Errors);
        }
        else
        {
            HxTrigger(new { toastSuccess = "Saved Successfully" , closeAppModal=true});
        }
        return PartialView("_BLAmendment",dto);
    }





    [HttpGet]
    public async Task<IActionResult> SubmitRequest(Guid id)
    {
        return PartialView("_SubmitRequest" ,id);
    }


    [HttpPost]
    public async Task<IActionResult> SubmitRequestApproved(Guid id)
    {
        var res = await _requestService.RegisterRequest(id);
        if (!res.Succeeded)
        {
            HxTrigger(new { toastError = string.Join(',', res.Errors) , closeAppModal = true });
        }
        else
        {
            HxTrigger(new { toastSuccess = "Submited Successfully!" ,closeAppModal=true , ServiceRequestChanged =true});
        }

        return Ok();
    }



    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepositCreditReturnReq()
    {


        var vm = new DepositCreditReturnDtl
        {
            HeaderSaved = false,
            SelectedDocuments = new List<SelectedPaymentDocumentVm>()
        };

        return PartialView("_DepositCreditReturnDtlForm", vm);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDepositCreditReturn(DepositCreditReturnDtl form)
    {
                
       try
        {
            if (form.Id != Guid.Empty)
            {
                await _requestService.SaveDepositCreditPaymentDocuments(form);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }


        form.HeaderSaved = true;

        return PartialView("_DepositCreditReturnDtlForm",await GetDepositCreditReturnDtl(form.Id));
    }


    public async Task<DepositCreditReturnDtl> GetDepositCreditReturnDtl(Guid masterId)
    {
        var currentParty = await _currentUser.GetPersonIdAsync();
        var selected = await _requestService.GetSelectedPaymentDocuments(masterId);
        var available = await _requestService.GetAvailablePaymentDocumentsForReturn(currentParty ?? Guid.Empty, masterId);
        MergeSelectedAndavailable(selected, available);
        return new DepositCreditReturnDtl
        {
            Id = masterId,
            SelectedDocuments = selected,
            HeaderSaved = true,
        };
    }

    private void MergeSelectedAndavailable(List<SelectedPaymentDocumentVm> selected, List<PaymentDocumentRowVm> available)
    {
        selected.AddRange(available.Select(x => new SelectedPaymentDocumentVm
        {
            PaymentDocumentId = x.PaymentDocumentId,
            ServiceRequestId = x.ServiceRequestId,
            DocumentNo = x.DocumentNo,
            Amount = x.Amount,
            AllocatedAmount = x.AllocatedAmount,
            IsSelected = false,
            PaymentMethod = x.PaymentMethod,
            PaymentMethodId = x.PaymentMethodId
        }));
    }

}
