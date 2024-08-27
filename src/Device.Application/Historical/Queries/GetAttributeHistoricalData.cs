using System;
using System.Linq;
using Device.Application.Historical.Query;

namespace Device.Application.Device.Command
{
    public class GetAttributeHistoricalData : GetHistoricalData
    {
        public GetAttributeHistoricalData(Guid assetId, string metrics, long? timeStart, long? timeEnd, string timegrain, string aggregate, int timeout, string timezoneOffset, string gapfillFunction, int pageSize, int? quality = null) : base(timeStart, timeEnd, timegrain, aggregate, timeout, assetId, metrics.Split(",").Select(x => Guid.Parse(x)), false, HistoricalDataType.SERIES, timezoneOffset, gapfillFunction, pageSize, quality)
        {
        }
    }
}
