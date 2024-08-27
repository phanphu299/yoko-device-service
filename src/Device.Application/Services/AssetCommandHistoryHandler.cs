using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Domain.Entity;

namespace Device.Application.Service
{
    public class AssetCommandHistoryHandler : IAssetCommandHistoryHandler
    {
        private readonly IAssetService _assetService;
        private readonly ILoggerAdapter<AssetCommandHistoryHandler> _logger;
        private readonly IReadAssetRepository _readAssetRepository;

        public AssetCommandHistoryHandler(IAssetService assetService,
                                        ILoggerAdapter<AssetCommandHistoryHandler> logger,
                                        IReadAssetRepository readAssetRepository)
        {
            _assetService = assetService;
            _logger = logger;
            _readAssetRepository = readAssetRepository;
        }

        public async Task SaveAssetAttributeValueAsync(params TimeSeries[] timeSeries)
        {
            foreach (var snapshot in timeSeries)
            {
                var request = new Application.Asset.Command.SendConfigurationToDeviceIot();
                request.AssetId = snapshot.AssetId;
                request.AttributeId = snapshot.AttributeId;
                request.Value = snapshot.ValueText ?? snapshot.Value?.ToString(); // Command Attribute with type text will stored value in ValueText
                var asset = await _readAssetRepository.OnlyAssetAsQueryable()
                                                .Include(x => x.AssetAttributeCommandMappings)
                                                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeCommand)
                                                .FirstOrDefaultAsync(x => x.Id == snapshot.AssetId);
                if (asset == null)
                    return;

                System.Guid? rowVersion;
                if (asset.AssetTemplateId.HasValue && asset.AssetAttributeCommandMappings.Any(x => x.Id == snapshot.AttributeId))
                {
                    //Has Template Id & Current Attribute was created from Template.
                    rowVersion = asset.AssetAttributeCommandMappings.First(x => x.Id == snapshot.AttributeId).RowVersion;
                }
                else
                {
                    // Current Attribute wasn't created from Template
                    rowVersion = asset.Attributes.FirstOrDefault(x => x.Id == snapshot.AttributeId && x.AssetAttributeCommand != null)?.AssetAttributeCommand.RowVersion;
                }

                if (rowVersion == null)
                {
                    _logger.LogTrace($"Skipped send config to device - Cannot find the valid Row Version for Asset-Attribute: {snapshot.AssetId} - {snapshot.AttributeId}");
                    continue;
                }

                request.RowVersion = rowVersion.Value;
                await _assetService.SendConfigurationToDeviceIotAsync(request, default(System.Threading.CancellationToken));
            }
        }
    }
}
