using System.Collections.Generic;
namespace Device.Application.Device.Command.Model
{
    public class BrokerErrorResposeDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string DetailCode { get; set; }
        public IDictionary<string, string[]> Failures { get; set; }
    }
}