using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace AHI.Device.Function.Model.ImportModel.Converter
{
    public class JsonStringTrimmer : JsonConverter<string>
    {
        public override bool CanWrite => false;
        public override string ReadJson(JsonReader reader, Type objectType, [AllowNull] string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                return ((string)reader.Value).Trim();
            }
            catch (Exception e)
            {
                throw new JsonException(e.Message, e);
            }
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] string value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}