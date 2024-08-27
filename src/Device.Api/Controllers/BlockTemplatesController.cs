using System;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command;
using Device.Application.BlockTemplate.Query;
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
    public class BlockTemplatesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BlockTemplatesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blocktemplates", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> AddAsync([FromBody] AddFunctionBlockTemplate addBlockFunction)
        {
            var response = await _mediator.Send(addBlockFunction);
            return Created($"/dev/blocktemplates/{response.Id}", response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blocktemplates/{id}", Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateFunctionBlockTemplate command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted($"/dev/blocktemplates/{response.Id}", response);
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blocktemplates/{id}", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetEntityByIdAsync(Guid id)
        {
            var command = new GetBlockTemplateById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpGet("{id}/functionblocks")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/{id}/functionblocks", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> GetFunctionBlockByTemplateIdAsync(Guid id)
        {
            var command = new GetFunctionBlockByTemplateId(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blocktemplates/search", Privileges.BlockTemplate.Rights.READ_BLOCK_TEMPLATE, Privileges.BlockTemplate.Rights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> SearchAsync([FromBody] GetFunctionBlockTemplateByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.DELETE_BLOCK_TEMPLATE)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockTemplate.ENTITY_NAME, "dev/blocktemplates", Privileges.BlockTemplate.Rights.DELETE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> DeleteForceListAsync([FromBody] DeleteFunctionBlockTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(Guid id)
        {
            var command = new FetchBlockTemplate(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/check/used")]
        public async Task<IActionResult> CheckUsedBlockTemplateAsync([FromRoute] Guid id)
        {
            var command = new CheckUsedBlockTemplate(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("validation/content")]
        public async Task<IActionResult> ValidationTemplateContentAsync([FromBody] ValidationBlockContent command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
        [HttpPost("validation")]
        public async Task<IActionResult> ValidationBlockTemplatesAsync([FromBody] ValidationBlockTemplates command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveBlockTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.READ_BLOCK_TEMPLATE)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyArchiveBlockTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.BlockTemplate.FullRights.WRITE_BLOCK_TEMPLATE)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveBlockTemplate command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
