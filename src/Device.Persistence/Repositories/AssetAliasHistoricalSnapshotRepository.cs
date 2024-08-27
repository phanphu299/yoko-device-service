using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.Extensions;

namespace Device.Persistence.Repository
{
    public class AssetAliasHistoricalSnapshotRepository : IAssetAliasHistoricalSnapshotRepository
    {
        private readonly ILoggerAdapter<AssetAliasHistoricalSnapshotRepository> _logger;
        private readonly IAssetRepository _assetRepository;
        private readonly IAssetHistoricalSnapshotRepository _assetHistoricalSnapshotRepository;
        private readonly IAssetRuntimeHistoricalSnapshotRepository _runtimeHistoricalSnapshotRepository;
        private readonly IAssetSnapshotRepository _snapshotRepository;

        public AssetAliasHistoricalSnapshotRepository(ILoggerAdapter<AssetAliasHistoricalSnapshotRepository> logger
                                                        , IAssetRepository assetRepository
                                                        , IAssetHistoricalSnapshotRepository assetHistoricalSnapshotRepository
                                                        , IAssetRuntimeHistoricalSnapshotRepository runtimeHistoricalSnapshotRepository
                                                        , IAssetSnapshotRepository snapshotRepository)
        {
            _logger = logger;
            _assetRepository = assetRepository;
            _assetHistoricalSnapshotRepository = assetHistoricalSnapshotRepository;
            _runtimeHistoricalSnapshotRepository = runtimeHistoricalSnapshotRepository;
            _snapshotRepository = snapshotRepository;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction)
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
            var runtimeAttributes = newMetrics.Where(x => (new[] { AttributeTypeConstants.TYPE_RUNTIME }).Contains(x.AttributeType));
            if (runtimeAttributes.Any())
            {
                tasks.Add(_runtimeHistoricalSnapshotRepository.QueryDataAsync(timezoneOffset, timeStart, timeEnd, runtimeAttributes, timeout, gapfillFunction).HandleResult<TimeSeries>());
            }

            var staticSnapshotAttributes = newMetrics.Where(x => (new[] { AttributeTypeConstants.TYPE_STATIC }).Contains(x.AttributeType));
            if (staticSnapshotAttributes.Any())
            {
                _logger.LogInformation($"Static is not supported historical data");
                // get snapshot instead
                tasks.Add(_snapshotRepository.QueryDataAsync(timezoneOffset, staticSnapshotAttributes, timeout));
            }

            var dynamicAttributes = newMetrics.Where(x => (new[] { AttributeTypeConstants.TYPE_DYNAMIC }).Contains(x.AttributeType));
            if (dynamicAttributes.Any())
            {
                tasks.Add(_assetHistoricalSnapshotRepository.QueryDataAsync(timezoneOffset, timeStart, timeEnd, dynamicAttributes, timeout, gapfillFunction).HandleResult<TimeSeries>());
            }

            var result = await Task.WhenAll(tasks);
            var timeseries = result.SelectMany(x => x);
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
    }
}
