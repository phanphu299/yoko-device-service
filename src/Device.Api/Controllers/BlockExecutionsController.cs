using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System;
using Device.Application.FunctionBlock.Command;
using Device.Application.BlockFunction.Query;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.Authorization;
using Device.Application.Constant;

namespace Device.Api.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class BlockExecutionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BlockExecutionsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpGet("{id}/execute")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}/execute", Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> ExecuteFunctionBlockWithGetAsync(Guid id, [FromQuery] long execution_time, [FromQuery] long? unixTimestamp)
        {
            if (execution_time == 0 && unixTimestamp == 0)
            {
                return Ok();
            }
            var startDate = execution_time.ToString().UnixTimeStampToDateTime().CutOffNanoseconds();
            var snapshotDatetTime = (DateTime?)null;
            if (unixTimestamp > 0)
            {
                snapshotDatetTime = unixTimestamp.ToString().UnixTimeStampToDateTime().CutOffNanoseconds();
            }
            var command = new RunFunctionBlockExecution(id, startDate, snapshotDatetTime);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("{id}/execute")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}/execute", Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> ExecuteFunctionBlockWithPostAsync(Guid id, [FromQuery] long execution_time, [FromQuery] long? unixTimestamp)
        {
            if (execution_time == 0 && unixTimestamp == 0)
            {
                return Ok();
            }
            var startDate = execution_time.ToString().UnixTimeStampToDateTime().CutOffNanoseconds();
            var snapshotDatetTime = (DateTime?)null;
            if (unixTimestamp > 0)
            {
                snapshotDatetTime = unixTimestamp.ToString().UnixTimeStampToDateTime().CutOffNanoseconds();
            }
            var command = new RunFunctionBlockExecution(id, startDate, snapshotDatetTime);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions", Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> AddAsync([FromBody] AddFunctionBlockExecution addBlockFunction)
        {
            var response = await _mediator.Send(addBlockFunction);
            return Created($"/dev/blockexecutions/{response.Id}", response);
        }

        [HttpPut("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}", Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateFunctionBlockExecution command)
        {
            command.Id = id;
            var response = await _mediator.Send(command);
            return Accepted($"/dev/blockexecutions/{response.Id}", response);
        }

        [HttpGet("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}", Privileges.BlockExecution.Rights.READ_BLOCK_EXECUTION)]
        public async Task<IActionResult> GetEntityByIdAsync(Guid id)
        {

            var command = new GetFunctionBlockExecutionById(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("search")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/search", Privileges.BlockExecution.Rights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> SearchAsync([FromBody] GetFunctionBlockExecutionByCriteria command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("{id}/publish")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}/publish", Privileges.BlockExecution.Rights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> PublishBlockFunctionAsync(Guid id)
        {
            var command = new PublishFunctionBlockExecution(id, true);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("{id}/unpublish")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}/unpublish", Privileges.BlockExecution.Rights.READ_BLOCK_EXECUTION, Privileges.BlockExecution.Rights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> UnpublishBlockFunctionAsync(Guid id)
        {
            var command = new UnpublishFunctionBlockExecution(id, true);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.DELETE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions/{id}", Privileges.BlockExecution.Rights.DELETE_BLOCK_EXECUTION)]
        public async Task<IActionResult> DeleteForceAsync(Guid id)
        {
            var command = new DeleteFunctionBlockExecution(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpDelete]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.DELETE_BLOCK_EXECUTION)]
        //[RightsAuthorizeFilterAttribute(ApplicationInformation.APPLICATION_ID, Privileges.BlockExecution.ENTITY_NAME, "dev/blockexecutions", Privileges.BlockExecution.Rights.DELETE_BLOCK_EXECUTION)]
        public async Task<IActionResult> DeleteForceListAsync([FromBody] DeleteFunctionBlockExecution command)
        {
            command.IsListDelete = true;
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpGet("{id}/fetch")]
        public async Task<IActionResult> FetchAsync(Guid id)
        {
            var command = new FetchFunctionBlockExecution(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateAsync([FromBody] ValidationBlockExecution command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION)]
        public async Task<IActionResult> ArchiveAsync([FromBody] ArchiveFunctionBlockExecution command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("archive/verify")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.READ_BLOCK_EXECUTION)]
        public async Task<IActionResult> VerifyArchiveAsync([FromBody] VerifyFunctionBlockExecution command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("retrieve")]
        [RightsAuthorizeFilter(Privileges.BlockExecution.FullRights.WRITE_BLOCK_EXECUTION)]
        public async Task<IActionResult> RetrieveAsync([FromBody] RetrieveFunctionBlockExecution command)
        {
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
