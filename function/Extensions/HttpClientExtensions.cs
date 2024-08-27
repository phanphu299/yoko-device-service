using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AHI.Device.Function.Model.SearchModel;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;

namespace Function.Extension
{
    public static class HttpClientExtension
    {
        public static async Task<BaseSearchResponse<T>> SearchAsync<T>(this HttpClient client, string url, FilteredSearchQuery query = null)
        {
            var response = await client.PostAsync(url, query is null ? null : new StringContent(query.AsJsonString, Encoding.UTF8, mediaType: "application/json"));
            response.EnsureSuccessStatusCode();

            var message = await response.Content.ReadAsByteArrayAsync();
            return message.Deserialize<BaseSearchResponse<T>>();
        }
    }
}