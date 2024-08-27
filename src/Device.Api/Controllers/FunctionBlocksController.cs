using System;
using System.Threading.Tasks;
using System.Web;
using AHI.Infrastructure.Authorization;
using Device.Application.Block.Command;
using Device.Application.BlockSnippet.Command;
using Device.Application.Constant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class FunctionBlocksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FunctionBlocksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> AddAsync([FromBody] AddFunctionBlock command)
        {
            var response = await _mediator.Send(command);
            return Created($"/dev/functionblocks/{response.Id}", response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/{id}", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateFunctionBlock command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted($"/dev/functionblocks/{response.Id}", response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.DELETE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks", Privileges.BlockTemplate.Rights.DELETE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> DeleteForceListAsync([FromBody] DeleteFunctionBlock command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/{id}", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetEntityByIdAsync(Guid id)
        {
            var command = new GetFunctionBlockById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/search", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetFunctionBlockByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }


        [HttpGet("snippets/{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/snippets/{id}", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var command = new GetBlockSnippetById() { Id = id };
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("snippets/search")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/search", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetBlockSnippetByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("snippets")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/snippets", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> AddAsync([FromBody] AddBlockSnippet command)
        {
            var response = await _mediator.Send(command);
            return Created(HttpUtility.UrlEncode($"/dev/functionblocks/snippets/{response.Id}"), response);
        }

        [HttpPut("snippets")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/snippets/{id}", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> UpdateAsync([FromBody] UpdateBlockSnippet command)
        {
            var response = await _mediator.Send(command);
            return Accepted(HttpUtility.UrlEncode($"/dev/functionblocks/snippets/{response.Id}"), response);
        }

        [HttpDelete("snippets")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.DELETE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/snippets", Privileges.BlockTemplate.Rights.DELETE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> DeleteAsync([FromBody] DeleteBlockSnippet command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/clone")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/{id}", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetBlockCloneAsync(Guid id)
        {
            var command = new GetFunctionBlockClone(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPatch("edit")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/functionblocks/{id}", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> UpsertBlockAsync([FromBody] JsonPatchDocument patchDoc)
        {
            var response = await _mediator.Send(new UpsertFunctionBlock(patchDoc));
            return Ok(response);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(Guid id)
        {
            var command = new FetchFunctionBlock(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/check/used")]
        public async Task<IActionResult> CheckUsedFunctionBlockAsync([FromRoute] Guid id)
        {
            var command = new CheckUsedFunctionBlock(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpPost("validation")]
        public async Task<IActionResult> ValidationFunctionBlockAsync([FromBody] ValidationFunctionBlocks command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("validation/content")]
        public async Task<IActionResult> ValidationFunctionBlockContentChangedAsync([FromBody] ValidationFunctionBlockContent command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        
        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveFunctionBlock command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyFunctionBlock command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveFunctionBlock command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
