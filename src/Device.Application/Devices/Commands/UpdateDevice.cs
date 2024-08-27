using System;
using System.Linq.Expressions;
using Device.Application.Device.Command.Model;
using MediatR;
using Device.Application.Constants;
using AHI.Infrastructure.Validation.CustomAttribute;
using System.Text.RegularExpressions;
using Device.Application.Constant;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Device.Command
{
    public class UpdateDevice : UpsertTagCommand, IRequest<UpdateDeviceDto>
    {
        [DynamicValidation(RemoteValidationKeys.device)]
        public string Id { get; set; }

        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }

        public string TelemetryTopic { get; set; }

        public string CommandTopic { get; set; }

        public bool? HasCommand { get; set; }

        public string Description { get; set; }
        public int SasTokenDuration { get; set; }
        public bool EnableHealthCheck { get; set; }
        public int MonitoringTime { get; set; }
        public int? HealthCheckMethod { get; set; }
        public int RetentionDays { get; set; } = 90;

        [DynamicValidation(RemoteValidationKeys.device)]
        public string UpdatedDeviceId { get; set; }

        public bool DeviceIdChanged => !string.IsNullOrEmpty(UpdatedDeviceId) && Id != UpdatedDeviceId;
        public string BackupDeviceId => string.Format("{0}_{1:yyyyMMddHHmmssffff}", Id, DateTime.UtcNow);

        private static Func<UpdateDevice, string, Domain.Entity.Device> Converter = Projection.Compile();

        private static Expression<Func<UpdateDevice, string, Domain.Entity.Device>> Projection
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
                    MonitoringTime = entity.MonitoringTime,
                    EnableHealthCheck = entity.EnableHealthCheck,
                    HealthCheckMethodId = entity.HealthCheckMethod,
                    RetentionDays = entity.RetentionDays
                };
            }
        }

        public static Domain.Entity.Device Create(UpdateDevice model, string projectId)
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
