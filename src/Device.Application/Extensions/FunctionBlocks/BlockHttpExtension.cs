using System.Net.Http;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Newtonsoft.Json;
namespace Device.Application.Service
{
    public static class BlockHttpExtension
    {
        public static IBlockHttpContext AddHeader(this IBlockHttpContext context, string key, string value)
        {
            context.SetHeader(key, value);
            return context;
        }
        public static Task<T> PostAsync<T>(this IBlockHttpContext context, object objectValue, string contentType = "application/json")
        {
            var content = new StringContent(JsonConvert.SerializeObject(objectValue), System.Text.Encoding.UTF8, contentType);
            context.SetContent(content);
            return context.BlockEngine.HttpPostAsync<T>(context);
        }
        public static Task<T> GetAsync<T>(this IBlockHttpContext context)
        {
            return context.BlockEngine.HttpGetAsync<T>(context);
        }
    }
}