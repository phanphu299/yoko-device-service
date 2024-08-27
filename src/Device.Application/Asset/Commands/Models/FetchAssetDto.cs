using System;

namespace Device.Application.Asset.Command.Model
{
    public class FetchAssetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public static FetchAssetDto Create(Domain.Entity.Asset entity)
        {
            if (entity == null)
                return null;
                
            return new FetchAssetDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
        }

        public static FetchAssetDto Create(GetAssetDto asset)
        {
            if (asset == null)
                return null;
                
            return new FetchAssetDto
            {
                Id = asset.Id,
                Name = asset.Name
            };
        }
    }
}