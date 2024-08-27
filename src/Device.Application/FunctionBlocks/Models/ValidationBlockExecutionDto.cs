using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.BlockFunction.Query;
using Device.Application.Constant;

namespace Device.Application.BlockFunction.Model
{
    public class ValidationBlockExecutionDto
    {
        public bool IsSyncWithBlockFunction { get; set; }
        public IEnumerable<ValidationConnectorDto> Connectors { get; set; }
    }

    public class ValidationConnectorDto
    {
        public Guid AssetId { get; set; }
        public string TargetName { get; set; }
        public string Type { get; set; } = BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;
        public Guid? TargetId { get; set; }
        public ValidationBlockExecutionPayloadDto Payload { get; set; }
        static Func<Connector, ValidationConnectorDto> Converter = Projection.Compile();
        private static Expression<Func<Connector, ValidationConnectorDto>> Projection
        {
            get
            {
                return entity => new ValidationConnectorDto
                {
                    AssetId = entity.AssetId,
                    TargetName = entity.TargetName,
                    Type = entity.Type
                };
            }
        }
        public static ValidationConnectorDto Create(Connector entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }
    public class TargetConnector
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid AssetId { get; set; }
        public string Type { get; set; } = BindingDataTypeIdConstants.TYPE_ASSET_TABLE;
        public ValidationBlockExecutionPayloadDto Payload { get; set; }
    }
    public class ValidationBlockExecutionPayloadDto
    {
        public string AttributeType { get; set; }
        public bool EnabledExpression { get; set; }

    }
}
