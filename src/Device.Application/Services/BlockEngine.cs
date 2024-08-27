using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;

namespace Device.Application.Service
{
    public class BlockEngine : IBlockEngine
    {
        private readonly IBlockExecution _blockExecution;
        private readonly IHttpClientFactory _httpClientFactory;

        public BlockEngine(IBlockExecution blockExecution
                            , IHttpClientFactory httpClientFactory)
        {
            _blockExecution = blockExecution;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<T> HttpPostAsync<T>(IBlockHttpContext context)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            foreach (var header in context.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            var message = await client.PostAsync(context.Endpoint, context.Content);
            message.EnsureSuccessStatusCode();
            var content = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<T> HttpGetAsync<T>(IBlockHttpContext context)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            foreach (var header in context.Headers.Where(h => !string.IsNullOrEmpty(h.Value)))
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            var message = await client.GetAsync(context.Endpoint);
            message.EnsureSuccessStatusCode();
            var content = await message.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        public Task<BlockQueryResult> RunAsync(IBlockContext context)
        {
            return _blockExecution.ExecuteAsync(context);
        }
    }
}