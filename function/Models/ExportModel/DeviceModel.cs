using System;
using System.Collections.Generic;

namespace AHI.Device.Function.Model.ExportModel
{
    public class DeviceModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tags { get; set; }
        public ICollection<long> TagIds { get; set; } = new List<long>();
        public string TelemetryTopic { get; set; }
        public string CommandTopic { get; set; }
        public bool? HasCommand { get; set; }
        public string Template { get; set; }
        public DateTime? TimeStampValue { get; set; }
        public string TimeStamp { get; set; }
        public int Metrics { get; set; }
        public string RetentionDays { get; set; }
        public string DeviceContent { get; set; }
        public Guid? BrokerId { get; set; }
        public string BrokerProjectId { get; set; }
        public string BrokerName { get; set; }
        public string PrimaryKey { get; set; }
        public int SasTokenDuration { get; set; }
        public int TokenDuration { get; set; }

        private string _deviceStatus = string.Empty;

        public string Status
        {
            get => _deviceStatus switch
            {
                Constant.Status.DEVICE_ACTIVE => Constant.Status.ACTIVE,
                Constant.Status.DEVICE_CREATED => Constant.Status.CREATED,
                Constant.Status.DEVICE_REGISTERED => Constant.Status.REGISTERED,
                _ => "N/A"
            };
            set => _deviceStatus = value;
        }
    }
}
