using System;
using System.Linq.Expressions;
using Device.Application.Constant;

namespace Device.Application.Asset.Command.Model
{
    public class SnapshotDto
    {
        static Func<Domain.Entity.AttributeSnapshot, SnapshotDto> SnapshotConverter = Projection.Compile();
        static Func<Domain.Entity.DeviceMetricSnapshotInfo, SnapshotDto> MetricConverter = MetricProjection.Compile();
        static Func<Domain.Entity.AssetAttribute, SnapshotDto> AttributeConverter = AttributeProjection.Compile();
        public string Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public string DataType { get; set; }
        public DateTime? Timestamp { get; set; }

        private static Expression<Func<Domain.Entity.AttributeSnapshot, SnapshotDto>> Projection
        {
            get
            {
                return model => new SnapshotDto
                {
                    Id = model.Id.ToString(),
                    // Name = model.Name,
                    Value = ParseValue(model),
                    Timestamp = model.Timestamp
                };
            }
        }

        private static Expression<Func<Domain.Entity.DeviceMetricSnapshotInfo, SnapshotDto>> MetricProjection
        {
            get
            {
                return model => new SnapshotDto
                {
                    Id = model.Id.ToString(),
                    Name = model.Name,
                    Value = string.Equals(model.DataType, DataTypeConstants.TYPE_BOOLEAN, StringComparison.InvariantCultureIgnoreCase) ? Convert.ToBoolean(model.Value) as object : string.Equals(model.DataType, DataTypeConstants.TYPE_DOUBLE, StringComparison.InvariantCultureIgnoreCase) ? Convert.ToDouble(model.Value) as object : model.Value,
                    Timestamp = model.UpdatedUtc
                };
            }
        }

        private static Expression<Func<Domain.Entity.AssetAttribute, SnapshotDto>> AttributeProjection
        {
            get
            {
                return model => new SnapshotDto
                {
                    Id = model.Id.ToString(),
                    Name = model.Name,
                    Value = model.Value,
                    Timestamp = model.UpdatedUtc
                };
            }
        }
        public static SnapshotDto Create(Domain.Entity.AttributeSnapshot model)
        {
            if (model != null)
            {
                return SnapshotConverter(model);
            }
            return null;
        }
        public static SnapshotDto Create(Domain.Entity.DeviceMetricSnapshotInfo model)
        {
            if (model != null)
            {
                return MetricConverter(model);
            }
            return null;
        }

        public static SnapshotDto Create(Domain.Entity.AssetAttribute model)
        {
            if (model != null)
            {
                return AttributeConverter(model);
            }
            return null;
        }

        private static object ParseValue(Domain.Entity.AttributeSnapshot snapshot)
        {
            if (snapshot.Value.Length == 0)
                return snapshot.Value;

            try
            {
                if (DataTypeConstants.TYPE_BOOLEAN.Equals(snapshot.DataType, StringComparison.InvariantCultureIgnoreCase))
                    return Convert.ToBoolean(snapshot.Value);

                if (DataTypeConstants.TYPE_DOUBLE.Equals(snapshot.DataType, StringComparison.InvariantCultureIgnoreCase))
                    return Convert.ToDouble(snapshot.Value);

                if (DataTypeConstants.TYPE_INTEGER.Equals(snapshot.DataType, StringComparison.InvariantCultureIgnoreCase))
                    return (int)Convert.ToDouble(snapshot.Value);

                return snapshot.Value;
            }
            catch (System.Exception)
            {
                return String.Empty;
            }
        }
    }
}
