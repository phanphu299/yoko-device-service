using System.Threading.Tasks;
using Device.Application.Device.Command;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Device.Api.Controller
{

    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public IntegrationsController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet("{id}/devices", Name = "GetAllDeviceByIntegration")]
        public async Task<IActionResult> GetByIdAsync(System.Guid id)
        {
            var command = new GetDeviceByIntegrationId(id);
            var response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
