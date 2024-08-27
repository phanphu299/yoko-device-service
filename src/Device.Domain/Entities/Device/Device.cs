using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class Device : IEntity<string>
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
        public DeviceTemplate Template { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string DeviceContent { get; set; }
        public bool EnableHealthCheck { get; set; }
        public int? SignalQualityCode { get; set; }
        public int? MonitoringTime { get; set; }
        public int? HealthCheckMethodId { get; set; }
        public DeviceSnapshot DeviceSnaphot { get; set; }
        public string ResourcePath { get; set; }
        public bool Deleted { get; set; }
        public string CreatedBy { get; set; }
        public virtual ICollection<EntityTagDb> EntityTags { get; set; }

        public Device()
        {
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            EntityTags ??= new List<EntityTagDb>();
        }

        public Device CloneAndUpdate(Device updatedDevice)
        {
            return new Device()
            {
                Id = this.Id,
                Name = updatedDevice.Name,
                TelemetryTopic = updatedDevice.TelemetryTopic,
                CommandTopic = updatedDevice.CommandTopic,
                HasCommand = updatedDevice.HasCommand,
                Description = updatedDevice.Description,
                Status = updatedDevice.Status,
                TemplateId = this.TemplateId,
                RetentionDays = updatedDevice.RetentionDays,
                CreatedUtc = this.CreatedUtc,
                UpdatedUtc = DateTime.UtcNow,
                DeviceContent = updatedDevice.DeviceContent,
                EnableHealthCheck = updatedDevice.EnableHealthCheck,
                SignalQualityCode = this.SignalQualityCode,
                MonitoringTime = updatedDevice.MonitoringTime,
                HealthCheckMethodId = updatedDevice.HealthCheckMethodId,
                ResourcePath = this.ResourcePath,
                Deleted = this.Deleted,
                CreatedBy = this.CreatedBy
            };
        }
    }
}
