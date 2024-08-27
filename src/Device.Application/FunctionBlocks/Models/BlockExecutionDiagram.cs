using System;
using System.Collections.Generic;

namespace Device.Application.Model
{
    internal class FunctionExecutionContent
    {
        public IEnumerable<FunctionExecutionLayer> Layers { get; set; }
    }

    internal class FunctionExecutionLayer
    {
        public IDictionary<string, BlockExecutionNode> Models { get; set; }
        public string Type { get; set; }
        public bool IsDiagramLink => string.Equals(Type, "diagram-links", StringComparison.InvariantCultureIgnoreCase);
    }

    internal class BlockExecutionNode
    {
        public BlockExecutionPort[] Ports { get; set; }
        public Guid Id { get; set; }
        public Guid? SourcePort { get; set; }
        public Guid? TargetPort { get; set; }
    }

    internal class BlockExecutionNodeExtra
    {
        public bool IsBlockTemplateNode { get; set; }
    }

    internal class BlockExecutionPort
    {
        public Guid Id { get; set; }
        public BlockExecutionPortExtra Extras { get; set; }
        public Guid[] Links { get; set; }
        public bool In { get; set; }
        public string Label { get; set; }
    }

    internal class BlockExecutionPortExtra
    {
        public BlockExecutionPortConfig Config { get; set; }
        public BlockExecutionPortBinding BlockBinding { get; set; }
        public Guid? TemplatePortId { get; set; }

    }

    internal class BlockExecutionPortConfig
    {
        public IDictionary<string, object> Payload { get; set; }
    }

    internal class BlockExecutionExtrasConfig
    {
        public string DataType { get; set; }
        public BlockExecutionPayload Payload { get; set; }
    }

    internal class BlockExecutionPayload
    {
        public Guid? AssetId { get; set; }
        public string AssetName { get; set; }
        public Guid? AttributeId { get; set; }
        public string AttributeName { get; set; }
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
    }

    internal class BlockExecutionPortBinding
    {
        public Guid Id { get; set; }
    }
}