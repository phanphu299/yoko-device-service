using System;

namespace Device.Application.Asset.Command.Model
{
    public class FetchAssetAttributeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public FetchAssetDto Asset { get; set; }

        public static FetchAssetAttributeDto Create(GetAssetDto asset, AssetAttributeDto attribute)
        {
            if (asset == null || attribute == null)
                return null;
            
            return new FetchAssetAttributeDto
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Asset = FetchAssetDto.Create(asset)
            };
        }
    }
}