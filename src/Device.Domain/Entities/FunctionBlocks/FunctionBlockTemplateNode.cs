using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockTemplateNode : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid BlockTemplateId { get; set; }
        public Guid FunctionBlockId { get; set; }
        public string BlockType { get; set; }
        public string Name { get; set; }
        public string AssetMarkupName { get; set; }
        public string TargetName { get; set; }
        public Guid? PortId { get; set; }
        public FunctionBlockTemplate BlockTemplate { get; set; }
        public FunctionBlock FunctionBlock { get; set; }
        public ICollection<FunctionBlockNodeMapping> Mappings { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public FunctionBlockTemplateNode()
        {
            Id = Guid.NewGuid();
            Mappings = new List<FunctionBlockNodeMapping>();
        }

        public FunctionBlockTemplateNode(Guid functionBlockId, string blockType, int sequentialNumber)
        {
            FunctionBlockId = functionBlockId;
            BlockType = blockType;
            SequentialNumber = sequentialNumber;
        }

        public FunctionBlockTemplateNode(Guid functionBlockId, string name, string assetMarkupName, string targetName, string blockType, Guid? portId)
        {
            Id = Guid.NewGuid();
            Mappings = new List<FunctionBlockNodeMapping>();
            FunctionBlockId = functionBlockId;
            Name = name;
            AssetMarkupName = assetMarkupName;
            TargetName = targetName;
            BlockType = blockType;
            PortId = portId;
        }
    }
}
