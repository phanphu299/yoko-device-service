using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.Constants;
using AHI.Infrastructure.Validation.CustomAttribute;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Asset.Command
{
    public class AddAsset : UpsertTagCommand
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [DynamicValidation(RemoteValidationKeys.asset_name)]
        public string Name { get; set; }
        public Guid? ParentAssetId { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public int? RetentionDays { get; set; }
        public bool IsDocument { get; set; }
        public IEnumerable<AssetAttribute> Attributes { get; set; } = new List<AssetAttribute>();
        public IEnumerable<AttributeMapping> Mappings { get; set; } = new List<AttributeMapping>();
        public IEnumerable<AddAsset> Children { get; set; } = new List<AddAsset>();
        static Func<AddAsset, Domain.Entity.Asset> Converter = Projection.Compile();
        private static Expression<Func<AddAsset, Domain.Entity.Asset>> Projection
        {
            get
            {
                return entity => new Domain.Entity.Asset
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    ParentAssetId = entity.AssetTemplateId,
                    AssetTemplateId = entity.AssetTemplateId,
                    RetentionDays = entity.RetentionDays ?? 90,
                    IsDocument = entity.IsDocument
                };
            }
        }
        public static Domain.Entity.Asset Create(AddAsset model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
