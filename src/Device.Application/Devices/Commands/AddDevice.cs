
using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using Device.Application.Device.Command.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Constant;
using System.Text.RegularExpressions;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Device.Command
{
    public class AddDevice : UpsertTagCommand, IRequest<AddDeviceDto>, INotification
    {
        [DynamicValidation(RemoteValidationKeys.device)]
        public string Id { get; set; }

        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }

        public string TelemetryTopic { get; set; }

        public string CommandTopic { get; set; }
        public bool? HasCommand { get; set; }
        public string Description { get; set; }
        public Guid TemplateId { get; set; }
        public int RetentionDays { get; set; }
        public string DeviceContent { get; set; }
        public bool EnableHealthCheck { get; set; } = false;
        public int? HealthCheckMethod { get; set; }
        public int MonitoringTime { get; set; } = 900;
        public string CreatedBy { get; set; }
        static Func<AddDevice, string, Domain.Entity.Device> Converter = Projection.Compile();

        private static Expression<Func<AddDevice, string, Domain.Entity.Device>> Projection
        {
            get
            {
                return (entity, projectId) => new Domain.Entity.Device
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TelemetryTopic = RemapProject(entity.TelemetryTopic, projectId),
                    CommandTopic = RemapProject(entity.CommandTopic, projectId),
                    HasCommand = entity.HasCommand,
                    Description = entity.Description,
                    TemplateId = entity.TemplateId,
                    RetentionDays = entity.RetentionDays,
                    DeviceContent = entity.DeviceContent,
                    EnableHealthCheck = entity.EnableHealthCheck,
                    MonitoringTime = entity.MonitoringTime,
                    HealthCheckMethodId = entity.HealthCheckMethod,
                    SignalQualityCode = entity.EnableHealthCheck ? (int)SignalQualityCode.BAD : (int?)null,
                    Status = entity.EnableHealthCheck ? SignalStatusConstants.DISCONNECTED : SignalStatusConstants.NOT_APPLICABLE,
                    CreatedBy = entity.CreatedBy
                };
            }
        }

        public static Domain.Entity.Device Create(AddDevice model, string projectId)
        {
            if (model != null)
            {
                return Converter(model, projectId);
            }
            return null;
        }

        private static string RemapProject(string topic, string projectId)
        {
            if (!string.IsNullOrEmpty(topic))
                return Regex.Replace(topic, RegexConstants.PATTERN_PROJECT_ID, projectId);
            return topic;
        }
    }
}
