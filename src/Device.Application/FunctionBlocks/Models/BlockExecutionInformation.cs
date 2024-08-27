using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.BlockFunction.Trigger.Model;
using Device.Application.Constant;
using Device.Application.Service;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Model
{
    public class BlockExecutionInformation
    {
        public string Content { get; set; }
        public IEnumerable<BlockExecutionInputInformation> Inputs { get; set; }
        public IEnumerable<BlockExecutionOutputInformation> Outputs { get; set; }
    }

    public class BlockExecutionSnapshot
    {
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public Guid? JobId { get; set; }
        public IEnumerable<BlockExecutionInformation> Information { get; set; }
        public AssetAttributeTriggerDto ParsedTriggerContent => TriggerContent.FromJson<AssetAttributeTriggerDto>();
        public Guid AssetId => this.IsTriggerTypeAssetAttribute() ? (Guid)ParsedTriggerContent.AssetId : Guid.Empty;
        public Guid AttributeId => this.IsTriggerTypeAssetAttribute() ? (Guid)ParsedTriggerContent.AttributeId : Guid.Empty;
    }

    public class BlockExecutionBindingInformation : IBlockExecutionBinding
    {
        public Guid FunctionBlockId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public IDictionary<string, object> Payload { get; set; }
        public bool HasAutoMapping { get; set; } = false; // If we re-publishing the Block Execution with a changed version of Block Template, sometime system need to auto mapping the input/ output by name => Add this field for tracking those fields
        public Guid AssetId => (this.IsAssetAttributeDataType() || this.IsAssetTableDataType()) ? SafetyParse(Payload.GetValueOrDefault(PayloadConstants.ASSET_ID)?.ToString()) : Guid.Empty;
        public Guid AttributeId => this.IsAssetAttributeDataType() ? SafetyParse(Payload.GetValueOrDefault(PayloadConstants.ATTRIBUTE_ID)?.ToString()) : Guid.Empty;
        public Guid TableId => this.IsAssetTableDataType() ? SafetyParse(Payload.GetValueOrDefault(PayloadConstants.TABLE_ID)?.ToString()) : Guid.Empty;
        public string TableName => this.IsAssetTableDataType() ? Payload.GetValueOrDefault(PayloadConstants.TABLE_NAME)?.ToString() : null;
        
        private Guid SafetyParse(string input)
        {
            var result = Guid.Empty;
            Guid.TryParse(input, out result);
            return result;
        }
    }

    public class BlockExecutionInputInformation : BlockExecutionBindingInformation
    {
    }

    public class BlockExecutionOutputInformation : BlockExecutionBindingInformation
    {
        public bool HasLinkedPort { get; set; }
    }

    public class BlockExecutionInputComparer : IEqualityComparer<BlockExecutionInputInformation>
    {
        public bool Equals(BlockExecutionInputInformation x, BlockExecutionInputInformation y)
        {
            return x.DataType == y.DataType
                && x.FunctionBlockId == y.FunctionBlockId
                && x.Key == y.Key
                && x.HasAutoMapping == y.HasAutoMapping
                && x.Payload.ToJson() == y.Payload.ToJson();
        }

        public int GetHashCode(BlockExecutionInputInformation obj)
        {
            return obj.DataType.GetHashCode()
                    ^ obj.FunctionBlockId.GetHashCode()
                    ^ obj.Key.GetHashCode()
                    ^ obj.HasAutoMapping.GetHashCode()
                    ^ obj.Payload.ToJson().GetHashCode();
        }
    }

    public class BlockExecutionOutputComparer : IEqualityComparer<BlockExecutionOutputInformation>
    {
        public bool Equals(BlockExecutionOutputInformation x, BlockExecutionOutputInformation y)
        {
            return x.DataType == y.DataType
                && x.FunctionBlockId == y.FunctionBlockId
                && x.Key == y.Key
                && x.HasLinkedPort == y.HasLinkedPort
                && x.HasAutoMapping == y.HasAutoMapping
                && x.Payload.ToJson() == y.Payload.ToJson();
        }

        public int GetHashCode(BlockExecutionOutputInformation obj)
        {
            return obj.DataType.GetHashCode()
                    ^ obj.FunctionBlockId.GetHashCode()
                    ^ obj.Key.GetHashCode()
                    ^ obj.HasAutoMapping.GetHashCode()
                    ^ obj.HasLinkedPort.GetHashCode()
                    ^ obj.Payload.ToJson().GetHashCode();
        }
    }
}