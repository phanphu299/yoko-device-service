using System;
using System.Linq.Expressions;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Constants;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Asset;

namespace Device.Application.AssetTemplate.Command
{
    public class AssetTemplateAttribute
    {
        public Guid Id { get; set; }
        public GetAssetTemplateDto AssetTemplate { get; set; }
        [DynamicValidation(RemoteValidationKeys.attribute)]
        public string Name { get; set; }
        public string Value { get; set; }
        // public bool EnabledExpression { get; set; } = true;
        // public string Expression { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public AttributeMapping Payload { get; set; }
        public AssetTemplateAttribute()
        {
            Id = Guid.NewGuid();
        }
        static Func<AssetTemplateAttribute, Domain.Entity.AssetAttributeTemplate> Converter = Projection.Compile();
        private static Expression<Func<AssetTemplateAttribute, Domain.Entity.AssetAttributeTemplate>> Projection
        {
            get
            {
                return element => new Domain.Entity.AssetAttributeTemplate
                {
                    Id = element.Id,
                    Name = element.Name,
                    Value = element.Value,
                    // EnabledExpression = element.EnabledExpression,
                    // Expression = element.Expression,
                    DataType = element.DataType,
                    AttributeType = element.AttributeType,
                    UomId = element.UomId,
                    DecimalPlace = element.DecimalPlace,
                    ThousandSeparator = element.ThousandSeparator
                };
            }
        }

        public static Domain.Entity.AssetAttributeTemplate Create(AssetTemplateAttribute model)
        {
            if (model == null)
                return null;
            return Converter(model);
        }
    }
}
