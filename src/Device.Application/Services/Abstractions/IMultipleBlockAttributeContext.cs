using System.Collections.Generic;

namespace Device.Application.Service.Abstraction
{
    public interface IMultipleBlockAttributeContext : IBlockAttributeContext
    {
        IEnumerable<string> AttributeNames { get; }
        IBlockAttributeContext SetAttributeNames(IEnumerable<string> attributeNames);
    }
}