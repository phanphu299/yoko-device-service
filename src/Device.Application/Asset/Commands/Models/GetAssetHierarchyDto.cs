using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Asset.Command.Model
{
    public class GetAssetHierarchyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime RootAssetCreatedUtc { get; set; }
        public bool HasWarning { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public int RetentionDays { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();
        public IEnumerable<AssetHierarchyDto> Hierarchy { get; set; }

        static Func<Domain.Entity.AssetHierarchy, GetAssetHierarchyDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.AssetHierarchy, GetAssetHierarchyDto>> Projection
        {
            get
            {
                return entity => new GetAssetHierarchyDto
                {
                    Id = entity.AssetId,
                    Name = entity.AssetName,
                    CreatedUtc = entity.AssetCreatedUtc,
                    RootAssetCreatedUtc = GetRootAssetCreatedDate(entity),
                    HasWarning = entity.AssetHasWarning,
                    AssetTemplateId = entity.AssetTemplateId,
                    RetentionDays = entity.AssetRetentionDays,
                    Hierarchy = entity.Hierarchy.Select(AssetHierarchyDto.Create)
                };
            }
        }

        private static DateTime GetRootAssetCreatedDate(Domain.Entity.AssetHierarchy entity)
        {
            return entity.Hierarchy.FirstOrDefault()?.CreatedUtc ?? entity.AssetCreatedUtc;
        }

        public static GetAssetHierarchyDto Create(Domain.Entity.AssetHierarchy entity)
        {
            if (entity == null)
            {
                return null;
            }

            return Converter(entity);
        }
    }
}
