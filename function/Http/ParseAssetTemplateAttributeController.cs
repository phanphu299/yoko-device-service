using System;
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
    public class ParseAssetTemplateAttributeController
    {
        private readonly ITenantContext _tenantContext;
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;
        private readonly ITemplateAttributeParserService _attributeParserService;

        public ParseAssetTemplateAttributeController(
                IConfiguration configuration,
                ITenantContext tenantContext,
                IUserContext userContext,
                ITemplateAttributeParserService attributeParserService)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _userContext = userContext;
            _attributeParserService = attributeParserService;
        }
        [FunctionName("ParseAttributeTemplate")]
        public async Task<IActionResult> ParseAttributeAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/assettemplates/attributes/parse")] HttpRequestMessage req, ExecutionContext context)
        {
            if (!await SecurityHelper.AuthenticateRequestAsync(req, _configuration))
                return new UnauthorizedResult();

            _tenantContext.RetrieveFromHeader(req.Headers);
            var content = await req.Content.ReadAsByteArrayAsync();
            var message = content.Deserialize<AssetTemplateAttributeMessage>();
            _userContext.SetUpn(message.Upn);
            var activityId = Guid.NewGuid();

            var response = await _attributeParserService.ParseAsync(message, activityId, context);     
            return new OkObjectResult(response);
        }
    }
}