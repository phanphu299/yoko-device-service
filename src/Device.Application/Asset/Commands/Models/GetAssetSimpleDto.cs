using System;
using System.Linq;
using System.Linq.Expressions;

namespace Device.Application.Asset.Command.Model
{
    public class GetAssetSimpleDto : GetAssetDto
    {
        private static Func<Domain.Entity.Asset, GetAssetSimpleDto> Converter = Projection.Compile();
        private static Func<GetAssetDto, GetAssetSimpleDto> DtoConverter = DtoProjection.Compile();
        private static Expression<Func<Domain.Entity.Asset, GetAssetSimpleDto>> Projection
        {
            get
            {
                return entity => new GetAssetSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    HasWarning = entity.AssetWarning != null && entity.AssetWarning.HasWarning,
                    ResourcePath = entity.ResourcePath,
                    CreatedBy = entity.CreatedBy,
                    IsDocument = entity.IsDocument,
                    AssetTemplateName = entity.AssetTemplate == null ? null : entity.AssetTemplate.Name
                };
            }
        }
        private static Expression<Func<GetAssetDto, GetAssetSimpleDto>> DtoProjection
        {
            get
            {
                return entity => new GetAssetSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    HasWarning = entity.HasWarning,
                    ResourcePath = entity.ResourcePath,
                    CreatedBy = entity.CreatedBy,
                    IsDocument = entity.IsDocument,
                    Attributes = entity.Attributes.Select(x => new AssetAttributeDto()
                    {
                        AssetId = x.AssetId,
                        Id = x.Id,
                        Name = x.Name,
                        AttributeType = x.AttributeType,
                        DataType = x.DataType
                    }),
                    AssetTemplateName = entity.AssetTemplateName
                };
            }
        }

        public static new GetAssetSimpleDto Create(Domain.Entity.Asset entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
        public static GetAssetSimpleDto Create(GetAssetDto entity)
        {
            if (entity == null)
                return null;
            return DtoConverter(entity);
        }
    }
}
