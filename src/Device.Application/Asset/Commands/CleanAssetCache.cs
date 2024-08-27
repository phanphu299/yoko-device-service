using System.Collections.Generic;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Asset.Command
{
    public class CleanAssetCache
    {
        public IEnumerable<AssetAttributeDto> AssetAttributes { get; set; }
        public bool OnlyCleanAssetDetail { get; set; }

        public CleanAssetCache()
        {
        }

        public CleanAssetCache(IEnumerable<AssetAttributeDto> assetAttributes)
        {
            AssetAttributes = assetAttributes;
        }
    }
}