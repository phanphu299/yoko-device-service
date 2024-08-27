using System.Threading;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class GenerateAssetTemplateAttributeAssemblyHandler : IRequestHandler<GenerateAssetTemplateAttributeAssembly, AttributeAssemblyDto>
    {
        private readonly IAssetTemplateAssemblyService _assetAssemblyService;
        public GenerateAssetTemplateAttributeAssemblyHandler(IAssetTemplateAssemblyService assetAssemblyService)
        {
            _assetAssemblyService = assetAssemblyService;
        }
        public System.Threading.Tasks.Task<AttributeAssemblyDto> Handle(GenerateAssetTemplateAttributeAssembly request, CancellationToken cancellationToken)
        {
            return _assetAssemblyService.GenerateAssemblyAsync(request.AssetTemplateId, cancellationToken);
        }
    }
}