using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using AHI.Device.Function.Model;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;

namespace AHI.Device.Function.Service
{
    public class MasterService : IMasterService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MasterService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var httpClient = _httpClientFactory.CreateClient(ClientNameConstant.MASTER_FUNCTION);
            var response = await httpClient.GetAsync($"fnc/mst/projects?migrated=true&type=asset");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            IEnumerable<ProjectDto> result = JsonConvert.DeserializeObject<IEnumerable<ProjectDto>>(content);

            return result.Where(x => !x.Deleted &&
                                     x.IsMigrated &&
                                     x.ProjectType == ProjectType.ASSET);
        }
    }
}