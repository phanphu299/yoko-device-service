using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Device.ApplicationExtension.Extension
{
    public static class JsonStringExtension
    {
        // public static string JsonSerialize(this object value)
        // {
        //     using (var stream = new MemoryStream())
        //     {
        //         JsonExtension.Serialize(value, stream);

        //         return Encoding.UTF8.GetString(stream.ToArray());
        //     }
        // }

        public static string JsonSerializeKeepDictionaryCase(this object value)
        {
            var defaultJsonSerializerSetting = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting;
            defaultJsonSerializerSetting.ContractResolver = new CamelCaseExceptDictionaryKeysResolver();

            return JsonConvert.SerializeObject(value, defaultJsonSerializerSetting);
        }

        class CamelCaseExceptDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);

                contract.DictionaryKeyResolver = propertyName => propertyName;

                return contract;
            }
        }
    }
}