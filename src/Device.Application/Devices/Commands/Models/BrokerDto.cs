using System;
using System.Linq;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Constant;

namespace Device.Application.Device.Command.Model
{
    public class BrokerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string Status { get; set; }

        public BrokerContentDto DeserializedContent => Content.FromJson<BrokerContentDto>();

        public bool IsEmqxDevice => BrokerConstants.EMQX_BROKERS.Contains(Type);
    }
}
