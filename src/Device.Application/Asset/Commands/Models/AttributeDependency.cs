using System;

namespace Device.Application.Asset.Command.Model
{
    public class AttributeDependency : BaseDependency
    {
        public AttributeDependency(string type, Guid id, string name) : base(type, id, name)
        {
        }

        public static AttributeDependency Create(string type, Guid id, string name)
        {
            return new AttributeDependency(type, id, name);
        }
    }
}
