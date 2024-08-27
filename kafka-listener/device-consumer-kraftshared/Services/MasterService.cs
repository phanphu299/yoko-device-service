using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using AHI.Infrastructure.Cache.Abstraction;
using System.Linq;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Service.Abstraction;

namespace Device.Consumer.KraftShared.Service
{
    public class MasterService : IMasterService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICache _cache;
        public MasterService(
                            IConfiguration configuration,
                            IHttpClientFactory httpClientFactory,
                            ICache cache)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }
        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var key = "projects_all";
            IEnumerable<ProjectDto> result = await _cache.GetAsync<IEnumerable<ProjectDto>>(key);
            if (result == null)
            {
                var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.MASTER_FUNCTION);
                var response = await httpClient.GetAsync($"fnc/mst/projects?migrated=true&type=asset");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<IEnumerable<ProjectDto>>(content);
            }
            return result.Where(x => !x.Deleted && 
                                     x.IsMigrated &&
                                     x.ProjectType == ProjectType.ASSET);
        }
    }
}