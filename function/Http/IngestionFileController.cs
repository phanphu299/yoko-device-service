using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Security.Helper;

namespace Function.Http
{
    public class IngestionFileController
    {
        private readonly ITenantContext _tenantContext;
        private readonly IDataIngestionService _dataIngestionService;

        public IngestionFileController(ITenantContext tenantContext, IDataIngestionService dataIngestionService)
        {
            _tenantContext = tenantContext;
            _dataIngestionService = dataIngestionService;
        }

        [FunctionName("IngestionFileController")]
        public async Task<IActionResult> RunAsync(
                [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/api/ingestion/validate")] HttpRequestMessage req)
        {
            ClaimsPrincipal principal = await SecurityHelper.ValidateTokenAsync(req.Headers.Authorization);
            if (principal == null)
            {
                return new UnauthorizedResult();
            }

            var payload = await req.Content.ReadAsStringAsync();
            var message = JsonConvert.DeserializeObject<DataIngestionMessage>(payload);

            _tenantContext.SetTenantId(message.TenantId);
            _tenantContext.SetSubscriptionId(message.SubscriptionId);
            _tenantContext.SetProjectId(message.ProjectId);
            var response = await _dataIngestionService.ValidateIngestionDataAsync(message.FilePath);
            if (!response.IsSuccess)
            {
                await _dataIngestionService.SendFileIngestionStatusNotifyAsync(ActionStatus.Fail, AHI.Device.Function.Constant.DescriptionMessage.INGEST_FAIL);
            }

            return new OkObjectResult(response);
        }
    }
}