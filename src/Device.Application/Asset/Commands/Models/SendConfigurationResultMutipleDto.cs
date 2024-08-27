using System.Collections.Generic;
using System.Text.Json.Serialization;
using static AHI.Infrastructure.Exception.Model.ValidationResultApiResponse;
namespace Device.Application.Asset.Command.Model
{
    public class SendConfigurationResultMutipleDto
    {
        public string Status { get; set; }
        public IEnumerable<SendConfigurationResultMutipleDetailDto<AttributeCommandDto>> Detail { get; }
        public SendConfigurationResultMutipleDto(string status, IEnumerable<SendConfigurationResultMutipleDetailDto<AttributeCommandDto>> detail)
        {
            Status = status;
            Detail = detail;
        }
    }
    public class SendConfigurationResultMutipleDetailDto<T>
    {
        [JsonIgnore]
        public bool Status { get; set; }
        public string Message { get; set; }
        public IEnumerable<FieldFailureMessage> Fields { get; }
        public T Data { get; set; }
        public SendConfigurationResultMutipleDetailDto(bool status, string message, IEnumerable<FieldFailureMessage> fields, T data)
        {
            Status = status;
            Message = message;
            Fields = fields;
            Data = data;
        }
        public SendConfigurationResultMutipleDetailDto(bool status, string message, IEnumerable<FieldFailureMessage> fields)
        {
            Status = status;
            Message = message;
            Fields = fields;
        }
    }
}