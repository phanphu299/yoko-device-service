using System;
using Device.Application.Asset.Command.Model;
using MediatR;
namespace Device.Application.Asset.Command
{
    public class SendConfigurationToDeviceIot : IRequest<SendConfigurationResultDto>
    {
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public object Value { get; set; }
        public Guid RowVersion { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
    }
}