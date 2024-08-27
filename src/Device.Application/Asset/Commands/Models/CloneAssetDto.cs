using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.AssetTemplate.Command.Model;

namespace Device.Application.Asset.Command.Model
{
    public class CloneAssetDto
    {
        static Func<Domain.Entity.Asset, CloneAssetDto> Converter = Projection.Compile();
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int RetentionDays { get; set; }
        public Guid? ParentAssetId { get; set; }
        public Guid? AssetTemplateId { get; set; }
        //public IEnumerable<GetAssetAttributeDto> Attributes { get; set; }
        public IEnumerable<CloneAssetDto> Children { get; set; }
        public GetAssetTemplateDto AssetTemplate { get; set; }
        public CloneAssetDto()
        {
            // Attributes = new List<GetAssetAttributeDto>();
            Children = new List<CloneAssetDto>();
        }

        private static Expression<Func<Domain.Entity.Asset, CloneAssetDto>> Projection
        {
            get
            {
                return entity => new CloneAssetDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    Children = entity.Children.Select(Create).Where(x => x != null),
                    AssetTemplate = GetAssetTemplateDto.Create(entity.AssetTemplate)
                };
            }
        }

        public static CloneAssetDto Create(Domain.Entity.Asset entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }
}
