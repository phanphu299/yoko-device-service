using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GenerateAssetAttributeAssembly : IRequest<AttributeAssemblyDto>
    {
        public Guid AssetId { get; set; }
        public GenerateAssetAttributeAssembly(Guid assetId)
        {
            AssetId = assetId;
        }
    }
}