using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Authorization;
using Device.Application.Asset.Command;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.FileRequest.Command;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class AssetTemplatesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AssetTemplatesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> AddAsync([FromBody] AddAssetTemplate addElement)
        {
            var response = await _mediator.Send(addElement);
            return Created($"/dev/assettemplates/{response.Id}", response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAssetTemplate command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted($"/dev/assettemplates/{response.Id}", response);
        }

        [HttpGet("{id}", Name = "GetAssetTemplateById")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
        public async Task<IActionResult> GetEntityByIdAsync([FromRoute] Guid id)
        {
            var command = new GetAssetTemplateById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetAssetTemplateByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveAssetTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyAssetTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveAssetTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("import")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> ImportAsync([FromBody] ImportFile command)
        {
            command.ObjectType = FileEntityConstants.ASSET_TEMPLATE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("export")]
        [RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
        public async Task<IActionResult> ExportAsync([FromBody] ExportAssetTemplate command)
        {
            command.ObjectType = FileEntityConstants.ASSET_TEMPLATE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.DELETE_ASSET_TEMPLATE)]
        public async Task<IActionResult> DeleteListAsync([FromBody] DeleteAssetTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("asset/{id}/create")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> CreateFromAssetAsync(Guid id)
        {
            var command = new CreateAssetTemplateFromAsset(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/attributes/generate/assembly")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateAssetTemplateAttributeAssemblyAsync(Guid id, [FromQuery] string token, [FromServices] ITokenService tokenService)
        {
            var tokenValid = await tokenService.CheckTokenAsync(token);
            if (!tokenValid)
            {
                return NotFound(new { IsSuccess = false, Message = id });
            }
            var command = new GenerateAssetTemplateAttributeAssembly(id);
            var response = await _mediator.Send(command);
            return File(response.Data, "application/octet-stream", response.Name);
        }

        [HttpHead("{id}")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE, Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> CheckExistingAssetTemplateAsync([FromRoute] Guid id)
        {
            var command = new CheckExistingAssetTemplate(new[] { id });
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("exist")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.READ_ASSET_TEMPLATE, Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> CheckExistingAssetTemplateAsync([FromBody] CheckExistingAssetTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/assets")]
        public async Task<IActionResult> GetAssetsByTemplateId(Guid id)
        {
            var command = new GetAssetsByTemplateId(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(Guid id)
        {
            var command = new FetchAssetTemplate(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/validate")]
        public async Task<IActionResult> ValidateAssetAttributesAsync([FromBody] ValidateAssetAttributeList command)
        {
            command.ValidationType = Application.Enum.ValidationType.AssetTemplate;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateAssetTemplateAsync(Guid id)
        {
            var command = new ValidateAssetTemplate(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/parse")]
        [RightsAuthorizeFilter(Privileges.AssetTemplate.FullRights.WRITE_ASSET_TEMPLATE)]
        public async Task<IActionResult> ParseAsync([FromBody] ParseAttributeTemplate command)
        {
            command.ObjectType = FileEntityConstants.ASSET_TEMPLATE_ATTRIBUTE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/export")]
        [RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
        public async Task<IActionResult> ExportAttributeAsync([FromBody] ExportAssetTemplateAttribute command)
        {
            command.ObjectType = FileEntityConstants.ASSET_TEMPLATE_ATTRIBUTE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}