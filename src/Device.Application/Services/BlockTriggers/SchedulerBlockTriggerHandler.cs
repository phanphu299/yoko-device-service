using System;
using System.Net.Http;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Application.BlockFunction.Trigger.Model;
using Device.Application.Helper;
using AHI.Infrastructure.Exception.Helper;

namespace Device.Application.Service
{
    public class SchedulerBlockTriggerHandler : BaseBlockTriggerHandler
    {
        private readonly IServiceProvider _serviceProvider;
        public SchedulerBlockTriggerHandler(IBlockTriggerHandler next, IServiceProvider serviceProvider) : base(next)
        {
            _serviceProvider = serviceProvider;
        }

        protected override string TriggerType => BlockFunctionTriggerConstants.TYPE_SCHEDULER;

        protected override async Task ProcessRegisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
            var schedulerService = httpClientFactory.CreateClient(HttpClientNames.SCHEDULER_SERVICE);
            var schedulerRequest = JsonConvert.DeserializeObject<SchedulerTriggerDto>(block.TriggerContent);

            if (!CronJobHelper.IsValidCronExpression(schedulerRequest.Cron))
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(SchedulerTriggerDto.Cron));

            schedulerRequest.Id = block.Id;
            schedulerRequest.Endpoint = $"http://device-service/dev/blockexecutions/{block.Id}/execute";
            schedulerRequest.Method = "GET";
            schedulerRequest.AppendTenantContextData(tenantContext);

            _ = await schedulerService.DeleteAsync($"sch/jobs/{block.Id}/recurring");
            _ = schedulerService.PostAsync("sch/jobs/recurring", new StringContent(JsonConvert.SerializeObject(schedulerRequest), System.Text.Encoding.UTF8, "application/json"));

            // It's important to save the jobId so that it could be deleted later
            block.JobId = block.Id;

            // *NOTE: No need to wait for the scheduler's response, because the block execution need to be created first, in case the scheduler need to be started now, it could be able to call back to the new created block execution
            // if (schedulerResponseMessage.IsSuccessStatusCode)
            // {
            //     var triggerDto = JsonConvert.DeserializeObject<BlockExecutionTriggerDto>(block.TriggerContent);
            //     if (triggerDto != null)
            //     {
            //         schedulerRequest.OverrideTrigger = triggerDto.OverrideTrigger;
            //     }
            //     var body = await schedulerResponseMessage.Content.ReadAsByteArrayAsync();
            //     var schedulerResponse = body.Deserialize<SchedulerResponse>();
            //     block.JobId = schedulerResponse.Id;
            //     // update the scheduler information
            //     block.TriggerContent = JsonConvert.SerializeObject(schedulerRequest);
            // }
            // else
            // {
            //     var content = await schedulerResponseMessage.Content.ReadAsByteArrayAsync();
            //     var logger = _serviceProvider.GetRequiredService<ILoggerAdapter<SchedulerBlockTriggerHandler>>();
            //     logger.LogError($"Cannot register the scheduler. {content}");

            //     var responseError = content.Deserialize<MessageResponse>();
            //     logger.LogError($"Cannot register the scheduler. {responseError.ToJson()}");

            //     if (responseError.ErrorCode.Equals(ExceptionErrorCode.ERROR_ENTITY_VALIDATION))
            //     {
            //         var field = responseError.Fields.FirstOrDefault();
            //         throw EntityValidationExceptionHelper.GenerateException(field?.Name, field?.ErrorCode, detailCode: MessageConstants.BLOCK_EXECUTION_ERROR_REGISTER_SCHEDULER_JOB);
            //     }
            //     throw new SystemCallServiceException(detailCode: MessageConstants.BLOCK_EXECUTION_ERROR_CALL_SERVICE);
            // }
        }

        protected override async Task ProcessUnregisterAsync(Domain.Entity.FunctionBlockExecution block)
        {
            var httpClientFactory = _serviceProvider.GetService<IHttpClientFactory>();
            var tenantContext = _serviceProvider.GetRequiredService<ITenantContext>();
            var logger = _serviceProvider.GetRequiredService<ILoggerAdapter<SchedulerBlockTriggerHandler>>();
            var schedulerService = httpClientFactory.CreateClient(HttpClientNames.SCHEDULER_SERVICE, tenantContext);
            var schedulerResponseMessage = await schedulerService.DeleteAsync($"sch/jobs/{block.JobId}/recurring");
            if (!schedulerResponseMessage.IsSuccessStatusCode)
            {
                schedulerResponseMessage = await schedulerService.DeleteAsync($"sch/jobs/{block.Id}/recurring");
            }
            if (!schedulerResponseMessage.IsSuccessStatusCode)
            {
                // not good, need to log the error message
                var message = await schedulerResponseMessage.Content.ReadAsStringAsync();
                logger.LogError($"Ungister error for FBE: {block.Id}/{block.JobId}\r\n. Message {message}");
            }
        }
    }
    public class SchedulerResponse
    {
        public Guid Id { get; set; }
    }
}