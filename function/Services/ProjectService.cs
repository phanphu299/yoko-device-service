using System.Net.Http;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace AHI.Device.Function.Service
{
    public class ProjectService : IProjectService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public ProjectService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<ProjectDto> GetCurrentProjectAsync()
        {
            var projectClient = _httpClientFactory.CreateClient(ClientNameConstant.PROJECT_SERVICE, _tenantContext);
            var response = await projectClient.GetAsync($"prj/projects/{_tenantContext.ProjectId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<ProjectDto>();
        }
    }
}