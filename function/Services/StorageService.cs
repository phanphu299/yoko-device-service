using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Newtonsoft.Json.Linq;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using System.IO;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;
using System;
using AHI.Infrastructure.SharedKernel.Abstraction;
using System.Text;

namespace AHI.Device.Function.Service
{
    public class StorageService : IStorageService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<StorageService> _logger;

        public StorageService(IHttpClientFactory httpClientFactory, ITenantContext tenantContext, ILoggerAdapter<StorageService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<string> UploadAsync(string path, string fileName, byte[] data)
        {
            var httpClient = GetStorageClient();
            var fileContent = new ByteArrayContent(data);
            var response = await UploadFileAsync(httpClient, path, fileName, fileContent);

            var responseContent = await response.Content.ReadAsByteArrayAsync();
            var filePath = responseContent.Deserialize<JObject>()["filePath"].ToString();

            return filePath;
        }

        // Download file directly to output Stream by using HttpContent.CopyToAsync
        // Using HttpCompletionOption.ResponseHeadersRead to avoid HttpClient buffer http content to internal (memory) stream when receive request
        public async Task DownloadFileToStreamAsync(string filePath, Stream outputStream)
        {
            HttpResponseMessage response = null;
            try
            {
                var httpClient = GetStorageClient();
                var path = await GetLinkAsync(httpClient, filePath);

                var downloadClient = GetDownloadClient();
                response = await downloadClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(outputStream);
            }
            catch (HttpRequestException ex)
            {
                // Log and return default
                _logger.LogError(ex, $"Failed to download file: {filePath}");
                throw;
            }
            finally // when using HttpCompletionOption.ResponseHeadersRead, need to explicitly dispose response when finish ASAP, to avoid holding http resources
            {
                response?.Dispose();
            }
        }

        private HttpClient GetStorageClient()
        {
            return _httpClientFactory.CreateClient(ClientNameConstant.STORAGE_SERVICE, _tenantContext);
        }

        private HttpClient GetDownloadClient()
        {
            return _httpClientFactory.CreateClient(ClientNameConstant.DOWNLOAD_CLIENT);
        }

        private async Task<HttpResponseMessage> UploadFileAsync(HttpClient storageClient, string path, string fileName, HttpContent fileContent)
        {
            var link = await GetLinkAsync(storageClient, path, skipCheckExists: true);
            path = new Uri(link).PathAndQuery.TrimStart('/'); // extract file path from returned url

            HttpResponseMessage response;
            using (var content = new MultipartFormDataContent())
            {
                content.Add(fileContent, "file", fileName);

                response = await storageClient.PostAsync(path, content);
            }
            response.EnsureSuccessStatusCode();
            return response;
        }

        private async Task<string> GetLinkAsync(HttpClient storageClient, string path, bool skipCheckExists = false)
        {
            var requestBody = new { FilePath = path, SkipCheckExists = skipCheckExists }.ToJson();
            var response = await storageClient.PostAsync($"sta/files/link", new StringContent(requestBody, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
