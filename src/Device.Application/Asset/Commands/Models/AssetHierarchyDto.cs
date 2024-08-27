using System.Linq.Expressions;
using System;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Asset.Command.Model
{
    public class AssetHierarchyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public Guid? ParentAssetId { get; set; }
        public bool HasWarning { get; set; }
        public bool IsFoundResult { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public int RetentionDays { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();

        static Func<Domain.Entity.Hierarchy, AssetHierarchyDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.Hierarchy, AssetHierarchyDto>> Projection
        {
            get
            {
                return entity => new AssetHierarchyDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    HasWarning = entity.HasWarning,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.TemplateId,
                    RetentionDays = entity.RetentionDays,
                    CreatedUtc = entity.CreatedUtc,
                    IsFoundResult = entity.IsFoundResult
                };
            }
        }

        public static AssetHierarchyDto Create(Domain.Entity.Hierarchy entity)
        {
            if (entity == null)
            {
                return null;
            }

            return Converter(entity);
        }
    }
}
