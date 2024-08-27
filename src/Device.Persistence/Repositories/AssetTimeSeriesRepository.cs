
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using System.Linq;
using System.Data;

namespace Device.Persistence.Repository
{
    public class AssetTimeSeriesRepository : IAssetTimeSeriesRepository
    {
        private readonly IDeviceMetricTimeseriesRepository _deviceMetricTimeseriesRepository;

        public AssetTimeSeriesRepository(IDeviceMetricTimeseriesRepository deviceMetricTimeseriesRepository)
        {
            _deviceMetricTimeseriesRepository = deviceMetricTimeseriesRepository;
        }

        public async Task<double> AggregateAssetAttributesValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            var timeseries = await _deviceMetricTimeseriesRepository.AggregateDeviceMetricValueAsync(deviceMetric, start, end, aggregate, filterOperation, filterValue);
            return timeseries;
        }
        public Task<int> GetCountAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetCountDeviceMetricValueAsync(deviceMetric, start, end, filterOperation, filterValue);
        }

        public Task<double> GetDurationAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetDurationDeviceMetricValueAsync(deviceMetric, start, end, filterOperation, filterValue);
        }

        public async Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var deviceMetrics = metrics.Select(x => new DeviceMetric() { DeviceId = x.DeviceId, MetricKey = x.MetricKey, DataType = x.DataType });
            var histograms = await _deviceMetricTimeseriesRepository.GetHistogramAsync(timeStart, timeEnd, binSize, deviceMetrics, timezoneOffset, timegrain, aggregate, gapfillFunction);
            return (from metric in metrics
                    join histogram in histograms on new { metric.DeviceId, metric.MetricKey } equals new { histogram.DeviceId, histogram.MetricKey }
                    select new Histogram()
                    {
                        AttributeId = metric.AttributeId,
                        AssetId = metric.AssetId,
                        TotalBin = histogram.TotalBin,
                        ValueFrom = histogram.ValueFrom,
                        ValueTo = histogram.ValueTo,
                        Items = histogram.Items
                    });

        }

        public Task<double> GetLastTimeDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetLastTimeDiffDeviceMetricAsync(deviceMetric);
        }

        public Task<double> GetLastValueDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetLastValueDiffDeviceMetricAsync(deviceMetric);
        }
        /*
         Not sure timescale supports timebucket for text or not. 
         If not -> please use the sql below
        */


        public async Task<TimeSeries> GetNearestAssetAttributeAsync(DateTime dateTime, HistoricalEntity assetAttribute, string padding)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            var timeseries = await _deviceMetricTimeseriesRepository.GetNearestValueDeviceMetricAsync(dateTime, deviceMetric, padding);
            return timeseries != null ? new TimeSeries()
            {
                AttributeId = assetAttribute.AttributeId,
                AssetId = assetAttribute.AssetId,
                Value = timeseries.Value,
                ValueText = timeseries.ValueText,
                DateTime = timeseries.DateTime,
            } : null;
        }

        public Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetTimeDiff2PointsDeviceMetricValueAsync(deviceMetric, start, end);
        }

        public Task<double> GetValueDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            var deviceMetric = new DeviceMetric()
            {
                DeviceId = assetAttribute.DeviceId,
                MetricKey = assetAttribute.MetricKey,
                DataType = assetAttribute.DataType
            };
            return _deviceMetricTimeseriesRepository.GetValueDiff2PointsDeviceMetricValueAsync(deviceMetric, start, end);
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit, int? quality = null)
        {
            var tasks = new List<Task<IEnumerable<TimeSeries>>>();
            var deviceMetrics = metrics.Select(x => new DeviceMetric() { DeviceId = x.DeviceId, MetricKey = x.MetricKey, DataType = x.DataType });
            var timeSeriesData = await _deviceMetricTimeseriesRepository.QueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, deviceMetrics, timeout, gapfillFunction, limit, quality);
            var data = (from metric in metrics
                        join timeseries in timeSeriesData on new { metric.DeviceId, metric.MetricKey } equals new { timeseries.DeviceId, timeseries.MetricKey }
                        select new TimeSeries()
                        {
                            AttributeId = metric.AttributeId,
                            AssetId = metric.AssetId,
                            Value = timeseries.Value,
                            ValueText = timeseries.ValueText,
                            UnixTimestamp = timeseries.UnixTimestamp,
                            DataType = metric.DataType,
                            LastGoodUnixTimestamp = timeseries.LastGoodUnixTimestamp,
                            LastGoodValue = timeseries.LastGoodValue,
                            LastGoodValueText = timeseries.LastGoodValueText,
                            SignalQualityCode = timeseries.SignalQualityCode
                        });
            return data;
        }

        public virtual async Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, HistoricalEntity historicalEntity, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
        {
            var metrics = new HistoricalEntity[] { historicalEntity };
            var deviceMetrics = metrics.Select(x => new DeviceMetric() { DeviceId = x.DeviceId, MetricKey = x.MetricKey, DataType = x.DataType });
            var queryData = await _deviceMetricTimeseriesRepository.PaginationQueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, deviceMetrics, timeout, gapfillFunction, pageIndex, pageSize, quality);
            var totalCount = queryData.TotalCount;
            var timeSeriesData = queryData.Series;

            var data =  from metric in metrics
                        join timeseries in timeSeriesData on new { metric.DeviceId, metric.MetricKey } equals new { timeseries.DeviceId, timeseries.MetricKey }
                        select new TimeSeries()
                        {
                            AttributeId = metric.AttributeId,
                            AssetId = metric.AssetId,
                            Value = timeseries.Value,
                            ValueText = timeseries.ValueText,
                            UnixTimestamp = timeseries.UnixTimestamp,
                            DataType = metric.DataType,
                            LastGoodUnixTimestamp = timeseries.LastGoodUnixTimestamp,
                            LastGoodValue = timeseries.LastGoodValue,
                            LastGoodValueText = timeseries.LastGoodValueText,
                            SignalQualityCode = timeseries.SignalQualityCode
                        };
            return (data, totalCount);
        }
        public async Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {

            var deviceMetrics = metrics.Select(x => new DeviceMetric() { DeviceId = x.DeviceId, MetricKey = x.MetricKey, DataType = x.DataType });
            var statistics = await _deviceMetricTimeseriesRepository.GetStatisticsAsync(timeStart, timeEnd, deviceMetrics, timezoneOffset, timegrain, aggregate, gapfillFunction);
            return (from metric in metrics
                    join stat in statistics on new { metric.DeviceId, metric.MetricKey } equals new { stat.DeviceId, stat.MetricKey }
                    select new Statistics()
                    {
                        AttributeId = metric.AttributeId,
                        AssetId = metric.AssetId,
                        Mean = stat.Mean,
                        STDev = stat.STDev,
                        Min = stat.Min,
                        Max = stat.Max,
                        Q1_Inc = stat.Q1_Inc,
                        Q2_Inc = stat.Q2_Inc,
                        Q3_Inc = stat.Q3_Inc,
                        Q1_Exc = stat.Q1_Exc,
                        Q2_Exc = stat.Q2_Exc,
                        Q3_Exc = stat.Q3_Exc
                    });
        }
    }
}