using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Authorization;
using Device.Application.Analytic.Query;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public partial class AssetsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly JsonSerializerOptions _jsonSetting;

        public AssetsController(IMediator mediator)
        {
            _mediator = mediator;
            _jsonSetting = new JsonSerializerOptions()
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.READ_ASSET)]
        public async Task<IActionResult> GetAssetByIdAsync([FromRoute] Guid id, [FromQuery] bool useCache = true, [FromQuery] bool getFullAsset = false, [FromQuery] bool authorizeAssetAttributeAccess = true)
        {
            BaseAssetDto response;
            if (!getFullAsset)
                response = await _mediator.Send(new GetAssetById(id, authorizeAssetAttributeAccess: authorizeAssetAttributeAccess) { UseCache = useCache });
            else
                response = await _mediator.Send(new GetFullAssetById(id, authorizeAssetAttributeAccess: authorizeAssetAttributeAccess) { UseCache = useCache });
            return Ok(response);
        }

        [HttpPost("checkexist")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.READ_ASSET)]
        public async Task<IActionResult> CheckExistingAssetIdsAsync([FromBody] IEnumerable<Guid> ids)
        {
            var command = new CheckExistingAssetIds(ids);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/assets/search", Privileges.Asset.Rights.READ_ASSET, Privileges.Asset.Rights.READ_CHILD_ASSET, Privileges.EventForwarding.Rights.READ_EVENT_FORWARDING)]
        public async Task<IActionResult> SearchAsync([FromBody] GetAssetByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveAsset command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.READ_ASSET)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyArchivedAsset command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveAsset command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/children")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.READ_ASSET)]
        public async Task<IActionResult> LoadChildrenAsync(Guid id)
        {
            var command = new GetAssetChildren(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("hierarchy/search")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.READ_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/assets/hierarchy/search", Privileges.Asset.Rights.READ_ASSET, Privileges.Asset.Rights.READ_CHILD_ASSET, Privileges.EventForwarding.Rights.READ_EVENT_FORWARDING)]
        public async Task<IActionResult> SearchHierarchyAsync([FromBody] GetAssetHierarchy command)
        {
            command.SetTagIds();
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/clone")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> GetCloneAssetAsync(Guid id, [FromQuery] bool includeChildren, CancellationToken cancellationToken)
        {
            var command = new GetAssetClone(id, includeChildren);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPatch("edit")]
        [RightsAuthorizeFilterAttribute(Privileges.Asset.FullRights.WRITE_ASSET)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/assets/edit", Privileges.Asset.Rights.WRITE_ASSET)]
        public async Task<IActionResult> UpsertAssetAsync([FromBody] JsonPatchDocument patchDoc)
        {
            var response = await _mediator.Send(new UpsertAsset(patchDoc));
            return Ok(response);
        }

        [HttpGet("{id}/snapshot")]
        public async Task<IActionResult> GetAttributeSnapshotAsync(Guid id, [FromQuery] bool useCache = true)
        {
            var command = new GetAttributeSnapshot(id) { UseCache = useCache };
            var response = await _mediator.Send(command);
            // using high performance json serializer
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);

            return File(jsonUtf8Bytes, "application/json");
        }

        [HttpPost("{id}/attributes/{attributeId}/push")]
        //[RightsAuthorizeFilterAttribute(Tuple.Create(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.WRITE_ASSET))]
        public async Task<IActionResult> SendConfigurationToIotDevice([FromRoute] Guid id, [FromRoute] Guid attributeId, [FromBody] SendConfigurationToDeviceIot command)
        {
            command.AssetId = id;
            command.AttributeId = attributeId;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("push")]
        //[RightsAuthorizeFilterAttribute(Tuple.Create(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.WRITE_ASSET))]
        public async Task<IActionResult> SendConfigurationToIotDeviceMutiple([FromBody] SendConfigurationToDeviceIotMutiple command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPatch("{assetId}/attributes")]
        [RightsAuthorizeFilterAttribute(Privileges.AssetAttribute.FullRights.WRITE_ASSET_ATTRIBUTE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/assets/{id}/attributes", Privileges.Asset.Rights.READ_ASSET)]
        public async Task<IActionResult> UpsertAttributesAsync(Guid assetId, [FromBody] JsonPatchDocument patchDoc)
        {
            UpsertAssetAttribute jsonPatch = new UpsertAssetAttribute
            {
                AssetId = assetId,
                Data = patchDoc
            };
            var response = await _mediator.Send(jsonPatch);
            return Ok(response);
        }

        [HttpPost("series")]
        public async Task<IActionResult> GetAssetAttributesSeries([FromBody] PaginationGetAssetAttributeSeries command, [FromQuery] int timeout = 5)
        {
            command.TimeoutInSecond = timeout;
            object response = Array.Empty<HistoricalDataDto>();
            var firstAsset = command.Assets.FirstOrDefault();
            if (command.BePaging && firstAsset != null && firstAsset.AttributeIds != null && firstAsset.RequestType == HistoricalDataType.SERIES)
                response = await _mediator.Send(command);
            else
            {
                response = await _mediator.Send(GetAssetAttributeSeries.Create(command));
            }

            //Detect if request has accept header in list mean client want to receive response as msgpack format.
            string[] messagePackAcceptType = { "application/msgpack", "application/x-msgpack", "application/*+msgpack" };
            var requestAccept = Request.Headers.Accept;
            if (messagePackAcceptType.Any(type => requestAccept.Contains(type)))
            {
                //serialize with message pack serializer. Dto need to config anotation [MessagePackObject], key [Key("keyname")]
                byte[] bytes = MessagePackSerializer.Serialize(response, ContractlessStandardResolver.Options);
                return File(bytes, "application/x-msgpack");
            }
            else
            {
                //Normal case, return response as application/json
                byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);
                return File(jsonUtf8Bytes, "application/json");
            }
        }

        [HttpPost("correlation")]
        ////[RightsAuthorizeFilterAttribute(new string[] { ApplicationInformation.APPLICATION_ID, ApplicationInformation.DASHBOARD_APPLICATION_ID }, "", "dev/assets/series", Privileges.Asset.FullRights.READ_ASSET, Privileges.Dashboard.FullRights.READ_DASHBOARD)]
        public async Task<IActionResult> GetAssetAttributesCorrelation([FromBody] GetAssetAttributeCorrelationData command, [FromQuery] int timeout = 5)
        {
            command.TimeoutInSecond = timeout;
            var response = await _mediator.Send(command);
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);

            return File(jsonUtf8Bytes, "application/json");
        }

        [HttpPost("histogram")]
        ////[RightsAuthorizeFilterAttribute(new string[] { ApplicationInformation.APPLICATION_ID, ApplicationInformation.DASHBOARD_APPLICATION_ID }, "", "dev/assets/series", Privileges.Asset.FullRights.READ_ASSET, Privileges.Dashboard.FullRights.READ_DASHBOARD)]
        public async Task<IActionResult> GetAssetAttributesHistogram([FromBody] GetAssetAttributeHistogramData command)
        {
            var response = await _mediator.Send(command);

            // using high performance json serializer
            byte[] jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(response, _jsonSetting);

            return File(jsonUtf8Bytes, "application/json");
        }

        [HttpPost("series/validate")]
        ////[RightsAuthorizeFilterAttribute(new string[] { ApplicationInformation.APPLICATION_ID, ApplicationInformation.DASHBOARD_APPLICATION_ID }, "", "dev/assets/series/validate", Privileges.Asset.FullRights.READ_ASSET, Privileges.Dashboard.FullRights.READ_DASHBOARD)]
        public async Task<IActionResult> ValidateAssetAttributesSeriesAsync([FromBody] ValidateAssetAttributesSeries command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("paths")]
        // //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, "", "dev/assets/paths",
        //     Privileges.Asset.FullRights.READ_ASSET,
        //     Privileges.Asset.FullRights.READ_CHILD_ASSET,
        //     Privileges.EventForwarding.FullRights.READ_EVENT_FORWARDING)]
        public async Task<IActionResult> GetAssetPathsAsync([FromBody] IEnumerable<Guid> ids, bool includeAttribute = false)
        {
            var command = new GetAssetPath(ids, includeAttribute);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/attributes/generate/assembly")]
        [AllowAnonymous]
        ////[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, "dev/assets/{id}/attributes/generate/assembly", Privileges.Asset.Rights.READ_ASSET, Privileges.Asset.Rights.READ_CHILD_ASSET)]
        public async Task<IActionResult> GenerateAssetAttributeAssemblyAsync(Guid id, [FromQuery] string token, [FromServices] ITokenService tokenService)
        {
            var tokenValid = await tokenService.CheckTokenAsync(token);
            if (!tokenValid)
            {
                return NotFound(new { IsSuccess = false, Message = id });
            }
            var command = new GenerateAssetAttributeAssembly(id);
            var response = await _mediator.Send(command);
            return File(response.Data, "application/octet-stream", response.Name);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(Guid id)
        {
            var command = new FetchAsset(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/attributes/{attributeId}/fetch")]
        public async Task<IActionResult> FetchAttributeAsync(Guid id, Guid attributeId)
        {
            var command = new FetchAssetAttribute(attributeId, id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/validate")]
        public async Task<IActionResult> ValidateAssetAttributesAsync(ValidateAssetAttributeList command)
        {
            command.ValidationType = Application.Enum.ValidationType.Asset;
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        [HttpPost("attributes/validate/multiple")]
        public async Task<IActionResult> ValidateMultipleAssetAttributesAsync(ValidateMultipleAssetAttributeList command)
        {
            command.ValidationType = Application.Enum.ValidationType.Asset;
            var response = await _mediator.Send(command);

            return Ok(response);
        }

        [HttpGet("{id}/validate")]
        public async Task<IActionResult> ValidateAssetAsync(Guid id)
        {
            var command = new ValidateAsset(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/export")]
        [RightsAuthorizeFilter(Privileges.Configuration.FullRights.SHARE_CONFIGURATION)]
        public async Task<IActionResult> ExportAssetAttributesAsync([FromBody] ExportAssetAttributes command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("attributes/parse")]
        [RightsAuthorizeFilter(Privileges.Asset.FullRights.WRITE_ASSET)]
        public async Task<IActionResult> ParseAssetAttributesAsync([FromBody] ParseAssetAttributes command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("{id}/attributes/{attributeId}/store")]
        public async Task<IActionResult> StoreAssetAttributeAsync(Guid id, Guid attributeId, [FromBody] StoreAssetAttribute command)
        {
            command.AssetId = id;
            command.AttributeId = attributeId;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("{id}/attributes/query")]
        public async Task<IActionResult> AssetAttributeQueryAsync(Guid id, [FromBody] AssetAttributeQuery command)
        {
            command.AssetId = id;
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
