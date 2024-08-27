using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AHI.Infrastructure.Repository
{
    public class JsonKeyExtractor
    {
        private readonly Stack<JToken> _stack;

        public JsonKeyExtractor()
        {
            _stack = new Stack<JToken>();
        }

        public HashSet<string> ExtractKeys(JObject data)
        {
            var keys = new HashSet<string>();
            _stack.Clear();

            _stack.Push(data);

            while (_stack.TryPeek(out _))
            {
                var token = _stack.Pop();

                if (token.Type != JTokenType.Object)
                {
                    if (token.Parent is JProperty current)
                    {
                        var key = current.Name;
                        if (!string.IsNullOrEmpty(current.Parent?.Path))
                        {
                            key = $"{current.Parent.Path}.{key}";
                        }
                        keys.Add(key);
                    }
                    continue;
                }
                foreach (var childToken in token.Values())
                    _stack.Push(childToken);
            }
            return keys;
        }
    }
}