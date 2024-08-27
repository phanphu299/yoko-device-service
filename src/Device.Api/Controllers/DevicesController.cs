using System.Threading.Tasks;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.Asset.Command;
using Device.Application.FileRequest.Command;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Device.Application.Service.Abstraction;
using Device.Application.Constant;
using AHI.Infrastructure.Authorization;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class DevicesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DevicesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> AddAsync([FromBody] AddDevice addEntity)
        {
            var response = await _mediator.Send(addEntity);
            return Created(HttpUtility.UrlEncode($"/dev/devices/{response.Id}"), response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/{id}", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateDevice command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted(HttpUtility.UrlEncode($"/dev/devices/{response.Id}"), response);
        }

        [HttpPut("token/refresh")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshToken command)
        {
            var response = await _mediator.Send(command);
            return Accepted(HttpUtility.UrlEncode($"/dev/devices/{response.Id}"), response);
        }

        [HttpPut]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> UpdateAsync([FromBody] UpdateDevice command)
        {
            var response = await _mediator.Send(command);
            return Accepted(HttpUtility.UrlEncode($"/dev/devices/{response.Id}"), response);
        }

        [HttpPatch]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> PartialUpdateAsync([FromBody] PatchDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/{id}", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> GetEntityByIdAsync(string id)
        {

            var command = new GetDeviceById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("details")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/details", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> GetEntityByIdAsync([FromBody] GetDeviceById command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/search", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetDeviceByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("healthcheckmethod/search")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE, Privileges.Asset.FullRights.WRITE_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/devices/healthcheckmethod/search",
        //Privileges.Device.FullRights.READ_DEVICE,
        //    Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> SearchHealthCheckMethodsAsync([FromBody] GetHealthCheckMethodByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.DELETE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/{id}", Privileges.Device.Rights.DELETE_DEVICE)]
        public async Task<IActionResult> DeleteForceAsync(string id)
        {
            var command = new DeleteDevice() { DeviceIds = new[] { id } };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.DELETE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices", Privileges.Device.Rights.DELETE_DEVICE)]
        public async Task<IActionResult> DeleteForceListAsync([FromBody] DeleteDevice command)
        {
            BaseResponse response = await _mediator.Send(command);
            return Ok(response);

        }

        [HttpPost("export")]
        [RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Configuration.ENTITY_NAME, "dev/devices/export", Privileges.Configuration.Rights.SHARE_CONFIGURATION)]
        public async Task<IActionResult> ExportDeviceAsync([FromBody] ExportDevice command)
        {
            command.ObjectType = FileEntityConstants.DEVICE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("import")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/import", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> ImportDevicesAsync([FromBody] ImportFile command)
        {
            command.ObjectType = FileEntityConstants.DEVICE;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/signals/{signalId}")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/devices/{id}/signals/{signalId}", Privileges.Asset.Rights.READ_ASSET)]
        public async Task<IActionResult> GetDeviceSnapshotAsync([FromRoute] string id, [FromRoute] string signalId)
        {
            var command = new GetDeviceSignalSnapshot(id, signalId);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("signals")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/devices/signals", Privileges.Asset.Rights.READ_ASSET)]
        public async Task<IActionResult> GetDeviceSnapshotAsync([FromBody] GetDeviceSignalSnapshot command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }


        [HttpGet("{id}/metrics")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/{id}/metrics", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> SearchMetricByDeviceIdAsync([FromRoute] string id, [FromQuery] bool isIncludeDisabledMetric = false)
        {
            var command = new GetMetricsByDeviceId(id, isIncludeDisabledMetric);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("metrics")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/metrics", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> SearchMetricByDeviceIdAsync([FromBody] GetMetricsByDeviceId command, [FromQuery] bool isIncludeDisabledMetric = false)
        {
            command.IsIncludeDisabledMetric = isIncludeDisabledMetric;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/snapshot")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/devices/{id}/snapshot", Privileges.Asset.Rights.READ_ASSET)]
        public async Task<IActionResult> GetMetricSnapshotAsync(string id)
        {

            var command = new GetMetricSnapshot(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("snapshot")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/devices/snapshot", Privileges.Asset.Rights.READ_ASSET)]
        public async Task<IActionResult> GetMetricSnapshotAsync([FromBody] GetMetricSnapshot command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("push")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.WRITE_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/push", Privileges.Device.Rights.WRITE_DEVICE)]
        public async Task<IActionResult> PushMessageAsync([FromBody] PushMessageToDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("brokers")]
        public async Task<IActionResult> SearchSharingBrokerAsync()
        {
            SearchSharingBroker command = new SearchSharingBroker();
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpHead("{id}")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/{id}", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> CheckExistDeviceAsync(string id)
        {
            var command = new CheckExistDevice(new string[] { id });
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("exist")]
        [RightsAuthorizeFilter(Privileges.Device.FullRights.READ_DEVICE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Device.ENTITY_NAME, "dev/devices/exist", Privileges.Device.Rights.READ_DEVICE)]
        public async Task<IActionResult> CheckExistDeviceAsync([FromBody] CheckExistDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpPost("metrics/generate/assembly")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateAssetTemplateAttributeAssemblyAsync([FromBody] GenerateDeviceMetricAssemply command, [FromQuery] string token, [FromServices] ITokenService tokenService)
        {
            var tokenValid = await tokenService.CheckTokenAsync(token);
            if (!tokenValid)
            {
                return NotFound(new { IsSuccess = false, Message = command.Id });
            }
            var response = await _mediator.Send(command);
            return File(response.Data, "application/octet-stream", response.Name);
        }

        [HttpPost("has_binding")]
        public async Task<IActionResult> SearchDeviceHasBinding([FromBody] GetDeviceHasBinding command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("fetch")]
        public async Task<IActionResult> FetchAsync([FromBody] FetchDevice command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("metrics/fetch")]
        public async Task<IActionResult> FetchMetricAsync([FromBody] FetchDeviceMetric command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("binding/validate")]
        public async Task<IActionResult> ValidateAssetAttributesSeriesAsync([FromBody] ValidateDeviceBindings command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
