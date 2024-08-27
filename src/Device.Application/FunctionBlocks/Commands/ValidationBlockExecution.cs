using System;
using Device.Application.Constant;
using MediatR;
using Device.Application.BlockFunction.Model;
using System.Collections.Generic;

namespace Device.Application.BlockFunction.Query
{
    public class ValidationBlockExecution : IRequest<ValidationBlockExecutionDto>
    {
        public Guid Id { get; set; }
        public IEnumerable<Connector> Connectors { get; set; } = new List<Connector>();
    }
    public class Connector
    {
        public Guid AssetId { get; set; }
        public string TargetName { get; set; }
        public string Type { get; set; } = BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;
    }
}
