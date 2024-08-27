using System;
using Newtonsoft.Json;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class JsonFileExtension
    {
        // Example: Assuming multiple entry in a zip file, placed in a root directory
        // C:\{model_name}s_yyyyMMddHHmmss\{prefix}_{name}_yyyyMMddHHmmss.json
        // The total length of the path above should be within 260 character
        // Exclude all fixed length substrings, the limit length for all {model_name}, {prefix} and {name} should be within 220
        private const int HARD_LIMIT = 219;
        public const string JSON_EXTENSION = ".json";
        public const string ZIP_EXTENSION = ".zip";

        public static string CreateJsonFileName(this string name, string prefix, string timestamp, int model_length = 0)
        {
            var builder = new System.Text.StringBuilder(name);

            // Add prefix
            if(!string.IsNullOrEmpty(prefix))
                builder.Insert(0, prefix).Insert(prefix.Length, '_');

            // Shorten to limit length
            var limit = HARD_LIMIT - model_length;
            if(builder.Length > limit)
                builder.Remove(limit, builder.Length - limit);
            
            // Append timestamp and extension
            builder.Append('_').Append(timestamp).Append(JSON_EXTENSION);

            // Remove invalid characters
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                builder.Replace(c, '_');

            return builder.ToString();
        }

        public static string CreateZipFileName(this string model, string timestamp)
        {
            return $"{model}s_{timestamp}{ZIP_EXTENSION}";
        }

        public static T ReadSingleObject<T>(this JsonTextReader reader, JsonSerializer serializer, Action<System.Exception> errorHandler) where T : class
        {
            try
            {
                if(reader.Read())
                {
                    return serializer.Deserialize<T>(reader);
                }
            }
            catch(JsonException e)
            {
                errorHandler.Invoke(e);
            }
            return null;
        }
    }
}