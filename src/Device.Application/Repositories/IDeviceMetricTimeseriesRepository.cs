using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IDeviceMetricTimeseriesRepository
    {
        Task<DeviceMetricTimeseries> GetNearestValueDeviceMetricAsync(DateTime dateTime, Domain.Entity.DeviceMetric deviceMetric, string padding);
        Task<double> GetLastTimeDiffDeviceMetricAsync(Domain.Entity.DeviceMetric deviceMetric);
        Task<double> GetLastValueDiffDeviceMetricAsync(Domain.Entity.DeviceMetric deviceMetric);
        Task<double> GetTimeDiff2PointsDeviceMetricValueAsync(Domain.Entity.DeviceMetric deviceMetric, DateTime start, DateTime end);
        Task<double> GetValueDiff2PointsDeviceMetricValueAsync(Domain.Entity.DeviceMetric deviceMetric, DateTime start, DateTime end);
        Task<IEnumerable<DeviceMetricTimeseries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.DeviceMetric> metrics, int timeout, string gapfillFunction, int limit, int? quality = null);
        Task<double> AggregateDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue);
        Task<int> GetCountDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string filterOperation, object filterValue);
        Task<double> GetDurationDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string filterOperation, object filterValue);
        Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<DeviceMetric> deviceMetrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction);
        Task<(IEnumerable<DeviceMetricTimeseries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.DeviceMetric> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null);
        Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<DeviceMetric> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction);
    }
}
