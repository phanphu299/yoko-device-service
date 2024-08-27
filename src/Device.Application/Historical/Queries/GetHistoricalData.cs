using System;
using System.Collections.Generic;
using Device.Application.Constant;
using Device.Application.Historical.Query.Model;
using MediatR;

namespace Device.Application.Historical.Query
{
    public class GetHistoricalData : IRequest<IEnumerable<HistoricalDataDto>>
    {
        public Guid AssetId { get; set; }
        public int TimeoutInSecond { get; set; }
        public long TimeStart { get; set; }
        public long TimeEnd { get; set; }
        public string TimeGrain { get; set; }
        public bool IsRawData
        {
            get
            {
                return RequestType == HistoricalDataType.SNAPSHOT ? true : string.IsNullOrEmpty(TimeGrain);
            }
        }
        public string Aggregate { get; set; }
        public IEnumerable<Guid> AttributeIds { get; set; }
        public bool UseCustomTimeRange { get; set; }
        public HistoricalDataType RequestType { get; set; }
        public string TimezoneOffset { get; private set; } = "+00:00";
        public string GapfillFunction { get; private set; } = PostgresFunction.TIME_BUCKET_GAPFILL;
        public bool UseCache { get; set; } = true;
        public int PageSize { get; set; }
        public int? Quality { get; set; }

        public GetHistoricalData(Guid assetId, IEnumerable<Guid> attributeIds)
        {
            AssetId = assetId;
            AttributeIds = attributeIds;
            TimeStart = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
            TimeEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        public GetHistoricalData(long? timeStart, long? timeEnd, string timegrain, string aggregate, int timeout, Guid assetId, IEnumerable<Guid> attributeIds, bool useCustomeTimeRange, HistoricalDataType requestType, string timezoneOffset, string gapfillFunction, int pageSize, int? quality = null)
        {
            TimeStart = timeStart ?? DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
            TimeEnd = timeEnd ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            TimeGrain = timegrain;
            Aggregate = aggregate ?? "avg";
            AssetId = assetId;
            AttributeIds = attributeIds;
            TimeoutInSecond = timeout;
            UseCustomTimeRange = useCustomeTimeRange;
            RequestType = requestType;
            TimezoneOffset = timezoneOffset;
            GapfillFunction = gapfillFunction;
            PageSize = pageSize;
            Quality = quality;
        }
    }
}
