using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Constant;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class AssetTemplateChangedProcessing
    {
        private readonly ITenantContext _tenantContext;
        private readonly IAssetTemplateService _assetTemplateService;

        public AssetTemplateChangedProcessing(ITenantContext tenantContext, IAssetTemplateService assetTemplateService)
        {
            _tenantContext = tenantContext;
            _assetTemplateService = assetTemplateService;
        }

        [FunctionName("AssetTemplateChangedProcessing")]
        public async Task RunAsync(
        [RabbitMQTrigger(RabbitMQTriggerConstants.ASSET_TEMPLATE_CHANGED_PROCESSING, ConnectionStringSetting = "RabbitMQ")] byte[] data, ExecutionContext context)
        {
            BaseModel<AssetTemplateMessage> request = data.Deserialize<BaseModel<AssetTemplateMessage>>();
            var eventMessage = request.Message;

            // setup Domain to use inside repository
            _tenantContext.SetTenantId(eventMessage.TenantId);
            _tenantContext.SetSubscriptionId(eventMessage.SubscriptionId);
            _tenantContext.SetProjectId(eventMessage.ProjectId);

            await _assetTemplateService.ProcessChangeAsync(eventMessage);
        }
    }
}