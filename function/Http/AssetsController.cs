using System.Net.Http;
using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Security.Helper;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.UserContext.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;

namespace Function.Http
{
    public class AssetsController
    {
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;
        private readonly IAssetAttributeParseService _assetService;
        
        public AssetsController(
            ITenantContext tenantContext,
            IUserContext userContext,
            IConfiguration configuration,
            IAssetAttributeParseService assetService
        )
        {
            _tenantContext = tenantContext;
            _userContext = userContext;
            _configuration = configuration;
            _assetService = assetService;
        }

        [FunctionName("ParseAssetAttributes")]
        public async Task<IActionResult> ParseAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/assets/attributes/parse")] HttpRequestMessage req,
            ExecutionContext context
        )
        {
            if (!await SecurityHelper.AuthenticateRequestAsync(req, _configuration))
                return new UnauthorizedResult();

            _tenantContext.RetrieveFromHeader(req.Headers);
            var content = await req.Content.ReadAsByteArrayAsync();
            var message = content.Deserialize<ParseAssetAttributeMessage>();
            _userContext.SetUpn(message.Upn);

            var response = await _assetService.ParseAsync(message, context);
            return new OkObjectResult(response);
        }
    }
}