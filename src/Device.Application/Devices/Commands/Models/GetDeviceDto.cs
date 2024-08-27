using System;
using System.Linq.Expressions;
using Device.Application.Template.Command.Model;
using AHI.Infrastructure.Service.Tag.Model;
using System.Collections.Generic;
using System.Linq;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.Device.Command.Model
{
    public class GetDeviceDto : TagDtos
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TelemetryTopic { get; set; }
        public string CommandTopic { get; set; }
        public bool? HasCommand { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string DeploymentStatus { get; set; }
        public bool EnableHealthCheck { get; set; }
        public int? HealthCheckMethod { get; set; }
        public int? MonitoringTime { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int RetentionDays { get; set; }
        public DateTime? Timestamp { get; set; }
        public DateTime? CommandDataTimestamp { get; set; }
        public GetTemplateDto Template { get; set; }
        public string DeviceContent { get; set; }
        public string CreatedBy { get; set; }

        private static Func<Domain.Entity.Device, GetDeviceDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.Device, GetDeviceDto>> Projection
        {
            get
            {
                return entity => new GetDeviceDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TelemetryTopic = entity.TelemetryTopic,
                    CommandTopic = entity.CommandTopic,
                    HasCommand = entity.HasCommand,
                    DeploymentStatus = entity.DeviceSnaphot != null ? entity.DeviceSnaphot.Status : null,
                    Status = entity.Status,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Template = GetTemplateDto.Create(entity.Template),
                    Description = entity.Description,
                    Timestamp = entity.DeviceSnaphot != null ? entity.DeviceSnaphot.Timestamp : null,
                    CommandDataTimestamp = entity.DeviceSnaphot != null ? entity.DeviceSnaphot.CommandDataTimestamp : null,
                    RetentionDays = entity.RetentionDays,
                    DeviceContent = entity.DeviceContent,
                    EnableHealthCheck = entity.EnableHealthCheck,
                    HealthCheckMethod = entity.HealthCheckMethodId,
                    MonitoringTime = entity.MonitoringTime,
                    CreatedBy = entity.CreatedBy,
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetDeviceDto Create(Domain.Entity.Device entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
    }

    public class GetDeviceSimpleDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        private static Expression<Func<Domain.Entity.Device, GetDeviceSimpleDto>> Projection
        {
            get
            {
                return entity => new GetDeviceSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description
                };
            }
        }

        public static GetDeviceSimpleDto Create(Domain.Entity.Device entity)
        {
            if (entity == null)
                return null;
            return Projection.Compile().Invoke(entity);
        }
    }
}
