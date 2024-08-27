using System;

namespace Device.Application.Service.Abstraction
{
    public interface ISingleBlockAttributeContext : IBlockAttributeContext
    {
        Guid AttributeId { get; }
        IBlockAttributeContext SetAttributeId(Guid attributeId);
    }
}