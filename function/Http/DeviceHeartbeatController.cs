using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Security.Helper;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
namespace Function.Http
{
    public class DeviceHeartbeatController
    {
        private readonly ILoggerAdapter<DeviceHeartbeatController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDeviceHeartbeatService _deviceHeartbeatService;
        private readonly ITenantContext _tenantContext;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IAssetNotificationService _notificationService;
        private readonly IMasterService _masterService;

        public DeviceHeartbeatController(
            IConfiguration configuration,
            ILoggerAdapter<DeviceHeartbeatController> logger,
            IDeviceHeartbeatService deviceHeartbeatService,
            ITenantContext tenantContext,
            IAssetNotificationService notificationService,
            IMasterService masterService,
            IDeviceRepository deviceRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _deviceHeartbeatService = deviceHeartbeatService;
            _tenantContext = tenantContext;
            _notificationService = notificationService;
            _masterService = masterService;
            _deviceRepository = deviceRepository;
        }

        // this controller will be call by scheduler to periodically check the device quality
        [FunctionName("SignalQualityCheck")]
        public async Task<IActionResult> SignalQualityCheckAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/signal/quality")] HttpRequestMessage req)
        {
            _logger.LogDebug($"[SignalQualityCheck] Starting...");
            if (!await SecurityHelper.AuthenticateRequestAsync(req, _configuration))
            {
                _logger.LogTrace($"[SignalQualityCheck] Unauthorized...");
                return new UnauthorizedResult();
            }

            var allProjects = await _masterService.GetAllProjectsAsync();
            var deviceChanges = new List<(string, string, string, string)>();
            var tasks = allProjects.Select(project => _deviceHeartbeatService.ProcessSignalQualityAsync(project));
            var result = await Task.WhenAll(tasks);

            deviceChanges.AddRange(result.SelectMany(x => x));

            // sending the notification to FE, this line of code will cause the performance issue, will refactor later
            var assetChanges = new List<(IEnumerable<Guid> AssetIds, string TenantId, string SubscriptionId, string ProjectId)>();
            foreach (var device in deviceChanges)
            {
                var (deviceId, tenantId, subscriptionId, projectId) = device;
                var ids = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);
                if (ids.Any())
                {
                    assetChanges.Add((ids, tenantId, subscriptionId, projectId));
                }
            }
            if (assetChanges.Any())
            {
                foreach (var assetChange in assetChanges)
                {
                    _logger.LogTrace($"[SignalQualityCheck] Sending Changes notification : Project: {assetChange.ProjectId} - Device: {assetChange.ProjectId} - Assets: {assetChange.AssetIds.ToJson()}");
                    var notificationTasks = assetChange.AssetIds.Select(assetId =>
                           {
                               _tenantContext.RetrieveFromString(assetChange.TenantId, assetChange.SubscriptionId, assetChange.ProjectId);
                               var notificationMessage = new AssetNotificationMessage(AHI.Device.Function.Constant.NotificationType.ASSET, assetId);
                               return _notificationService.NotifyAssetAsync(notificationMessage);
                           });

                    // FE need to load the snapshot when receiving this message.
                    await Task.WhenAll(notificationTasks);
                }
            }
            _logger.LogDebug($"[SignalQualityCheck] Completed...");
            return new OkResult();
        }
    }
}