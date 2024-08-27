using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Job.Model;
using Device.Job.Constant;
using Device.Job.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using FluentValidation;
using Device.Application.Constant;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace Device.Job.Service
{
    public class JobService : IJobService
    {
        private readonly ICache _cache;
        private readonly IConfiguration _configuragion;
        private readonly ITenantContext _tenantContext;
        private readonly JobBackgroundService _jobBackgroundService;
        private readonly IValidator<AddJob> _validator;
        private readonly ILoggerAdapter<JobService> _logger;

        public JobService(ICache cache,
            IConfiguration configuragion,
            ITenantContext tenantContext,
            JobBackgroundService jobBackgroundService,
            IValidator<AddJob> validator,
            ILoggerAdapter<JobService> logger)
        {
            _cache = cache;
            _configuragion = configuragion;
            _tenantContext = tenantContext;
            _jobBackgroundService = jobBackgroundService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<JobDto> AddJobAsync(AddJob model)
        {
            Guid activityId = model.Payload.ContainsKey(NotificationKeys.ACTIVITY_ID) ? Guid.Parse(model.Payload[NotificationKeys.ACTIVITY_ID].ToString()) : Guid.NewGuid();
            Guid widgetId = model.Payload.ContainsKey(NotificationKeys.WIDGET_ID) ? Guid.Parse(model.Payload[NotificationKeys.WIDGET_ID].ToString()) : Guid.NewGuid();

            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start JobService - AddJobAsync");

            try
            {
                var validation = await _validator.ValidateAsync(model);
                if (validation.Errors.Any())
                {
                    throw new EntityValidationException(validation.Errors.ToList());
                }

                if (!JobType.IsValidJobType(model.Type.ToLower()))
                {
                    throw new GenericCommonException(message: $"Invalid {nameof(model.Type)}");
                }

                if (!JobOutputType.IsValidOutputType(model.OutputType.ToLower()))
                {
                    throw new GenericCommonException(message: $"Invalid {nameof(model.OutputType)}");
                }

                var jobInfo = AddJob.Create(model);
                var jobParam = GetJobInfo(jobInfo.Id);
                var key = GetJobKey(_tenantContext, jobInfo.Id);

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start JobService - StoreAsync | cacheKey: {key}");
                await _cache.StoreAsync(key, jobInfo);

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start JobService - QueueAsync");
                await _jobBackgroundService.QueueAsync(_tenantContext, jobInfo, activityId);

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | End JobService - AddJobAsync");
                return new JobDto(jobInfo.Id, jobParam.CheckIn, jobParam.Endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {activityId} | widgetId: {widgetId} | Error in JobService - AddJobAsync {ex.Message}");
                throw;
            }
        }

        public async Task<JobStatusDto> GetJobStatusAsync(GetJobStatus model)
        {
            var key = GetJobKey(_tenantContext, model.Id);
            var jobInfo = await _cache.GetAsync<JobInfo>(key);

            if (jobInfo == null)
            {
                throw new EntityNotFoundException();
            }

            var jobParam = GetJobInfo(jobInfo.Id);

            return new JobStatusDto(jobInfo.Id, jobParam.CheckIn, jobParam.Endpoint, jobInfo.Status, jobInfo.FilePath, jobInfo.FailedMessage);
        }

        public static string GetJobKey(ITenantContext tenantContext, Guid id)
        {
            return $"{tenantContext.TenantId}_{tenantContext.SubscriptionId}_{tenantContext.ProjectId}_job_{id}".ToLowerInvariant();
        }

        private (int CheckIn, string Endpoint) GetJobInfo(Guid id)
        {
            var checkIn = _configuragion["CheckInSeconds"].ToNumber();
            var endpoint = $"{_configuragion["PublicApi:Device"]}/dev/jobs/{id}/status";
            return (checkIn, endpoint);
        }
    }
}