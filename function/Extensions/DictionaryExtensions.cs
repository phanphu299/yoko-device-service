using System;
using System.Collections.Generic;

namespace Function.Extension
{
    public static class DictionaryExtensions
    {
        public static object GetValueOrDefault(this IDictionary<Guid, string> payload, Guid? key)
        {
            if (key == null)
                return null;
            return payload.ContainsKey(key.Value) ? payload[key.Value] : null;
        }
    }
}
