using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using System.Collections.Generic;
using System.Linq;

namespace Device.Application.Device.Command.Model
{
    public class ArchiveDeviceDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TelemetryTopic { get; set; }
        public string CommandTopic { get; set; }
        public bool? HasCommand { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public Guid TemplateId { get; set; }
        public int RetentionDays { get; set; }
        public string DeviceContent { get; set; }
        public bool EnableHealthCheck { get; set; }
        public int? SignalQualityCode { get; set; }
        public int? MonitoringTime { get; set; }
        public int? HealthCheckMethodId { get; set; }
        public string ResourcePath { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public IEnumerable<EntityTag> EntityTags { get; set; }
        static Func<Domain.Entity.Device, ArchiveDeviceDto> DtoConverter = DtoProjection.Compile();

        private static Expression<Func<Domain.Entity.Device, ArchiveDeviceDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveDeviceDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TelemetryTopic = entity.TelemetryTopic,
                    CommandTopic = entity.CommandTopic,
                    HasCommand = entity.HasCommand,
                    Description = entity.Description,
                    Status = entity.Status,
                    TemplateId = entity.TemplateId,
                    RetentionDays = entity.RetentionDays,
                    DeviceContent = entity.DeviceContent,
                    EnableHealthCheck = entity.EnableHealthCheck,
                    SignalQualityCode = entity.SignalQualityCode,
                    MonitoringTime = entity.MonitoringTime,
                    HealthCheckMethodId = entity.HealthCheckMethodId,
                    ResourcePath = entity.ResourcePath,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    EntityTags = entity.EntityTags != null ? entity.EntityTags.OrderBy(x => x.Id).Select(x => new Domain.Entity.EntityTagDb
                    {
                        Id = x.Id,
                        TagId = x.TagId,
                        EntityType = x.EntityType,
                        EntityIdString = x.EntityIdString,
                        EntityIdLong = x.EntityIdLong,
                        EntityIdGuid = x.EntityIdGuid,
                        EntityIdInt = x.EntityIdInt
                    }).ToList() : new List<Domain.Entity.EntityTagDb>()
                };
            }
        }

        public static ArchiveDeviceDto CreateDto(Domain.Entity.Device model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        static Func<ArchiveDeviceDto, Domain.Entity.Device> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<ArchiveDeviceDto, Domain.Entity.Device>> EntityProjection
        {
            get
            {
                return model => new Domain.Entity.Device
                {
                    Id = model.Id,
                    Name = model.Name,
                    TelemetryTopic = model.TelemetryTopic,
                    CommandTopic = model.CommandTopic,
                    HasCommand = model.HasCommand,
                    Description = model.Description,
                    Status = model.Status,
                    TemplateId = model.TemplateId,
                    RetentionDays = model.RetentionDays,
                    DeviceContent = model.DeviceContent,
                    EnableHealthCheck = model.EnableHealthCheck,
                    SignalQualityCode = model.SignalQualityCode,
                    MonitoringTime = model.MonitoringTime,
                    HealthCheckMethodId = model.HealthCheckMethodId,
                    ResourcePath = model.ResourcePath,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    EntityTags = model.EntityTags != null ? model.EntityTags.OrderBy(x => x.Id).Select(x => new Domain.Entity.EntityTagDb
                    {
                        Id = x.Id,
                        TagId = x.TagId,
                        EntityType = x.EntityType,
                        EntityIdString = x.EntityIdString,
                        EntityIdLong = x.EntityIdLong,
                        EntityIdGuid = x.EntityIdGuid,
                        EntityIdInt = x.EntityIdInt
                    }).ToList() : new List<Domain.Entity.EntityTagDb>()
                };
            }
        }

        public static Domain.Entity.Device CreateEntity(ArchiveDeviceDto model)
        {
            if (model != null)
            {
                return EntityConverter(model);
            }
            return null;
        }
    }
}
