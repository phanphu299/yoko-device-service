using System.Collections.Generic;

namespace Device.ApplicationExtension.Extension
{
    public static class DictionaryExtensions
    {
        public static object GetValueOrDefault(this IDictionary<string, object> payload, string key)
        {
            return payload.ContainsKey(key) ? payload[key] : null;
        }
    }
}