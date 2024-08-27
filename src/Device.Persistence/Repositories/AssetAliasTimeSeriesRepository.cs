
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using System.Linq;
using System.Data;
using Device.Persistence.Extensions;
using Device.Application.Constant;

namespace Device.Persistence.Repository
{
    public class AssetAliasTimeSeriesRepository : IAssetAliasTimeSeriesRepository
    {
        private readonly IAssetTimeSeriesRepository _timeSeriesRepository;

        private readonly IAssetRuntimeTimeSeriesRepository _assetRuntimeTimeSeriesRepository;
        private readonly IAssetRepository _assetRepository;

        public AssetAliasTimeSeriesRepository(IAssetTimeSeriesRepository snapshotTimeSeriesRepository,
             IAssetRuntimeTimeSeriesRepository assetRuntimeTimeSeriesRepository
             , IAssetRepository assetRepository)
        {
            _timeSeriesRepository = snapshotTimeSeriesRepository;
            _assetRepository = assetRepository;
            _assetRuntimeTimeSeriesRepository = assetRuntimeTimeSeriesRepository;
        }

        public async Task<double> AggregateAssetAttributesValueAsync(Domain.Entity.HistoricalEntity assetAttribute, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.AggregateAssetAttributesValueAsync(newAssetAttribute, start, end, aggregate, filterOperation, filterValue);
        }

        public async Task<int> GetCountAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetCountAssetAttributeValueAsync(newAssetAttribute, start, end, filterOperation, filterValue);
        }

        public async Task<double> GetDurationAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetDurationAssetAttributeValueAsync(newAssetAttribute, start, end, filterOperation, filterValue);
        }

        public async Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            // var referMetrics = new List<HistoricalEntityReference>();
            var targetAttributeTasks = metrics.Select(x => _assetRepository.FindTargetAttributeAsync(x.AttributeId));
            var targetAttributes = await Task.WhenAll(targetAttributeTasks);
            //task run TimeSeries
            var newMetrics = targetAttributes.Select(x => new HistoricalEntity()
            {
                AssetId = x.AssetId,
                AttributeId = x.TargetAttributeId,
                AttributeType = x.AttributeType,
                DataType = x.DataType,
                DeviceId = x.DeviceId,
                MetricKey = x.MetricKey
            });
            var timeseries = await _timeSeriesRepository.GetHistogramAsync(timeStart, timeEnd, binSize, newMetrics, timezoneOffset, timegrain, aggregate, gapfillFunction);
            // decorate the result
            return timeseries.Select(histogram =>
            {
                var mapping = targetAttributes.First(alias => alias.TargetAttributeId == histogram.AttributeId);
                var metric = metrics.First(metricInput => metricInput.AttributeId == mapping.AttributeId);
                return new Histogram()
                {
                    AssetId = metric.AssetId,
                    AttributeId = metric.AttributeId,
                    TotalBin = histogram.TotalBin,
                    ValueFrom = histogram.ValueFrom,
                    ValueTo = histogram.ValueTo,
                    Items = histogram.Items
                };
            });
        }

        public async Task<double> GetLastTimeDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetLastTimeDiffAssetAttributeAsync(newAssetAttribute);
        }

        public async Task<double> GetLastValueDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetLastValueDiffAssetAttributeAsync(newAssetAttribute);
        }

        public async Task<TimeSeries> GetNearestAssetAttributeAsync(DateTime dateTime, HistoricalEntity assetAttribute, string padding)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            var timeseries = await _timeSeriesRepository.GetNearestAssetAttributeAsync(dateTime, newAssetAttribute, padding);
            return new TimeSeries()
            {
                AssetId = assetAttribute.AssetId,
                AttributeId = assetAttribute.AttributeId,
                UnixTimestamp = timeseries.UnixTimestamp,
                Value = timeseries.Value,
                ValueText = timeseries.ValueText
            };
        }

        public async Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetTimeDiff2PointsAssetAttributeValueAsync(newAssetAttribute, start, end);
        }

        public async Task<double> GetValueDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            var targetAssetAttribute = await _assetRepository.FindTargetAttributeAsync(assetAttribute.AttributeId);
            var newAssetAttribute = new HistoricalEntity()
            {
                AssetId = targetAssetAttribute.AssetId,
                AttributeId = targetAssetAttribute.TargetAttributeId,
                AttributeType = targetAssetAttribute.AttributeType,
                DataType = targetAssetAttribute.DataType,
                DeviceId = targetAssetAttribute.DeviceId,
                MetricKey = targetAssetAttribute.MetricKey
            };
            return await _timeSeriesRepository.GetValueDiff2PointsAssetAttributeValueAsync(newAssetAttribute, start, end);
        }

        public async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit, int? quality = null)
        {
            var targetAttributeTasks = metrics.Select(x => _assetRepository.FindTargetAttributeAsync(x.AttributeId));
            var targetAttributes = await Task.WhenAll(targetAttributeTasks);
            //task run TimeSeries
            var newMetrics = targetAttributes.Select(x => new HistoricalEntity()
            {
                AssetId = x.AssetId,
                AttributeId = x.TargetAttributeId,
                AttributeType = x.AttributeType,
                DataType = x.DataType,
                DeviceId = x.DeviceId,
                MetricKey = x.MetricKey
            });
            var tasks = new List<Task<IEnumerable<TimeSeries>>>();
            var runtimeAttributes = newMetrics.Where(x => AttributeTypeConstants.TYPE_RUNTIME == x.AttributeType);
            if (runtimeAttributes.Any())
            {
                tasks.Add(_assetRuntimeTimeSeriesRepository.QueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, runtimeAttributes, timeout, gapfillFunction, limit).HandleResult<TimeSeries>());
            }
            var dynamicAttributes = newMetrics.Where(x => AttributeTypeConstants.TYPE_DYNAMIC == x.AttributeType);
            if (dynamicAttributes.Any())
            {
                tasks.Add(_timeSeriesRepository.QueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, dynamicAttributes, timeout, gapfillFunction, limit).HandleResult<TimeSeries>());
            }
            var result = await Task.WhenAll(tasks);
            var timeseries = result.SelectMany(x => x);
            // decorate the result
            var data = timeseries.Select(result =>
           {
               var mapping = targetAttributes.First(alias => alias.TargetAttributeId == result.AttributeId);
               var metric = metrics.First(metricInput => metricInput.AttributeId == mapping.AttributeId);
               return new TimeSeries()
               {
                   AssetId = metric.AssetId,
                   AttributeId = metric.AttributeId,
                   UnixTimestamp = result.UnixTimestamp,
                   Value = result.Value,
                   ValueText = result.ValueText,
                   DataType = metric.DataType
               };
           });

            return data;
        }
        
        public async Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var targetAttributeTasks = metrics.Select(x => _assetRepository.FindTargetAttributeAsync(x.AttributeId));
            var targetAttributes = await Task.WhenAll(targetAttributeTasks);
            //task run TimeSeries
            var newMetrics = targetAttributes.Select(x => new HistoricalEntity()
            {
                AssetId = x.AssetId,
                AttributeId = x.TargetAttributeId,
                AttributeType = x.AttributeType,
                DataType = x.DataType,
                DeviceId = x.DeviceId,
                MetricKey = x.MetricKey
            });
            var statistics = await _timeSeriesRepository.GetStatisticsAsync(timeStart, timeEnd,newMetrics, timezoneOffset, timegrain, aggregate, gapfillFunction);
            // decorate the result
            return statistics.Select(stat =>
            {
                var mapping = targetAttributes.First(alias => alias.TargetAttributeId == stat.AttributeId);
                var metric = metrics.First(metricInput => metricInput.AttributeId == mapping.AttributeId);
                return new Statistics()
                {
                    AssetId = metric.AssetId,
                    AttributeId = metric.AttributeId,
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
                };

            });
        }

        public async Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, HistoricalEntity historicalEntity, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
        {
            var targetAttribute = await _assetRepository.FindTargetAttributeAsync(historicalEntity.AttributeId);
            //task run TimeSeries

            (IEnumerable<TimeSeries> Series, int TotalCount) pagingData = (Array.Empty<TimeSeries>(), 0);
            if (targetAttribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME)
            {
                pagingData = await _assetRuntimeTimeSeriesRepository.PaginationQueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, historicalEntity, timeout, gapfillFunction, pageIndex, pageSize).HandleResult<TimeSeries>();
            }
            if (targetAttribute.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
            {
                pagingData = await _timeSeriesRepository.PaginationQueryDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, historicalEntity, timeout, gapfillFunction, pageIndex, pageSize).HandleResult<TimeSeries>();
            }
            return (pagingData.Series, pagingData.TotalCount);
        }
    }
}