using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using AHI.Infrastructure.Validation.CustomAttribute;

namespace Device.Application.Asset.Command
{
    public class AssetAttribute
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        [DynamicValidation(RemoteValidationKeys.attribute)]
        public string Name { get; set; }
        public string Value { get; set; }
        // public bool EnableExpression { get; set; } = true;
        public string Expression { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public AttributeMapping Payload { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsStandalone { get; set; } = false;
        public AssetAttribute()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
        }
        static Func<AssetAttribute, Domain.Entity.AssetAttribute> Converter = Projection.Compile();
        private static Expression<Func<AssetAttribute, Domain.Entity.AssetAttribute>> Projection
        {
            get
            {
                return dto => new Domain.Entity.AssetAttribute
                {
                    Id = dto.Id,
                    AssetId = dto.AssetId,
                    Name = dto.Name,
                    Value = dto.Value,
                    // EnabledExpression = dto.EnableExpression,
                    DataType = dto.DataType,
                    //Expression = dto.Expression,
                    AttributeType = dto.AttributeType,
                    UomId = dto.UomId,
                    DecimalPlace = dto.DecimalPlace,
                    ThousandSeparator = dto.ThousandSeparator
                };
            }
        }

        public static Domain.Entity.AssetAttribute Create(AssetAttribute model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }

    }
}
