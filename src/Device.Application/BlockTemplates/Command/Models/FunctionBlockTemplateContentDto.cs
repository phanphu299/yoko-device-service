using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Block.Command.Model;

namespace Device.Application.BlockTemplate.Command.Model
{
    /*
        This class using to map the block template content to POCO
        Author: Thanh Tran
    */
    public class FunctionBlockTemplateContentDto
    {
        public IEnumerable<ConnectorInfo> Inputs { get; set; }
        public IEnumerable<ConnectorInfo> Outputs { get; set; }
        public IEnumerable<GetFunctionBlockDto> FunctionBlocks { get; set; }
        public IEnumerable<FunctionBlockLinkDto> Links { get; set; }
    }
    public class ConnectorInfo
    {
        public Guid FunctionBlockId { get; set; }
        public string Name { get; set; }
        public string AssetMarkupName { get; set; }
        public string TargetName { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? PortId { get; set; }

        public ConnectorInfo(Guid functionBlockId, string name, string assetMarkupName, string targetName, Guid? assetId, Guid? portId)
        {
            FunctionBlockId = functionBlockId;
            Name = name;
            AssetMarkupName = assetMarkupName;
            TargetName = targetName;
            AssetId = assetId;
            PortId = portId;
        }

        public class ConnectorInfoComparer : IEqualityComparer<ConnectorInfo>
        {
            public bool Equals(ConnectorInfo x, ConnectorInfo y)
            {
                return x.FunctionBlockId == y.FunctionBlockId
                        && x.AssetId == y.AssetId
                        && x.PortId == y.PortId
                        && string.Equals(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(x.AssetMarkupName, y.AssetMarkupName, StringComparison.InvariantCultureIgnoreCase)
                        && string.Equals(x.TargetName, y.TargetName, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(ConnectorInfo obj)
            {
                return obj.FunctionBlockId.GetHashCode()
                        ^ obj.Name.GetHashCode()
                        ^ obj.PortId.GetHashCode()
                        ^ obj.AssetMarkupName.GetHashCode()
                        ^ obj.TargetName.GetHashCode()
                        ^ obj.AssetId.GetHashCode();
            }
        }
    }
    public class FunctionBlockLinkDto
    {
        public FunctionBlockLinkDetailDto Output { get; set; }
        public IEnumerable<FunctionBlockLinkDetailDto> Inputs { get; set; }

        public class FunctionBlockLinkDtoComparer : IEqualityComparer<FunctionBlockLinkDto>
        {
            public bool Equals(FunctionBlockLinkDto x, FunctionBlockLinkDto y)
            {
                return x.Output.ToJson() == y.Output.ToJson()
                    && x.Inputs.ToJson() == y.Inputs.ToJson();
            }

            public int GetHashCode(FunctionBlockLinkDto obj)
            {
                return obj.Output.ToJson().GetHashCode()
                        ^ obj.Inputs.ToJson().GetHashCode();
            }
        }
    }
    public class FunctionBlockLinkDetailDto
    {
        public Guid FunctionBlockId { get; set; }
        public Guid BindingId { get; set; }
        public Guid PortId { get; set; }
    }
}