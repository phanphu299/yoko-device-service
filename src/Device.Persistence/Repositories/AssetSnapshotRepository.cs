using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Dapper;
using Npgsql;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.Extensions;

namespace Device.Persistence.Repository
{
    public class AssetSnapshotRepository : IAssetSnapshotRepository
    {
        private readonly ILoggerAdapter<AssetSnapshotRepository> _logger;
        private readonly IDbConnectionResolver _dbConnectionResolver;


        public AssetSnapshotRepository(ILoggerAdapter<AssetSnapshotRepository> logger, IDbConnectionResolver dbConnectionResolver)
        {
            _logger = logger;
            _dbConnectionResolver = dbConnectionResolver;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, IEnumerable<HistoricalEntity> metrics, int timeout)
        {
            // for snapshot -> only get first asset
            var start = DateTime.UtcNow;
            IEnumerable<HistoricalEntity> staticSnapshotAttributes = metrics.Where(x => (new[] { AttributeTypeConstants.TYPE_STATIC }).Contains(x.AttributeType));
            IEnumerable<HistoricalEntity> commandSnapshotAttributes = metrics.Where(x => (new[] { AttributeTypeConstants.TYPE_COMMAND }).Contains(x.AttributeType));
            IEnumerable<HistoricalEntity> dynamicSnapshotAttributes = metrics.Where(x => (new[] { AttributeTypeConstants.TYPE_DYNAMIC }).Contains(x.AttributeType));
            IEnumerable<HistoricalEntity> integrationSnapshotAttributes = metrics.Where(x => (new[] { AttributeTypeConstants.TYPE_INTEGRATION }).Contains(x.AttributeType));
            IEnumerable<HistoricalEntity> runtimeSnapshotAttributes = metrics.Where(x => (new[] { AttributeTypeConstants.TYPE_RUNTIME }).Contains(x.AttributeType));

            using (var connection = GetDbConnection())
            {
                var sql = BuildSnapshotQuery();
                IEnumerable<TimeSeries> result = await connection.QueryAsync<TimeSeries>(sql, new
                {
                    currentAssetId = Guid.Empty, // use this field and hasAssetId in the future to improve the performance
                    commandAttributeIds = commandSnapshotAttributes.Select(x => x.AttributeId).ToArray(),
                    dynamicAttributeIds = dynamicSnapshotAttributes.Select(x => x.AttributeId).ToArray(),
                    integrationAttributeIds = integrationSnapshotAttributes.Select(x => x.AttributeId).ToArray(),
                    runtimeAttributeIds = runtimeSnapshotAttributes.Select(x => x.AttributeId).ToArray(),
                    staticAttributeIds = staticSnapshotAttributes.Select(x => x.AttributeId).ToArray(),
                    hasAssetId = false, // use this field in the future to improve the performance
                    hascommandAttributeIds = commandSnapshotAttributes.Any(),
                    hasDynamicAttributeIds = dynamicSnapshotAttributes.Any(),
                    hasIntegrationAttributeIds = integrationSnapshotAttributes.Any(),
                    hasRuntimeAttributeIds = runtimeSnapshotAttributes.Any(),
                    hasStaticAttributeIds = staticSnapshotAttributes.Any()
                }, commandTimeout: 30) // timeout after 30s for now, do not apply the timeout for snapshot and historical snapshot
                .HandleResult<TimeSeries>();

                connection.Close();
                _logger.LogTrace($"Query from database take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");

                // data returns:
                /*
                    asset_id	                            attribute_id	                        data_type	_ts	                value	            signal_quality_code	   row_num
                    e1627e07-7ae1-45fd-85e9-0cbf7948ca69	3c412678-9011-4a01-88ff-005a9be78b62	double	    2022-09-17T02:16:10	18.708294685328518	192                    -1
                    34b9ee44-a25e-4127-ac56-dd4f29204cf2	b20a95e4-08d3-49fd-9951-e7e2fa93939f	double	    2022-08-26T23:59:50	29.00936297560547	192                    1
                    e1627e07-7ae1-45fd-85e9-0cbf7948ca69	3c412678-9011-4a01-88ff-005a9be78b62	double	    2022-08-26T23:59:50	59.023957866720835	192                    1
                    34b9ee44-a25e-4127-ac56-dd4f29204cf2	b20a95e4-08d3-49fd-9951-e7e2fa93939f	double	    2022-09-17T02:16:10	54.34058660377775	192                    -1
                */
                IEnumerable<TimeSeries> timeSeries = result.GroupBy(x => new { x.AssetId, x.AttributeId, x.DataType }).Select(x =>
                {
                    var timeseriesData = x.OrderByDescending(x => x.RowNum).First();
                    if (DataTypeConstants.NUMBERIC_TYPES.Contains(x.Key.DataType))
                    {
                        TryParseTimeSeriesNumericData(timeseriesData, x.Key.DataType);
                    }
                    return timeseriesData;
                });

                return timeSeries;
            }
        }

        private void TryParseTimeSeriesNumericData(TimeSeries timeseriesData, string dataType)
        {
            if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                timeseriesData.ValueBoolean = TimeSeries.ParseTimeseriesBoolean(timeseriesData.ValueText);
                timeseriesData.LastGoodValueBoolean = TimeSeries.ParseTimeseriesBoolean(timeseriesData.LastGoodValueText);
            }
            else
            {
                // parse to double
                if (double.TryParse(timeseriesData.ValueText, out var doubleValue))
                {
                    timeseriesData.Value = doubleValue;
                }

                if (double.TryParse(timeseriesData.LastGoodValueText, out var lastGoodDoubleValue))
                {
                    timeseriesData.LastGoodValue = lastGoodDoubleValue;
                }
            }
        }

        private static string BuildSnapshotQuery()
        {
            return @$"select datatype as DataType
                        , assetid as AssetId
                        , attributeid as AttributeId
                        , unixtimestamp as UnixTimestamp
                        , valuetext as ValueText
                        , signalqualitycode as SignalQualityCode
                        , lastgoodvaluetext as LastGoodValueText
                        , lastgoodunixtimestamp as LastGoodUnixTimestamp
                        from fn_get_snapshot_with_time_series(@currentAssetId, @commandAttributeIds, @dynamicAttributeIds, @integrationAttributeIds, @runtimeAttributeIds, @staticAttributeIds, @hasAssetId, @hascommandAttributeIds, @hasDynamicAttributeIds, @hasIntegrationAttributeIds, @hasRuntimeAttributeIds, @hasStaticAttributeIds)
                        ";
        }

        protected IDbConnection GetDbConnection() => _dbConnectionResolver.CreateConnection(isReadOnly: true);
    }
}
