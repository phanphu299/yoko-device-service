
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Linq;
using System.Data;
using Device.Application.Constant;
using Device.ApplicationExtension.Extension;
using Dapper;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Newtonsoft.Json;
using Device.Persistence.Extensions;
using Device.Application.DbConnections;
using Device.Application.Helper;

namespace Device.Persistence.Repository
{
    public class AssetRuntimeTimeSeriesRepository : IAssetRuntimeTimeSeriesRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly int LIMIT;
        private readonly ILoggerAdapter<AssetRuntimeTimeSeriesRepository> _logger;
        private readonly IDbConnectionResolver _dbConnectionResolver;
        public AssetRuntimeTimeSeriesRepository(IConfiguration configuration,
            ITenantContext tenantContext,
            ILoggerAdapter<AssetRuntimeTimeSeriesRepository> logger,
            IDbConnectionResolver dbConnectionResolver)

        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            LIMIT = Convert.ToInt32(_configuration["TimeseriesLimit"] ?? "20000");
            _logger = logger;
            _dbConnectionResolver = dbConnectionResolver;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit, int? quality = null)
        {
            var tasks = new List<Task<IEnumerable<TimeSeries>>>();
            var numbericTypeAttributes = metrics.Where(x => DataTypeConstants.NUMBERIC_TYPES.Contains(x.DataType));
            var textTypeAttributes = metrics.Where(x => DataTypeConstants.TEXT_TYPES.Contains(x.DataType));
            tasks.Add(QueryNumbericSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, numbericTypeAttributes, timeout, gapfillFunction, limit).HandleResult<TimeSeries>());
            tasks.Add(QueryTextSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, textTypeAttributes, timeout, gapfillFunction, limit).HandleResult<TimeSeries>());
            var result = await Task.WhenAll(tasks);

            var timeSeriesData = result.SelectMany(x => x);
            var data = (from metric in metrics
                        join timeseries in timeSeriesData on new { metric.AssetId, metric.AttributeId } equals new { timeseries.AssetId, timeseries.AttributeId }
                        select new TimeSeries()
                        {
                            AttributeId = metric.AttributeId,
                            AssetId = metric.AssetId,
                            Value = timeseries.Value,
                            ValueText = timeseries.ValueText,
                            UnixTimestamp = timeseries.UnixTimestamp,
                            DataType = metric.DataType
                        });

            return data;
        }

        public virtual async Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, HistoricalEntity historicalEntity, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
        {
            var metrics = new HistoricalEntity[] { historicalEntity };
            var tasks = new List<Task<(IEnumerable<TimeSeries> Series, int TotalCount)>>();
            if (DataTypeConstants.NUMBERIC_TYPES.Contains(historicalEntity.DataType))
            {
                tasks.Add(PaginationQueryNumbericSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, metrics, timeout, gapfillFunction, pageIndex, pageSize).HandleResult<TimeSeries>());
            }
            else if (DataTypeConstants.TEXT_TYPES.Contains(historicalEntity.DataType))
            {
                tasks.Add(PaginationQueryTextSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, metrics, timeout, gapfillFunction, pageIndex, pageSize).HandleResult<TimeSeries>());
            }

            var result = await Task.WhenAll(tasks);
            var totalCount = result.First().TotalCount;
            var timeSeriesData = result.SelectMany(x => x.Series);
            var data = from metric in metrics
                       join timeseries in timeSeriesData on new { metric.AssetId, metric.AttributeId } equals new { timeseries.AssetId, timeseries.AttributeId }
                       select new TimeSeries()
                       {
                           AttributeId = metric.AttributeId,
                           AssetId = metric.AssetId,
                           Value = timeseries.Value,
                           ValueText = timeseries.ValueText,
                           UnixTimestamp = timeseries.UnixTimestamp,
                           DataType = metric.DataType
                       };

            return (data, totalCount);
        }

        private async Task<IEnumerable<TimeSeries>> QueryNumbericSeriesDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit)
        {
            limit = limit > 0 ? limit : LIMIT;
            //var recordLimit = limit * metrics.Count();
            var sql = @$"select * from (select
                        msf.asset_id as AssetId,
                        msf.asset_attribute_id as AttributeId,
                        extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                        msf.value as Value,
                        row_number () over (partition by asset_id, asset_attribute_id order by _ts desc) as RowNum
                    FROM asset_attribute_runtime_series msf
                    WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                and msf._ts >= @Start
                                and msf._ts <= @End
                    --order by msf._ts desc
                    ) s
                    where s.RowNum <= {limit}";
            if (!string.IsNullOrEmpty(timegrain) && TimeSeriesSqlHandler.ContainsKey(gapfillFunction))
            {
                sql = TimeSeriesSqlHandler[gapfillFunction].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, limit));
            }

            using (var connection = GetDbConnection())
            {
                try
                {
                    var timeSeriesData = await connection.QueryAsync<TimeSeries>(sql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, commandTimeout: timeout);
                    return timeSeriesData;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private async Task<IEnumerable<TimeSeries>> QueryTextSeriesDataAsync(string timezoneId, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit)
        {
            /*
             Not sure timescale supports timebucket for text or not.
             If not -> please use the sql below
            */
            limit = limit > 0 ? limit : LIMIT;
            //var recordLimit = limit * metrics.Count();
            string sql = @$"select * from (SELECT
                            msf.asset_id as AssetId,
                            msf.asset_attribute_id as AttributeId,
                            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                            msf.value as ValueText,
                            row_number () over (partition by asset_id, asset_attribute_id order by _ts desc) as RowNum
                            FROM asset_attribute_runtime_series_text msf
                            WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                    and msf._ts >= @Start
                                    and msf._ts <= @End
                            --order by  msf._ts desc
                        ) s
                        where s.RowNum <= {limit}";

            using (var connection = GetDbConnection())
            {
                try
                {
                    var timeSeriesData = await connection.QueryAsync<TimeSeries>(sql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, commandTimeout:
                timeout);
                    return timeSeriesData;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected IDbConnection GetDbConnection() => _dbConnectionResolver.CreateConnection(true);
        protected IDbConnection GetReadWriteDbConnection() => _dbConnectionResolver.CreateConnection();

        public async Task SaveAssetAttributeValueAsync(params TimeSeries[] timeSeries)
        {
            using (var dbConnection = GetReadWriteDbConnection())
            {

                var assetIds = timeSeries.Select(x => x.AssetId).Distinct().ToArray();
                var attributeIds = timeSeries.Select(x => x.AttributeId).Distinct().ToArray();
                var queryAttributeRuntime = @"select count(attribute_id) from v_asset_attribute_runtimes where attribute_id = ANY(@AttributeIds) and enabled_expression = false;";
                var attributeRuntimeCount = await dbConnection.QuerySingleOrDefaultAsync<int>(queryAttributeRuntime, new { AttributeIds = attributeIds });
                if (attributeIds.Length != attributeRuntimeCount)
                {
                    _logger.LogError($"Attributes: {string.Join(",", attributeIds)}");
                    throw new EntityInvalidException(detailCode: MessageConstants.ASSET_ATTRIBUTE_RUNTIME_USED_EXPRESSION);
                }
                var queryRetentionDay = @"select id, retention_days from assets where id = ANY(@AssetIds)";
                var snapshotValues = timeSeries.GroupBy(x => new { x.AssetId, x.AttributeId }).Select(asset =>
                  {
                      var snapshotValue = asset.OrderByDescending(x => x.DateTime).First();
                      return new
                      {
                          AssetId = asset.Key.AssetId,
                          AttributeId = asset.Key.AttributeId,
                          Timestamp = snapshotValue.DateTime,
                          Value = snapshotValue.ValueText ?? snapshotValue.Value?.ToString()
                      };
                  }).ToList();
                var retentionDays = await dbConnection.QueryAsync<(Guid, int)>(queryRetentionDay, new { AssetIds = assetIds });

                await dbConnection.ExecuteAsync($@"INSERT INTO asset_attribute_runtime_snapshots(_ts, asset_id, asset_attribute_id, value)
                                                VALUES(@Timestamp, @AssetId, @AttributeId, @Value)
                                                ON CONFLICT (asset_id, asset_attribute_id)
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE asset_attribute_runtime_snapshots._ts < EXCLUDED._ts;
                                                ", snapshotValues);
                var numericValues = timeSeries.Where(x => x.Value != null).Select(x => new
                {
                    Timestamp = x.DateTime,
                    AssetId = x.AssetId,
                    AttributeId = x.AttributeId,
                    Value = x.Value,
                    RetentionDays = retentionDays.First(a => a.Item1 == x.AssetId).Item2
                });
                if (numericValues.Any())
                {
                    await dbConnection.ExecuteAsync(GetInsertAssetAttributeCommand("asset_attribute_runtime_series"), numericValues);
                }
                var textValues = timeSeries.Where(x => !string.IsNullOrEmpty(x.ValueText)).Select(x => new
                {
                    Timestamp = x.DateTime,
                    AssetId = x.AssetId,
                    AttributeId = x.AttributeId,
                    Value = x.ValueText, // should be a string
                    RetentionDays = retentionDays.First(a => a.Item1 == x.AssetId).Item2
                });
                if (textValues.Any())
                {
                    await dbConnection.ExecuteAsync(GetInsertAssetAttributeCommand("asset_attribute_runtime_series_text"), textValues);
                }
                dbConnection.Close();
            }
        }

        private string GetInsertAssetAttributeCommand(string tableName)
        {
            return $@"DELETE FROM {tableName}
                      WHERE asset_id = @AssetId 
                      AND asset_attribute_id = @AttributeId 
                      AND _ts = @Timestamp;

                      INSERT INTO {tableName}(_ts, asset_id, asset_attribute_id, value, retention_days)
                      VALUES (@Timestamp, @AssetId, @AttributeId, @Value, @RetentionDays);";
        }

        public async Task<TimeSeries> GetNearestAssetAttributeAsync(DateTime dateTime, HistoricalEntity assetAttribute, string padding)
        {
            using (var dbConnection = GetDbConnection())
            {
                // Timestamp and value of nearest a datetime
                // Padding Option: left or right of the datetime
                var paddingQuery = padding == "left" ? " and sn._ts < @DateTime order by sn._ts desc" : "and sn._ts > @DateTime order by sn._ts asc";
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
                {
                    string sql = @$"select
                            @AssetId as AssetId
                            , @AttributeId as AttributeId
                            , value  as ValueText
                            , _ts as DateTime
                            from asset_attribute_runtime_series_text sn
                            where  sn.asset_id = @AttributeId and sn.asset_attribute_id= @AttributeId
                            {paddingQuery}
                            limit @Limit";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<TimeSeries>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, DateTime = dateTime, Limit = 1 })
                    );
                    dbConnection.Close();
                    return result;
                }
                else
                {
                    string sql = @$"select
                            @AssetId as AssetId
                            , @AttributeId as AttributeId
                            , value  as Value
                            , _ts as DateTime
                            from asset_attribute_runtime_series sn
                            where  sn.asset_id = @AttributeId and sn.asset_attribute_id= @AttributeId
                            {paddingQuery}
                            limit @Limit";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<TimeSeries>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, DateTime = dateTime, Limit = 1 })
                    );
                    dbConnection.Close();
                    return result;
                }
            }
        }

        public async Task<double> GetLastTimeDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
                {
                    var sql = @$"select extract(epoch from _ts) duration
                            from asset_attribute_runtime_series sn
                            where  sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                            order by sn._ts desc
                            limit @Limit";
                    try
                    {
                        var result = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QueryAsync<double?>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, Limit = 2 })
                        );
                        if (result.Count() == 2)
                        {
                            diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                        }
                    }
                    finally
                    {
                        dbConnection.Close();
                    }
                }
                return diff;
            }
        }

        public async Task<double> GetLastValueDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
                {
                    var sql = @$"select value
                            from asset_attribute_runtime_series sn
                            where sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                            order by sn._ts desc
                            limit @Limit";
                    try
                    {
                        var result = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QueryAsync<double?>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, Limit = 2 })
                        );
                        if (result.Count() == 2)
                        {
                            diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                        }
                    }
                    finally
                    {
                        dbConnection.Close();

                    }
                }
                return diff;
            }
        }

        public async Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
                {
                    string tableName = "asset_attribute_runtime_series";
                    try
                    {
                        var sql = @$"select extract(epoch from _ts) duration from (
                                    select _ts from (
                                            select _ts
                                            from {tableName} sn
                                            where sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                                                    and sn._ts > @Start
                                                    order by sn._ts asc
                                            limit 1
                                    ) s1
                                    union all
                                    select _ts from (
                                            select _ts
                                            from {tableName} sn
                                            where sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                                                    and sn._ts < @End
                                                    order by sn._ts desc
                                            limit 1
                                    ) s2
                              ) s3 order by _ts desc
                              LIMIT 2";
                        var result = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QueryAsync<double?>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, Start = start, End = end })
                        );
                        if (result.Count() == 2)
                        {
                            diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                        }
                    }
                    finally
                    {
                        dbConnection.Close();
                    }
                }
                return diff;
            }
        }

        public async Task<double> GetValueDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
                {
                    string tableName = "asset_attribute_runtime_series";
                    var sql = @$"select value from (
                                    select value, _ts from (
                                            select value, _ts
                                            from {tableName} sn
                                            where sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                                                    and sn._ts > @Start
                                                    order by sn._ts asc
                                            limit 1
                                    ) s1
                                    union all
                                    select value, _ts from (
                                            select value, _ts
                                            from {tableName} sn
                                            where sn.asset_id = @AssetId and sn.asset_attribute_id = @AttributeId
                                                    and sn._ts < @End
                                                    order by sn._ts desc
                                            limit 1
                                    ) s2
                              ) s3 order by _ts desc
                              LIMIT 2";
                    try
                    {
                        var result = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QueryAsync<double?>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, Start = start, End = end })
                        );
                        if (result.Count() == 2)
                        {
                            diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                        }
                    }
                    finally
                    {
                        dbConnection.Close();
                    }
                }
                return 0;
            }
        }

        public async Task<double> AggregateAssetAttributesValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            string tableName = "asset_attribute_runtime_series";
            if (DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
            {
                tableName = "asset_attribute_runtime_series_text";
            }
            var filter = "";
            if (!string.IsNullOrEmpty(filterOperation))
            {
                filter = $" and msf.Value {filterOperation} @FilterValue";
            }
            using (var dbConnection = GetDbConnection())
            {
                try
                {
                    var sql = @$" SELECT
                            {aggregate}(msf.value) as value
                            FROM {tableName} msf
                            WHERE msf.asset_id = @AssetId
                                    and msf.asset_attribute_id = @AttributeId
                                    and msf._ts >= @Start
                                    and msf._ts < @End
                                    {filter}
                                ";
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var rs = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<double?>(sql, new { AssetId = assetAttribute.AssetId, AttributeId = assetAttribute.AttributeId, Start = start, End = end, FilterValue = filterValue })
                    );
                    return rs ?? 0;
                }
                finally
                {
                    dbConnection.Close();
                }
            }
        }

        public async Task<double> GetDurationAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            string tableName = "fnc_sel_asset_attribute_lead";
            if (DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
            {
                tableName = "fnc_sel_asset_attribute_lead_text";
            }
            var sql = @$" select sum(duration)
                            from {tableName} (@AssetId, @AttributeId, @Start, @End)
                            where value {filterOperation} @FilterValue;
                        ";
            using (var connection = GetDbConnection())
            {
                try
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var rs = await retryStrategy.ExecuteAsync(async () =>
                        await connection.QueryFirstOrDefaultAsync<double?>(sql, new
                        {
                            AssetId = assetAttribute.AssetId,
                            AttributeId = assetAttribute.AttributeId,
                            Start = start,
                            End = end,
                            FilterValue = filterValue
                        })
                    );
                    return rs ?? 0;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task<int> GetCountAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            string tableName = "fnc_sel_asset_attribute_lead";
            if (DataTypeConstants.TEXT_TYPES.Contains(assetAttribute.DataType))
            {
                tableName = "fnc_sel_asset_attribute_lead_text";
            }
            var sql = @$"  select count(duration)
                            from {tableName} (@AssetId, @AttributeId, @Start, @End)
                            where value {filterOperation} @FilterValue;
                        ";
            using (var connection = GetDbConnection())
            {
                try
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await connection.QueryFirstOrDefaultAsync<int?>(sql, new
                        {
                            AssetId = assetAttribute.AssetId,
                            AttributeId = assetAttribute.AttributeId,
                            Start = start,
                            End = end,
                            FilterValue = filterValue
                        })
                    );
                    return result ?? 0;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var limit = LIMIT * metrics.Count();
            var sql = HistogramSqlHandler[PostgresFunction.DEFAULT_FUNCTION].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, limit, metrics, binSize));

            if (!string.IsNullOrEmpty(timegrain) && HistogramSqlHandler.ContainsKey(gapfillFunction))
            {
                sql = HistogramSqlHandler[gapfillFunction].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, limit, metrics, binSize));
            }
            using (var connection = GetDbConnection())
            {
                try
                {
                    return await connection
                            .QueryAsync<Histogram>(sql, new { Start = timeStart, End = timeEnd, BinSize = binSize, AttributeIds = metrics.Select(x => x.AttributeId).ToArray() })
                            .HandleResult<Histogram>();
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var limit = LIMIT * metrics.Count();
            var sql = StatisticsSqlHandler[PostgresFunction.DEFAULT_FUNCTION].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, limit, metrics));
            using (var connection = GetDbConnection())
            {
                try
                {
                    return await connection
                            .QueryAsync<Statistics>(sql, new { Start = timeStart, End = timeEnd })
                            .HandleResult<Statistics>();
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics), string>> StatisticsSqlHandler
        {
            get
            {
                var dict = new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics), string>>();
                dict.Add(PostgresFunction.DEFAULT_FUNCTION, StatisticsDefaultFillFunction);
                //dict.Add(PostgresFunction.TIME_BUCKET, HistogramTimeBucketFunction);
                //dict.Add(PostgresFunction.TIME_BUCKET_GAPFILL, HistogramTimeBucketGapFillFunction);
                return dict;
            }
        }

        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit), string>> TimeSeriesSqlHandler
        {
            get
            {
                var dict = new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, int Limit), string>>();
                dict.Add(PostgresFunction.TIME_BUCKET, SeriesTimeBucketFunction);
                dict.Add(PostgresFunction.TIME_BUCKET_GAPFILL, SeriesTimeBucketGapFillFunction);
                return dict;
            }
        }
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit), string> SeriesTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"SELECT
                        s.asset_id as AssetId,
                        s.asset_attribute_id as AttributeId,
                        EXTRACT(epoch FROM s.TimeBucket {offsetTime.PostgresOffsetQueryReverse}) * 1000 AS UnixTimestamp,
                        s.value as Value
                    FROM (
                            SELECT
                                msf.asset_id,
                                msf.asset_attribute_id,
                                {data.GapfillFunction}(INTERVAL '{data.Timegrain}',  msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                {data.Aggregate}(msf.value) as value
                            FROM asset_attribute_runtime_series msf
                            WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                    AND msf._ts >= @Start
                                    AND msf._ts <= @End
                            GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                        ) s
                    ORDER BY UnixTimestamp desc
                    LIMIT {data.Limit}";
        };
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit), string> SeriesTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"SELECT
                        s.asset_id as AssetId,
                        s.asset_attribute_id as AttributeId,
                        EXTRACT(epoch FROM s.TimeBucket {offsetTime.PostgresOffsetQueryReverse}) * 1000 AS UnixTimestamp,
                        s.value as Value
                    FROM (
                            SELECT
                                msf.asset_id,
                                msf.asset_attribute_id,
                                {data.GapfillFunction}(INTERVAL '{data.Timegrain}',  msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                INTERPOLATE({data.Aggregate}(msf.value)) as value
                            FROM asset_attribute_runtime_series msf
                            WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                    AND msf._ts >= @Start
                                    AND msf._ts <= @End
                            GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                        ) s
                    ORDER BY UnixTimestamp desc
                    LIMIT {data.Limit}";
        };

        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics, double BinSize), string>> HistogramSqlHandler
        {
            get
            {
                var dict = new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics, double BinSize), string>>();
                dict.Add(PostgresFunction.DEFAULT_FUNCTION, HistogramDefaultFillFunction);
                dict.Add(PostgresFunction.TIME_BUCKET, HistogramTimeBucketFunction);
                dict.Add(PostgresFunction.TIME_BUCKET_GAPFILL, HistogramTimeBucketGapFillFunction);
                return dict;
            }
        }
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics, double BinSize), string> HistogramTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"  WITH rawData AS (
                                SELECT
                                    msf.asset_id,
                                    msf.asset_attribute_id,
                                    {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                    {data.Aggregate}(msf.value) as value
                                FROM asset_attribute_runtime_series msf
                                WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                        AND msf._ts >= @Start
                                        AND msf._ts <= @End
                                GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                                LIMIT {data.Limit}
                        )
                        select  r.asset_id as AssetId
                                , r.asset_attribute_id as AttributeId
                                , t.value_from  as ValueFrom
                                , t.value_to as ValueTo
                                , histogram(r.value, t.value_from ,t.value_to ,t.total_bin) as Items
                                , t.total_bin as TotalBin
                        from rawData r
                        join (
							select asset_id, asset_attribute_id,
                                    (floor(min(value)/{data.BinSize})::int * {data.BinSize}) as value_from,
                                    (floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize} as value_to,
                                    cast(((((floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize}) as int) as total_bin
							from rawData
							group by asset_id, asset_attribute_id
                        ) as t on t.asset_id = r.asset_id and t.asset_attribute_id = r.asset_attribute_id
                        group by r.asset_id, r.asset_attribute_id, t.value_from, t.value_to, t.total_bin;";

        };
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics, double BinSize), string> HistogramTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"  WITH rawData AS (
                                SELECT
                                    msf.asset_id,
                                    msf.asset_attribute_id,
                                    {data.GapfillFunction}(INTERVAL '{data.Timegrain}',  msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                    INTERPOLATE({data.Aggregate}(msf.value)) as value
                                FROM asset_attribute_runtime_series msf
                                WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                        AND msf._ts >= @Start
                                        AND msf._ts <= @End
                                GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                                LIMIT {data.Limit}
                        )
                        select  r.asset_id as AssetId
                                , r.asset_attribute_id as AttributeId
                                , t.value_from  as ValueFrom
                                , t.value_to as ValueTo
                                , histogram(r.value, t.value_from ,t.value_to ,t.total_bin) as Items
                                , t.total_bin as TotalBin
                        from rawData r
                        join (
							select asset_id, asset_attribute_id,
                                    (floor(min(value)/{data.BinSize})::int * {data.BinSize}) as value_from,
                                    (floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize} as value_to,
                                    cast(((((floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize}) as int) as total_bin
							from rawData
							group by asset_id, asset_attribute_id
                        ) as t on t.asset_id = r.asset_id and t.asset_attribute_id = r.asset_attribute_id
                        group by r.asset_id, r.asset_attribute_id, t.value_from, t.value_to, t.total_bin;";
        };
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics, double BinSize), string> HistogramDefaultFillFunction = data =>
        {
            var metrics = JsonConvert.SerializeObject(data.Metrics.Select(m => new { asset_id = m.AssetId, attribute_id = m.AttributeId, device_id = string.Empty, metric_key = string.Empty }));
            var sql = @$"   with vars as (
                                select (floor(min(aars.value)/{data.BinSize})::int * {data.BinSize}) as min_value
                                            ,((floor(max(aars.value)/{data.BinSize})::int + 1) * {data.BinSize}) as max_value
                                            ,((((floor(max(aars.value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(aars.value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize})::int  as total_bin
                                , aars.asset_id
                                , aars.asset_attribute_id as attribute_id
                            from asset_attribute_runtime_series aars
                            inner join (
                                select asset_id, attribute_id, device_id, metric_key from json_to_recordset('{metrics}') as specs(asset_id uuid, attribute_id uuid,device_id varchar(255), metric_key VARCHAR(255))
                            ) as m on aars.asset_id = m.asset_id and aars.asset_attribute_id = m.attribute_id
                            where aars._ts >= @start and aars._ts <= @end
                            group by aars.asset_id, aars.asset_attribute_id)
                            select    aars.asset_id as AssetId
                                    , aars.asset_attribute_id as AttributeId
                                    , v.min_value as ValueFrom
                                    , v.max_value as ValueTo
                                    , histogram(aars.value, v.min_value, v.max_value, v.total_bin) as Items
                                    , v.total_bin as TotalBin
                            from asset_attribute_runtime_series aars
                            inner join (
                                select asset_id, attribute_id, device_id, metric_key from json_to_recordset('{metrics}') as specs(asset_id uuid, attribute_id uuid,device_id varchar(255), metric_key VARCHAR(255))
                            ) as m on aars.asset_id = m.asset_id and aars.asset_attribute_id = m.attribute_id
                            join vars v on aars.asset_id = v.asset_id and aars.asset_attribute_id = v.attribute_id
                            where aars._ts >= @start and aars._ts <= @end
                            group by aars.asset_id, aars.asset_attribute_id, v.min_value, v.max_value, v.total_bin;
            ";
            return sql;
        };

        private async Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryNumbericSeriesDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize)
        {
            var fromWhereSql = @$"  FROM asset_attribute_runtime_series msf
                                    WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                                AND msf._ts >= @Start
                                                AND msf._ts <= @End";
            var getDataSql = @$"
                       SELECT
                            msf.asset_id AS AssetId,
                            msf.asset_attribute_id AS AttributeId,
                            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                            msf.value AS Value
                        {fromWhereSql}
                        ORDER BY msf._ts DESC
                        OFFSET {pageIndex * pageSize} ROWS
                        FETCH NEXT {pageSize} ROWS ONLY;";
            var getTotalCountSql = @$"SELECT 
                                        COUNT(1) as TotalCount
                                    {fromWhereSql}";

            string[] sqlQuery = new string[] { getDataSql, getTotalCountSql };

            if (!string.IsNullOrEmpty(timegrain) && TimeSeriesSqlHandler.ContainsKey(gapfillFunction))
            {
                sqlQuery = PaginationTimeSeriesSqlHandler[gapfillFunction].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, pageIndex, pageSize));
            }

            var queryDataSql = sqlQuery[0];
            var queryTotalCountSql = sqlQuery[1];
            var tasks = new List<Task>
            {
                GetDbConnection().QueryPagingDataAsync<TimeSeries>(queryDataSql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, timeout),
                GetDbConnection().QueryTotalCountAsync(queryTotalCountSql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, timeout)
            };
            await Task.WhenAll(tasks);
            var output = ((Task<IEnumerable<TimeSeries>>)tasks.ElementAt(0)).Result;
            var totalCount = ((Task<int>)tasks.ElementAt(1)).Result;

            return (output, totalCount);
        }

        private async Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryTextSeriesDataAsync(string timezoneId, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize)
        {
            var fromWhereSql = @$"  FROM asset_attribute_runtime_series_text msf
                                    WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                            and msf._ts >= @Start
                                            and msf._ts <= @End";

            var getDataSql = @$"
                        SELECT
                            msf.asset_id AS AssetId,
                            msf.asset_attribute_id AS AttributeId,
                            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                            msf.value AS ValueText
                        {fromWhereSql}
                        ORDER BY msf._ts DESC
                        OFFSET {pageIndex * pageSize} ROWS
                        FETCH NEXT {pageSize} ROWS ONLY;";

            var getTotalCountSql = @$"SELECT 
                                        COUNT(1) as TotalCount
                                    {fromWhereSql}";

            var tasks = new List<Task>
            {
                GetDbConnection().QueryPagingDataAsync<TimeSeries>(getDataSql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, timeout),
                GetDbConnection().QueryTotalCountAsync(getTotalCountSql, new { AttributeIds = metrics.Select(x => x.AttributeId).ToArray(), Start = timeStart, End = timeEnd }, timeout)
            };
            await Task.WhenAll(tasks);
            var output = ((Task<IEnumerable<TimeSeries>>)tasks.ElementAt(0)).Result;
            var totalCount = ((Task<int>)tasks.ElementAt(1)).Result;

            return (output, totalCount);
        }

        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int PageIndex, int PageSize), string[]>> PaginationTimeSeriesSqlHandler
        {
            get
            {
                var dict = new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, int PageIndex, int PageSize), string[]>>()
                {
                    {PostgresFunction.TIME_BUCKET, PaginationSeriesTimeBucketFunction},
                    {PostgresFunction.TIME_BUCKET_GAPFILL, PaginationSeriesTimeBucketGapFillFunction}
                };

                return dict;
            }
        }

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int PageIndex, int PageSize), string[]> PaginationSeriesTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();

            var fromWhereSql = @$"FROM (
                                    SELECT
                                        msf.asset_id,
                                        msf.asset_attribute_id,
                                        {data.GapfillFunction}(INTERVAL '{data.Timegrain}',  msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                        {data.Aggregate}(msf.value) as value
                                    FROM asset_attribute_runtime_series msf
                                    WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                            AND msf._ts >= @Start
                                            AND msf._ts <= @End
                                    GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                                ) s";

            var getDataSql = @$"SELECT
                                    s.asset_id as AssetId,
                                    s.asset_attribute_id as AttributeId,
                                    EXTRACT(epoch FROM s.TimeBucket {offsetTime.PostgresOffsetQueryReverse}) * 1000 AS UnixTimestamp,
                                    s.value as Value
                                {fromWhereSql}
                                ORDER BY UnixTimestamp DESC
                                OFFSET {data.PageIndex * data.PageSize} ROWS
                                FETCH NEXT {data.PageSize} ROWS ONLY;";

            var getTotalCountSql = @$"SELECT 
                                        COUNT(1) AS TotalCount
                                    {fromWhereSql}";

            return new string[] { getDataSql, getTotalCountSql };
        };

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int PageIndex, int PageSize), string[]> PaginationSeriesTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            var fromWhereSql = @$"FROM (
                                    SELECT
                                        msf.asset_id,
                                        msf.asset_attribute_id,
                                        {data.GapfillFunction}(INTERVAL '{data.Timegrain}',  msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                        INTERPOLATE({data.Aggregate}(msf.value)) as value
                                    FROM asset_attribute_runtime_series msf
                                    WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                            AND msf._ts >= @Start
                                            AND msf._ts <= @End
                                    GROUP BY msf.asset_id,msf.asset_attribute_id, TimeBucket
                                ) s ";

            var getDataSql = @$"SELECT
                                    s.asset_id as AssetId,
                                    s.asset_attribute_id as AttributeId,
                                    EXTRACT(epoch FROM s.TimeBucket {offsetTime.PostgresOffsetQueryReverse}) * 1000 AS UnixTimestamp,
                                    s.value as Value
                                {fromWhereSql}
                                ORDER BY UnixTimestamp DESC
                                OFFSET {data.PageIndex * data.PageSize} ROWS
                                FETCH NEXT {data.PageSize} ROWS ONLY;";

            var getTotalCountSql = @$"SELECT 
                                        COUNT(1) AS TotalCount
                                    {fromWhereSql}";

            return new string[] { getDataSql, getTotalCountSql };
        };

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, int Limit, IEnumerable<HistoricalEntity> Metrics), string> StatisticsDefaultFillFunction = data =>
        {
            var asset_IDs = string.Join(",", data.Metrics.Select(m => $"'{m.AssetId}'::uuid"));
            var attribute_IDs = string.Join(",", data.Metrics.Select(m => $"'{m.AttributeId}'::uuid"));

            var sql = @$" WITH series as (
                            SELECT
                                aars.asset_id,
                                aars.asset_attribute_id as attribute_id,
                                aars.value,
                                ROW_NUMBER() OVER (partition by asset_id, asset_attribute_id ORDER BY aars.value) AS row_num
                                from asset_attribute_runtime_series aars
                                where aars.asset_id = any(array[{asset_IDs}])
                                and aars.asset_attribute_id = any(array[{attribute_IDs}])
                                and aars._ts >= @Start and aars._ts <= @End
                            ), stats as (
                                select aars.asset_id,
                                    aars.asset_attribute_id as attribute_id,
                                    count(*) as n
                                from asset_attribute_runtime_series aars
                                where aars.asset_id = any(array[{asset_IDs}])
                                and aars.asset_attribute_id = any(array[{attribute_IDs}])
                                and aars._ts >= @Start and aars._ts <= @End
                                group by aars.asset_id, aars.asset_attribute_id
                            )
                            select s.asset_id, s.attribute_id,
                                stddev_samp (aars.value) as STDev,
                                avg (aars.value) as Mean,
                                min  (aars.value) as Min,
                                max (aars.value) as Max,
                                percentile_cont(0.25) WITHIN GROUP (ORDER BY aars.value) AS Q1_Inc,
                                percentile_cont(0.50) WITHIN GROUP (ORDER BY aars.value) AS Q2_Inc,
                                percentile_cont(0.50) WITHIN GROUP (ORDER BY aars.value) AS Q2_Exc,
                                percentile_cont(0.75) WITHIN GROUP (ORDER BY aars.value) AS Q3_Inc,
                                percentile_cont(0.25) WITHIN GROUP (ORDER BY aars.value) filter (where row_num <= s.n - 2) AS Q1_Exc,
                                percentile_cont(0.75) WITHIN GROUP (ORDER BY aars.value) filter (where row_num > 2) AS Q3_Exc
                                from stats s
                                inner join series aars on s.asset_id = aars.asset_id and s.attribute_id = aars.attribute_id
                                group by s.asset_id, s.attribute_id; ";
            return sql;
        };

    }
}
