using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Device.Job.Model;
using Device.Job.Service.Abstraction;

namespace Device.Job.Controller
{
    [Route("dev/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "oidc")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _service;

        public JobsController(IJobService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddJobAsync(AddJob model)
        {
            var response = await _service.AddJobAsync(model);
            return AcceptedAtRoute("GetJobStatus", new { id = response.Id }, response);
        }

        [HttpGet("{id}/status", Name = "GetJobStatus")]
        public async Task<IActionResult> GetJobStatusAsync([FromRoute] Guid id)
        {
            var command = new GetJobStatus(id);
            var response = await _service.GetJobStatusAsync(command);
            return Ok(response);
        }
    }
}