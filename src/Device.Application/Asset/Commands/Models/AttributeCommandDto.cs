using System.Collections.Generic;
namespace Device.Application.Asset.Command.Model
{
    public class AttributeCommandDto
    {
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<AttributeCommandDetailDto> Attributes { get; set; }
        public AttributeCommandDto(string tenantId, string subscriptionId, string projectId, IEnumerable<AttributeCommandDetailDto> attributes)
        {
            TenantId = tenantId;
            ProjectId = projectId;
            SubscriptionId = subscriptionId;
            Attributes = attributes;
        }
    }
}