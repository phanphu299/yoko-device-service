using System;
using System.Threading.Tasks;
using System.Web;
using Device.Application.BlockCategory.Command;
using Device.Application.BlockFunctionCategory.Command;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AHI.Infrastructure.Authorization;
using Device.Application.Constant;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class BlockCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BlockCategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories/{id}", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var command = new GetBlockCategoryById { Id = id };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories/search", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetBlockCategoryByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("hierarchy/search")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories/hierarchy/search", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> HierarchySearchAsync([FromBody] GetBlockCategoryHierarchy command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> AddAsync([FromBody] AddBlockCategory command)
        {
            var response = await _mediator.Send(command);
            return Created(HttpUtility.UrlEncode($"/dev/blockcategories/{response.Id}"), response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories/{id}", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateBlockCategory command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted(HttpUtility.UrlEncode($"/dev/blockcategories/{response.Id}"), response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.DELETE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blockcategories", Privileges.BlockTemplate.Rights.DELETE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> DeleteAsync([FromBody] DeleteBlockCategory command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("paths")]
        public async Task<IActionResult> GetAssetPathsAsync([FromBody] GetBlockCategoryPath command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveBlockCategory command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyArchiveBlockCategory command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveBlockCategory command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
