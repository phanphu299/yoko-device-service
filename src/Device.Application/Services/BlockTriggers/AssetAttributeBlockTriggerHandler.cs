using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Trigger.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Internal;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using System.Linq;
using AHI.Infrastructure.Exception.Helper;

namespace Device.Application.Service
{
    public class AssetAttributeBlockTriggerHandler : BaseBlockTriggerHandler
    {
        private readonly IServiceProvider _serviceProvider;
        public AssetAttributeBlockTriggerHandler(IBlockTriggerHandler next, IServiceProvider serviceProvider) : base(next)
        {
            _serviceProvider = serviceProvider;
        }

        protected override string TriggerType => BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT;

        protected override async Task ProcessRegisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
            var eventRequest = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(block.TriggerContent);
            eventRequest.AssetId = block.TriggerAssetId ?? eventRequest.AssetId;
            eventRequest.AttributeId = block.TriggerAttributeId ?? eventRequest.AttributeId;
            var tenantContextRequest = new TenantContext();
            tenantContextRequest.RetrieveFromString(tenantContext.TenantId, eventRequest.SubscriptionId, eventRequest.ProjectId);
            var eventService = httpClientFactory.CreateClient(HttpClientNames.EVENT_SERVICE, tenantContextRequest);
            // cleanup the existing trigger.
            _ = await eventService.DeleteAsync($"evn/eventforwardings/{block.Id}");
            await DeleteByNameAsync(block);
            var eventResponseMessage = await eventService.PostAsync($"evn/eventforwardings", new StringContent(JsonConvert.SerializeObject(new
            {
                Id = block.Id,
                Name = $"Block function {block.Id} forwarding",
                Type = "EVENT_TYPE_WEBHOOK",
                Content = new WebHookRequestContent()
                {
                    Endpoint = $"http://device-service/dev/blockexecutions/{block.Id}/execute?type=asset_attribute&asset_id={eventRequest.AssetId}&attribute_id={eventRequest.AttributeId}",
                    Payload = GetString(tenantContext, eventRequest)
                },
                Active = true,
                IsVisible = false,
                AssetAttributes = new[]
                {
                    new
                    {
                        AssetId = eventRequest.AssetId,
                        AttributeId = eventRequest.AttributeId
                    }
                }
            }), System.Text.Encoding.UTF8, "application/json"));
            if (eventResponseMessage.IsSuccessStatusCode)
            {
                var body = await eventResponseMessage.Content.ReadAsByteArrayAsync();
                var schedulerResponse = body.Deserialize<MessageResponse>();
                block.JobId = schedulerResponse.Id;
                // update the scheduler information
                //block.TriggerContent = JsonConvert.SerializeObject(eventRequest);
            }
            else
            {
                var streamContent = await eventResponseMessage.Content.ReadAsByteArrayAsync();
                var logger = _serviceProvider.GetRequiredService<ILoggerAdapter<AssetAttributeBlockTriggerHandler>>();

                var responseError = streamContent.Deserialize<MessageResponse>();
                logger.LogError($"Cannot register the event forwarding. {responseError.ToJson()}");

                if (responseError.ErrorCode.Equals(ExceptionErrorCode.ERROR_ENTITY_VALIDATION))
                {
                    var field = responseError.Fields.FirstOrDefault();
                    throw EntityValidationExceptionHelper.GenerateException(field?.Name, field?.ErrorCode, detailCode: MessageConstants.BLOCK_EXECUTION_ERROR_REGISTER_EVENT_FORWARDING);
                }
                throw new SystemCallServiceException(detailCode: MessageConstants.BLOCK_EXECUTION_ERROR_CALL_SERVICE);
            }
        }

        private string GetString(ITenantContext tenantContext, AssetAttributeTriggerDto eventRequest)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"tenantId={tenantContext.TenantId}");
            stringBuilder.AppendLine($"subscriptionId={eventRequest.SubscriptionId}");
            stringBuilder.AppendLine($"projectId={eventRequest.ProjectId}");
            stringBuilder.AppendLine($"assetId={eventRequest.AssetId}");
            stringBuilder.AppendLine($"attributeId={eventRequest.AttributeId}");
            return stringBuilder.ToString();
        }

        protected override async Task ProcessUnregisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
            var logger = _serviceProvider.GetRequiredService<ILoggerAdapter<AssetAttributeBlockTriggerHandler>>();
            var eventService = httpClientFactory.CreateClient(HttpClientNames.EVENT_SERVICE, tenantContext);
            var eventResponseMessage = await eventService.DeleteAsync($"evn/eventforwardings/{block.JobId}");
            if (!eventResponseMessage.IsSuccessStatusCode)
            {
                // this is fallback to function blockId
                eventResponseMessage = await eventService.DeleteAsync($"evn/eventforwardings/{block.Id}");
            }
            if (!eventResponseMessage.IsSuccessStatusCode)
            {
                await DeleteByNameAsync(block);
                var message = await eventResponseMessage.Content.ReadAsStringAsync();
                logger.LogError($"Unregister error for FBE: {block.Id}/{block.JobId}\r\n. Message {message}");
            }
        }
        private async Task DeleteByNameAsync(Domain.Entity.FunctionBlockExecution block)
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
            var logger = _serviceProvider.GetRequiredService<ILoggerAdapter<AssetAttributeBlockTriggerHandler>>();
            var eventService = httpClientFactory.CreateClient(HttpClientNames.EVENT_SERVICE, tenantContext);
            // this is the incomplete job_id, needs to query by the name and retry with new Id
            var searchContent = new StringContent(JsonConvert.SerializeObject(new
            {
                PageSize = int.MaxValue,
                Filter = JsonConvert.SerializeObject(new
                {
                    queryKey = "name",
                    queryType = "text",
                    operation = "eq",
                    queryValue = $"Block function {block.Id} forwarding"
                }),
                Fields = new[] { "id" }
            }), System.Text.Encoding.UTF8, "application/json");
            var eventResponseSearchMessage = await eventService.PostAsync("evn/eventforwardings/search", searchContent);
            if (eventResponseSearchMessage.IsSuccessStatusCode)
            {
                // parse and get the event forwarding Id
                var body = await eventResponseSearchMessage.Content.ReadAsByteArrayAsync();
                var eventforwardings = body.Deserialize<BaseSearchResponse<MessageResponse>>();

                // only 1 record is ideal result
                if (eventforwardings.TotalCount == 1)
                {
                    var eventforwarding = eventforwardings.Data.First();
                    await eventService.DeleteAsync($"evn/eventforwardings/{eventforwarding.Id}");
                }
            }
        }
    }
    public class WebHookRequestContent
    {
        [JsonProperty("webhook_endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("webhook_payload")]
        public string Payload { get; set; }
    }
}
