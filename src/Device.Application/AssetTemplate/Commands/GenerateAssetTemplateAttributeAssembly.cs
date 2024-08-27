using System;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GenerateAssetTemplateAttributeAssembly : IRequest<AttributeAssemblyDto>
    {
        public Guid AssetTemplateId { get; set; }
        public GenerateAssetTemplateAttributeAssembly(Guid assetTemplateId)
        {
            AssetTemplateId = assetTemplateId;
        }
    }
}