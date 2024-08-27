using System.Linq;

namespace Device.Application.Device.Command.Model
{
    public class FetchDeviceMetricDto
    {
        public int MetricId { get; set; }
        public string MetricKey { get; set; }
        public string MetricName { get; set; }
        public GetDeviceDto Device { get; set; }

        public static FetchDeviceMetricDto Create(Domain.Entity.TemplateDetail entity)
        {
            if (entity == null)
                return null;

            return new FetchDeviceMetricDto
            {
                MetricId = entity.Id,
                MetricKey = entity.Key,
                MetricName = entity.Name,
                Device = GetDeviceDto.Create(entity.Payload.Template.Devices.FirstOrDefault())
            };
        }
    }
}