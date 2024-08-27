using System;
using System.Threading.Tasks;
using Device.Job.Service.Abstraction;
using System.Net.Http;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using System.Text;
using Device.Application.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace Device.Job.Service
{
    public class StorageService : IStorageService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        public StorageService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        public async Task<string> UploadFromEndpointAsync(string path, string fileName, string downloadEndpoint)
        {
            var httpClient = GetStorageClient();
            var link = await GetLinkAsync(httpClient, path, skipCheckExists: true);
            path = new Uri(link).PathAndQuery.TrimStart('/'); // extract file path from returned url
            HttpResponseMessage response;
            using (var content = new MultipartFormDataContent())
            {
                var byteArrayContent = new ByteArrayContent(Array.Empty<byte>());
                content.Add(byteArrayContent, "file", fileName);
                content.Add(new StringContent(downloadEndpoint), "downloadEndpoint");
                response = await httpClient.PostAsync($"sta/files/{path}", content);
            }
            // return response without ensure success to allow caller to handle fail response

            var responseContent = await response.Content.ReadAsByteArrayAsync();
            var result = responseContent.Deserialize<CdnResult>();

            var token = await GetTokenAsync(httpClient);
            return $"{result.FilePath}?token={token}";
        }

        private async Task<string> GetTokenAsync(HttpClient client)
        {
            var response = await client.PostAsync("sta/files/token", null);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            return content.Deserialize<GetTokenResponse>().Token;
        }

        private HttpClient GetStorageClient()
        {
            var client = _httpClientFactory.CreateClient(HttpClientNames.STORAGE, _tenantContext);
            return client;
        }


        private async Task<string> GetLinkAsync(HttpClient storageClient, string path, bool skipCheckExists = false)
        {
            var requestBody = new { FilePath = path, SkipCheckExists = skipCheckExists }.ToJson();
            var response = await storageClient.PostAsync($"sta/files/link", new StringContent(requestBody, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private class GetTokenResponse
        {
            public bool IsSuccess { get; set; }
            public string Token { get; set; }
        }

        private class CdnResult
        {
            public bool IsSuccess { get; set; }
            public string FilePath { get; set; }
        }
    }
}
