using System.Threading.Tasks;
using Device.Application.Alias;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Device.Application.Uom;
using Device.Application.Uom.Command;
using Device.Application.FileRequest.Command;
using AHI.Infrastructure.Authorization;
using Device.Application.Constant;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class UomsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UomsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.WRITE_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms", Privileges.Uom.Rights.WRITE_UOM)]
        public async Task<IActionResult> AddUomAsync([FromBody] AddUom addElement)
        {
            var validator = new AddUomValidation();
            var result = validator.Validate(addElement);
            if (!result.IsValid)
            {
                return BadRequest(result.Errors);
            }
            var response = await _mediator.Send(addElement);
            return Created($"/dev/uoms/{response.Id}", response);
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/{id}", Privileges.Uom.Rights.READ_UOM)]
        public async Task<IActionResult> GetEntityByIdAsync(int id)
        {

            var command = new GetUomById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpHead("{id}")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/{id}", Privileges.Uom.Rights.READ_UOM)]
        public async Task<IActionResult> CheckExistUomAsync(int id)
        {

            var command = new CheckExistUom(new int[] { id });
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("exist")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/exist", Privileges.Uom.Rights.READ_UOM)]
        public async Task<IActionResult> CheckExistUomAsync([FromBody] CheckExistUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.WRITE_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/{id}", Privileges.Uom.Rights.WRITE_UOM)]
        public async Task<IActionResult> UpdateUomAsync([FromRoute] int id, [FromBody] UpdateUom command)
        {
            command.Id = id;
            var validator = new UpdateUomValidation();
            var result = validator.Validate(command);
            if (!result.IsValid)
            {
                return BadRequest(result.Errors);
            }
            var response = await _mediator.Send(command);
            return Accepted($"/dev/uoms/{response.Id}", response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/search", Privileges.Uom.Rights.READ_UOM)]
        public async Task<IActionResult> SearchUomAsync([FromBody] GetUomByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.WRITE_UOM)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.READ_UOM)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.DELETE_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms", Privileges.Uom.Rights.DELETE_UOM)]
        public async Task<IActionResult> DeleteListUomAsync([FromBody] DeleteUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("import")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.WRITE_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/import", Privileges.Uom.Rights.WRITE_UOM)]
        public async Task<IActionResult> ImportUomAsync([FromBody] ImportFile command)
        {
            command.ObjectType = FileEntityConstants.UOM;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("export")]
        [RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Configuration.ENTITY_NAME, "dev/uoms/export", Privileges.Configuration.Rights.SHARE_CONFIGURATION)]
        public async Task<IActionResult> ExportUomAsync([FromBody] ExportUom command)
        {
            command.ObjectType = FileEntityConstants.UOM;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("calc")]
        [RightsAuthorizeFilter(Privileges.Uom.FullRights.WRITE_UOM)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Uom.ENTITY_NAME, "dev/uoms/calc", Privileges.Uom.Rights.WRITE_UOM)]
        public async Task<IActionResult> CalculationRefUomAsync([FromBody] CalculationRefUom command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(int id)
        {
            var command = new FetchUom(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
