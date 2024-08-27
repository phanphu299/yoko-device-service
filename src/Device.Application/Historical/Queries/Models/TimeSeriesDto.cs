using System;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;
using MessagePack;

namespace Device.Application.Historical.Query.Model
{
    [MessagePackObject]
    public class TimeSeriesDto
    {
        static Func<string, Domain.Entity.TimeSeries, bool, TimeSeriesDto> Converter = Projection.Compile();
        [Key("ts")]
        public long ts { get; set; }
        [Key("v")]
        public object v { get; set; }
        [Key("l")]
        public object l { get; set; }
        [Key("lts")]
        public long lts { get; set; }
        [Key("q")]
        public int? q { get; set; }
        private static Expression<Func<string, Domain.Entity.TimeSeries, bool, TimeSeriesDto>> Projection
        {
            get
            {
                return (dataType, entity, isRawData) => new TimeSeriesDto
                {
                    q = entity.SignalQualityCode,
                    ts = entity.UnixTimestamp,
                    lts = entity.LastGoodUnixTimestamp,
                    v = entity.ValueBoolean != null ? entity.ValueBoolean : entity.Value.ParseValueWithDataType(dataType, entity.ValueText, isRawData),
                    l = entity.LastGoodValueBoolean != null ? entity.LastGoodValueBoolean : entity.LastGoodValue.ParseValueWithDataType(dataType, entity.LastGoodValueText, isRawData)
                };
            }
        }

        public static TimeSeriesDto Create(string dataType, Domain.Entity.TimeSeries entity, bool isRawData)
        {
            if (entity == null)
                return null;
            return Converter(dataType, entity, isRawData);
        }
    }
}
