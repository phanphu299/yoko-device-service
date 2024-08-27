using System.Collections.Generic;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class MultipleBlockAttributeContext : IMultipleBlockAttributeContext, IBlockAttributeContext
    {
        private IEnumerable<string> _attributeNames;
        public IEnumerable<string> AttributeNames => _attributeNames;
        public IBlockAttributeContext SetAttributeNames(IEnumerable<string> attributeNames)
        {
            _attributeNames = attributeNames;
            return this;
        }
    }
}