using System;

namespace Device.Application.Asset.Command.Model
{
    public class AssetDependency : BaseDependency
    {
        public AssetDependency(string type, Guid id, string name) : base(type, id, name)
        {
        }

        public static AssetDependency Create(string type, Guid id, string name)
        {
            return new AssetDependency(type, id, name);
        }
    }
}
