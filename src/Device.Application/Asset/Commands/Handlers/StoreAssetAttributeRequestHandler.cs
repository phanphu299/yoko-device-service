using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Constant;
using Device.Application.Events;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Device.Application.Asset.Command.Handler
{
    public class StorageAssetAttributeRequestHandler : IRequestHandler<StoreAssetAttribute, bool>
    {
        private readonly IBlockExecutionRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly ITenantContext _tenantContext;
        private readonly IAssetService _assetService;
        private readonly bool _enableChangedAssetAttributeEvent;
        private readonly ILoggerAdapter<StorageAssetAttributeRequestHandler> _logger;

        public StorageAssetAttributeRequestHandler(
            IBlockExecutionRepository repository,
            ISecurityService securityService,
            INotificationService notificationService,
            IDomainEventDispatcher dispatcher,
            ITenantContext tenantContext,
            IConfiguration configuration,
            ILoggerAdapter<StorageAssetAttributeRequestHandler> logger,
            IAssetService assetService)
        {
            _repository = repository;
            _notificationService = notificationService;
            _assetService = assetService;
            _dispatcher = dispatcher;
            _tenantContext = tenantContext;
            _logger = logger;

            bool.TryParse(configuration["Sdk:EnableChangedAssetAttributeEvent"] ?? bool.TrueString, out _enableChangedAssetAttributeEvent);
        }
        public async Task<bool> Handle(StoreAssetAttribute request, CancellationToken cancellationToken)
        {
            var result = false;
            if (request.Values != null && request.Values.Any())
            {
                switch (request.AttributeType)
                {
                    case AttributeTypeConstants.TYPE_COMMAND:
                        var command = new SendConfigurationToDeviceIot()
                        {
                            AssetId = request.AssetId,
                            AttributeId = request.AttributeId,
                            Value = request.Values.First().Value
                        };
                        // skip the current row version check. this is request from internal api.
                        await _assetService.SendConfigurationToDeviceIotAsync(command, cancellationToken, false);
                        result = true;
                        break;
                    case AttributeTypeConstants.TYPE_RUNTIME:
                        await _repository.SetAssetAttributeValueAsync(request.AssetId, request.AttributeId, request.Values);
                        result = true;
                        break;
                }

                // notify the ui client
                await _notificationService.SendAssetNotifyAsync(new AssetNotificationMessage(request.AssetId, NotificationType.ASSET, null));

                var maxTimestamp = request.Values.Max(x => x.Timestamp);
                var timestamp = maxTimestamp.ToUtcDateTimeOffset().ToUnixTimeMilliseconds();
                _logger.LogTrace($"[StorageAssetAttributeRequestHandler] Dispatch changed event for asset {request.AssetId} - timestamp: {timestamp}. (Config: {_enableChangedAssetAttributeEvent})");
                if (_enableChangedAssetAttributeEvent)
                {
                    // Dispatch the event to integrate with other services
                    await _dispatcher.SendAsync(new AssetAttributeChangedEvent(request.AssetId, timestamp, _tenantContext, forceReload: false));
                }
            }
            return result;
        }
    }
}
