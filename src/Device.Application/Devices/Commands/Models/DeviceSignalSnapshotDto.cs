using System;
using System.Linq.Expressions;

namespace Device.Application.Device.Command.Model
{
    public class DeviceSignalSnapshotDto
    {
        public string ResourceId { get; set; }
        public string SignalId { get; set; }
        public string Value { get; set; }
        public DateTime UpdatedUtc { get; set; }
        static Func<Domain.Entity.DeviceMetricSnapshot, DeviceSignalSnapshotDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.DeviceMetricSnapshot, DeviceSignalSnapshotDto>> Projection
        {
            get
            {
                return entity => new DeviceSignalSnapshotDto
                {
                    ResourceId = entity.DeviceId,
                    SignalId = entity.MetricId,
                    Value = entity.Value,
                    UpdatedUtc = entity.UpdatedUtc
                };
            }
        }

        public static DeviceSignalSnapshotDto Create(Domain.Entity.DeviceMetricSnapshot entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
    }
}