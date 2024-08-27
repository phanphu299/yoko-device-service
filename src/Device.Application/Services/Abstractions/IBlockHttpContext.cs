using System.Collections.Generic;
using System.Net.Http;
namespace Device.Application.Service.Abstraction
{
    public interface IBlockHttpContext
    {
        //IBlockContext Parent { get; }
        public IBlockEngine BlockEngine { get; }
        string Endpoint { get; }
        IEnumerable<KeyValuePair<string, string>> Headers { get; }
        HttpContent Content { get; }
        IBlockHttpContext SetHeader(string key, string value);
        BlockHttpContext SetContent(HttpContent content);
    }
}