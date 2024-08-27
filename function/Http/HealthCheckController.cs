using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;

namespace Function.Http
{
    public class HealthCheckController
    {
        [FunctionName("HealthProbeCheck")]
        public IActionResult LivenessProbeCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fnc/healthz")] HttpRequestMessage req)
        {
            return new OkResult();
        }
    }
}