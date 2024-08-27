using System.Net.Http;
using System.Threading.Tasks;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Security.Helper;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;

namespace Function.Http
{
    public class TagsController
    {
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly ITagService _tagService;
        private readonly IEntityTagService _entityTagService;

        public TagsController(
                IConfiguration configuration,
                ITenantContext tenantContext,
                IEntityTagService entityTagService,
                ITagService tagService)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _tagService = tagService;
            _entityTagService = entityTagService;
        }
        [FunctionName("DeleteTagBinding")]
        public async Task<IActionResult> DeleteTagBinding([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "fnc/dev/tags")] HttpRequestMessage req, ExecutionContext context)
        {
            if (!await SecurityHelper.AuthenticateRequestAsync(req, _configuration))
                return new UnauthorizedResult();

            _tenantContext.RetrieveFromHeader(req.Headers);
            var content = await req.Content.ReadAsByteArrayAsync();
            var deleteTagMessage = content.Deserialize<DeleteTagMessage>();
            var entityTags = await _entityTagService.GetEntityIdsByTagIdsAsync(deleteTagMessage.TagIds);
            await _tagService.DeleteTagsAsync(deleteTagMessage.TagIds);
            await _entityTagService.RemoveCachesAsync(entityTags);
            return new OkResult();
        }
    }
}
