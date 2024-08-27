using System;
using System.Collections.Generic;
using Device.Application.Block.Command.Model;

namespace Device.Application.Model
{
    internal class TemplateContent
    {
        public IEnumerable<TemplateLayers> Layers { get; set; }
    }

    internal class TemplateLayers
    {
        public IDictionary<string, FunctionModel> Models { get; set; }
        public string Type { get; set; }
        public bool IsDiagramLink => string.Equals(Type, "diagram-links");
    }

    internal class FunctionModel
    {
        public Guid Id { get; set; }
        public FunctionPort[] Ports { get; set; }
        public Guid? Source { get; set; }
        public Guid? Target { get; set; }
        public Guid? SourcePort { get; set; }
        public Guid? TargetPort { get; set; }
        public Guid FunctionBlockId { get; set; }
        public string Name { get; set; }
        public string FunctionBlockType { get; set; }
        public Guid? AssetId { get; set; }
    }

    internal class FunctionPort
    {
        public Guid Id { get; set; }
        public bool In { get; set; }
        public FunctionBinding BlockBinding { get; set; }
        public Guid[] Links { get; set; }
    }

    internal class FunctionBinding
    {
        public Guid Id { get; set; }
        public Guid FunctionBlockId { get; set; }
        public string BindingType { get; set; }
        public string DataType { get; set; }
    }

    internal class NodeLink
    {
        public Guid Id { get; set; }
        public FunctionModel Current { get; set; }
        public IEnumerable<NodeLink> Next { get; set; }
    }

    internal class NodeGraph
    {
        public FunctionModel Current { get; set; }
        public Guid LinkId { get; set; }
        public int Index { get; set; }
        public NodeGraph(FunctionModel current, Guid linkId, int index)
        {
            Current = current;
            Index = index;
            LinkId = linkId;
        }
    }

    public class FunctionBlockTemplateContent
    {
        public string Content { get; set; }
        public Guid NodeId { get; set; }
    }

    public class FunctionBlockTemplateContentResult
    {
        public string DesignContent { get; set; }
        public Guid Version { get; set; }
        public IEnumerable<GetFunctionBlockDto> Blocks { get; set; }
        public IEnumerable<FunctionBlockTemplateContent> Contents { get; set; }
    }
}