using System.Collections.Generic;
using System.Net.Http;
using Device.Application.Service.Abstraction;
namespace Device.Application.Service
{
    public class BlockHttpContext : IBlockHttpContext
    {
        public BlockHttpContext(IBlockEngine engine, string endpoint)
        {
            _engine = engine;
            _endpoint = endpoint;
        }
        private string _endpoint;
        public string Endpoint => _endpoint;
        private List<KeyValuePair<string, string>> _headers = new List<KeyValuePair<string, string>>();
        public IEnumerable<KeyValuePair<string, string>> Headers => _headers;
        private HttpContent _content;
        public HttpContent Content => _content;
        private IBlockEngine _engine;
        public IBlockEngine BlockEngine => _engine;

        public BlockHttpContext SetContent(HttpContent content)
        {
            _content = content;
            return this;
        }
        public IBlockHttpContext SetHeader(string key, string value)
        {
            _headers.Add(KeyValuePair.Create<string, string>(key, value));
            return this;
        }
    }
}