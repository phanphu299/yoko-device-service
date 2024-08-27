using System;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class SingleBlockAttributeContext : ISingleBlockAttributeContext, IBlockAttributeContext
    {
        private Guid _attributeId;
        public Guid AttributeId => _attributeId;
        public IBlockAttributeContext SetAttributeId(Guid attributeId)
        {
            _attributeId = attributeId;
            return this;
        }
    }
}