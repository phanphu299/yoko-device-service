using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Dapper;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Helper;
using Device.Application.Repository;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using Device.Persistence.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Device.Persistence.Repository
{
    public class DeviceMetricTimeseriesRepository : IDeviceMetricTimeseriesRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly int LIMIT;
        private readonly IDbConnectionResolver _dbConnectionResolver;
        private readonly ILogger<DeviceMetricTimeseriesRepository> _logger;
        public DeviceMetricTimeseriesRepository(IConfiguration configuration, ITenantContext tenantContext, IDbConnectionResolver dbConnectionResolver, ILogger<DeviceMetricTimeseriesRepository> logger)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            LIMIT = Convert.ToInt32(_configuration["TimeseriesLimit"] ?? "20000");
            _dbConnectionResolver = dbConnectionResolver;
            _logger = logger;
        }

        public virtual async Task<IEnumerable<DeviceMetricTimeseries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<DeviceMetric> metrics, int timeout, string gapfillFunction, int limit, int? quality = null)
        {
            var tasks = new List<Task<IEnumerable<DeviceMetricTimeseries>>>();
            var numbericTypeAttributes = metrics.Where(x => DataTypeConstants.NUMBERIC_TYPES.Contains(x.DataType));
            var textTypeAttributes = metrics.Where(x => DataTypeConstants.TEXT_TYPES.Contains(x.DataType));
            if (numbericTypeAttributes.Any())
            {
                tasks.Add(QuerySeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, numbericTypeAttributes, timeout, gapfillFunction, limit, quality, isText: false).HandleResult<DeviceMetricTimeseries>());
            }
            if (textTypeAttributes.Any())
            {
                tasks.Add(QuerySeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, textTypeAttributes, timeout, gapfillFunction, limit, quality, isText: true).HandleResult<DeviceMetricTimeseries>());
            }
            var result = await Task.WhenAll(tasks);
            return result.SelectMany(x => x);
        }

        public virtual async Task<(IEnumerable<DeviceMetricTimeseries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<DeviceMetric> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
        {
            var tasks = new List<Task<(IEnumerable<DeviceMetricTimeseries> Series, int TotalCount)>>();
            var numbericTypeAttributes = metrics.Where(x => DataTypeConstants.NUMBERIC_TYPES.Contains(x.DataType));
            var textTypeAttributes = metrics.Where(x => DataTypeConstants.TEXT_TYPES.Contains(x.DataType));
            if (numbericTypeAttributes.Any())
            {
                tasks.Add(PaginationQueryNumbericSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, numbericTypeAttributes, timeout, gapfillFunction, pageIndex, pageSize, quality).HandleResult<DeviceMetricTimeseries>());
            }
            if (textTypeAttributes.Any())
            {
                tasks.Add(PaginationQueryTextSeriesDataAsync(timezoneOffset, timeStart, timeEnd, timegrain, aggregate, textTypeAttributes, timeout, gapfillFunction, pageIndex, pageSize, quality).HandleResult<DeviceMetricTimeseries>());
            }
            var result = await Task.WhenAll(tasks);
            var timeseries = result.SelectMany(x => x.Series);
            var totalCount = result.First().TotalCount;
            return (timeseries, totalCount);
        }

        private async Task<IEnumerable<DeviceMetricTimeseries>> QuerySeriesDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<DeviceMetric> metrics, int timeout, string gapfillFunction, int limit, int? quality, bool isText)
        {
            var metricArr = metrics.ToArray();
            var qualityFilter = quality != null ? $"and msf.signal_quality_code = {quality}" : string.Empty;
            limit = limit > 0 ? limit : LIMIT;
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(timegrain) && TimeSeriesSqlHandler.ContainsKey(gapfillFunction))
            {
                stringBuilder.AppendLine(TimeSeriesSqlHandler[gapfillFunction].Invoke((isText, timezoneOffset, timegrain, aggregate, gapfillFunction, metrics, limit, qualityFilter)));
            }
            else
            {
                for (int i = 0; i < metricArr.Length; i++)
                {
                    var deviceMetric = metricArr[i];
                    stringBuilder.AppendLine(@$"
                    select
                        msf.device_id as DeviceId,
                        msf.metric_key as MetricKey,
                        extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                        extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp,
                        msf.signal_quality_code as SignalQualityCode,
                        {(
                            isText
                            ? @"msf.value as ValueText,
                            msf.last_good_value as LastGoodValueText"
                            : @"msf.value as Value,
                            msf.last_good_value as LastGoodValue"
                        )}
                    FROM {(isText ? "device_metric_series_text" : "device_metric_series")} msf
                    WHERE msf._ts >= @Start
                        and msf._ts <= @End
                        and (msf.device_id = '{deviceMetric.DeviceId.Replace("'", "''")}' and  msf.metric_key= '{deviceMetric.MetricKey.Replace("'", "''")}')
                        {qualityFilter}
                    order by msf._ts desc
                    limit {limit};
                    ");
                }
            }

            var timeseries = new List<DeviceMetricTimeseries>();
            using (var connection = GetDbConnection())
            {
                try
                {
                    var query = stringBuilder.ToString();
                    using var multipleResults = await connection.QueryMultipleAsync(query, new { Start = timeStart, End = timeEnd }, commandTimeout: timeout);
                    while (!multipleResults.IsConsumed)
                    {
                        var currentResult = (await multipleResults.ReadAsync<DeviceMetricTimeseries>()).AsList();
                        timeseries.AddRange(currentResult);
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
            return timeseries;
        }
        public async Task<DeviceMetricTimeseries> GetNearestValueDeviceMetricAsync(DateTime dateTime, DeviceMetric deviceMetric, string padding)
        {
            using (var dbConnection = GetDbConnection())
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                // Timestamp and value of nearest a datetime
                // Padding Option: left or right of the datetime
                var paddingQuery = padding == "left" ? " and _ts < @DateTime order by _ts desc" : "and _ts > @DateTime order by _ts asc";
                if (DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
                {
                    string sql = @$"select
                            @DeviceId as DeviceId
                            , @MetricKey as MetricKey
                            , value  as ValueText
                            , _ts as DateTime
                            from device_metric_series_text sn
                            where  sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                    and sn.device_id = @DeviceId and sn.metric_key= @MetricKey
                            {paddingQuery}
                            limit @Limit";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<DeviceMetricTimeseries>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, DateTime = dateTime, Limit = 1, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();
                    return result;
                }
                else
                {
                    string sql = @$"select
                            @DeviceId as DeviceId
                            , @MetricKey as MetricKey
                            , value  as Value
                            , _ts as DateTime
                            from device_metric_series sn
                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                    and sn.device_id = @DeviceId and sn.metric_key= @MetricKey
                            {paddingQuery}
                            limit @Limit";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<DeviceMetricTimeseries>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, DateTime = dateTime, Limit = 1, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();
                    return result;
                }
            }
        }
        public async Task<double> GetLastTimeDiffDeviceMetricAsync(DeviceMetric deviceMetric)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
                {
                    var sql = @$"select extract(epoch from _ts) duration
                            from device_metric_series sn
                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                    and sn.device_id = @DeviceId and sn.metric_key= @MetricKey
                            order by sn._ts desc
                            limit @Limit";
                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<double?>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, Limit = 2, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();


                    if (result.Count() == 2)
                    {
                        diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                    }
                }
                return diff;
            }
        }
        public async Task<double> GetLastValueDiffDeviceMetricAsync(DeviceMetric deviceMetric)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
                {
                    var sql = @$"select value
                            from device_metric_series sn
                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                    and sn.device_id = @DeviceId and sn.metric_key = @MetricKey
                            order by sn._ts desc
                            limit @Limit";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<double?>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, Limit = 2, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();

                    if (result.Count() == 2)
                    {
                        diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                    }
                }
                return diff;
            }
        }
        private IDbConnection GetDbConnection() => _dbConnectionResolver.CreateConnection(isReadOnly: true);

        public async Task<double> GetTimeDiff2PointsDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
                {
                    string tableName = "device_metric_series";
                    var sql = @$"select extract(epoch from _ts) duration from (
                                    select _ts from (
                                            select _ts
                                            from {tableName} sn
                                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                                    and sn.device_id = @DeviceId and sn.metric_key = @MetricKey
                                                    and sn._ts > @Start
                                                    order by sn._ts asc
                                            limit 1
                                    ) s1
                                    union all
                                    select _ts from (
                                            select _ts
                                            from {tableName} sn
                                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                                    and sn.device_id = @DeviceId and sn.metric_key = @MetricKey
                                                    and sn._ts < @End
                                                    order by sn._ts desc
                                            limit 1
                                    ) s2
                              ) s3 order by _ts desc
                              LIMIT 2";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<double?>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, Start = start, End = end, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();

                    if (result.Count() == 2)
                    {
                        diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                    }
                }
                return diff;
            }
        }

        public async Task<double> GetValueDiff2PointsDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end)
        {
            using (var dbConnection = GetDbConnection())
            {
                double diff = 0;
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (!DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
                {
                    string tableName = "device_metric_series";
                    var sql = @$"select value from (
                                    select value, _ts from (
                                            select value, _ts
                                            from {tableName} sn
                                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                                    and sn.device_id = @DeviceId and sn.metric_key = @MetricKey
                                                    and sn._ts > @Start
                                                    order by sn._ts asc
                                            limit 1
                                    ) s1
                                    union all
                                    select value, _ts from (
                                            select value, _ts
                                            from {tableName} sn
                                            where sn.signal_quality_code = @SignalQualityCode -- only get the good quality
                                                    and sn.device_id = @DeviceId and sn.metric_key = @MetricKey
                                                    and sn._ts < @End
                                                    order by sn._ts desc
                                            limit 1
                                    ) s2
                              ) s3 order by _ts desc
                              LIMIT 2";

                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<double?>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, Start = start, End = end, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    dbConnection.Close();

                    if (result.Count() == 2)
                    {
                        diff = result.ElementAt(0).Value - result.ElementAt(1).Value;
                    }
                }
                return diff;
            }
        }

        public async Task<double> AggregateDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            var filter = "";
            if (!string.IsNullOrEmpty(filterOperation))
            {
                filter = $" and msf.Value {filterOperation} @FilterValue";
            }
            var sql = @$"   SELECT
                            {aggregate}(msf.value) as value
                            FROM device_metric_series msf
                            WHERE msf.signal_quality_code = @SignalQualityCode -- only get the good quality
                                    and msf.device_id = @DeviceId
                                    and msf.metric_key = @MetricKey
                                    and msf._ts >= @Start
                                    and msf._ts < @End
                                    {filter}
                                ";
            using (var dbConnection = GetDbConnection())
            {
                try
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<double?>(sql, new { DeviceId = deviceMetric.DeviceId, MetricKey = deviceMetric.MetricKey, Start = start, End = end, FilterValue = filterValue, SignalQualityCode = SignalQualityCode.GOOD })
                    );
                    return result ?? 0;
                }
                finally
                {
                    dbConnection.Close();
                }
            }
        }

        public async Task<int> GetCountDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            string tableName = "fnc_sel_device_metric_lead";
            if (DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
            {
                tableName = "fnc_sel_device_metric_lead_text";
            }
            var sql = @$" select count(duration)
                            from {tableName} (@DeviceId, @MetricKey, @Start, @End)
                            where signal_quality_code = @SignalQualityCode and  value {filterOperation} @FilterValue;
                        ";
            using (var connection = GetDbConnection())
            {
                try
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await connection.QueryFirstOrDefaultAsync<int?>(sql, new
                        {
                            DeviceId = deviceMetric.DeviceId,
                            MetricKey = deviceMetric.MetricKey,
                            Start = start,
                            End = end,
                            FilterValue = filterValue,
                            SignalQualityCode = SignalQualityCode.GOOD
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

        public async Task<double> GetDurationDeviceMetricValueAsync(DeviceMetric deviceMetric, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            string tableName = "fnc_sel_device_metric_lead";
            if (DataTypeConstants.TEXT_TYPES.Contains(deviceMetric.DataType))
            {
                tableName = "fnc_sel_device_metric_lead_text";
            }
            var sql = @$" select sum(duration)
                            from {tableName} (@DeviceId, @MetricKey, @Start, @End)
                            where signal_quality_code = @SignalQualityCode and value {filterOperation} @FilterValue;
                        ";
            using (var connection = GetDbConnection())
            {
                try
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var result = await retryStrategy.ExecuteAsync(async () =>
                        await connection.QueryFirstOrDefaultAsync<double?>(sql, new
                        {
                            DeviceId = deviceMetric.DeviceId,
                            MetricKey = deviceMetric.MetricKey,
                            Start = start,
                            End = end,
                            FilterValue = filterValue,
                            SignalQualityCode = SignalQualityCode.GOOD
                        }, commandTimeout: 30)
                    );
                    return result ?? 0;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public async Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<DeviceMetric> deviceMetrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var sql = HistogramSqlHandler[PostgresFunction.DEFAULT_FUNCTION].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, deviceMetrics, LIMIT, binSize));

            if (!string.IsNullOrEmpty(timegrain) && HistogramSqlHandler.ContainsKey(gapfillFunction))
            {
                sql = HistogramSqlHandler[gapfillFunction].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, deviceMetrics, LIMIT, binSize));
            }

            using (var connection = GetDbConnection())
            {
                try
                {
                    return await connection.QueryAsync<Histogram>(sql, new { Start = timeStart, End = timeEnd, BinSize = binSize })
                                            .HandleResult<Histogram>();
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        public async Task<IEnumerable<Statistics>> GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<DeviceMetric> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            var sql = StatisticsSqlHandler[PostgresFunction.DEFAULT_FUNCTION].Invoke((timezoneOffset, timegrain, aggregate, gapfillFunction, metrics, LIMIT));
            using (var connection = GetDbConnection())
            {
                try
                {
                    return await connection.QueryAsync<Statistics>(sql, new { Start = timeStart, End = timeEnd })
                                            .HandleResult<Statistics>();
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private static IDictionary<string, Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, string QualityFilter), string>> TimeSeriesSqlHandler
        {
            get
            {
                return new Dictionary<string, Func<(bool isText, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, string QualityFilter), string>>
                {
                    { PostgresFunction.TIME_BUCKET, SeriesTimeBucketFunction },
                    { PostgresFunction.TIME_BUCKET_GAPFILL, SeriesTimeBucketGapFillFunction }
                };
            }
        }

        private static Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, string QualityFilter), string> SeriesTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            string textValue = GetSelectTextValueSql(data.IsText, data.Aggregate);
            var selectSql = GetSelectSql(data.IsText, offsetTime.PostgresOffsetQueryReverse);

            return @$"
                    {selectSql}
                    FROM (
                        SELECT 	msf.device_id AS DeviceId,
                                msf.metric_key AS MetricKey,
                                {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                {(data.IsText ? textValue : $"{data.Aggregate}(msf.value) AS value")}
                        FROM {(data.IsText ? "device_metric_series_text" : "device_metric_series")} msf
                        WHERE msf._ts >= @Start
                                AND msf._ts <= @End
                                AND ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                {data.QualityFilter}
                        GROUP BY DeviceId, MetricKey, TimeBucket
                    ) s
                    ORDER BY s.TimeBucket desc
                    LIMIT {data.Limit};";
        };

        private static Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, string QualityFilter), string> SeriesTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            string textValue = GetSelectTextValueSql(data.IsText, data.Aggregate);
            var selectSql = GetSelectSql(data.IsText, offsetTime.PostgresOffsetQueryReverse);

            return @$"
                    {selectSql}
                    FROM (
                        SELECT 	msf.device_id as DeviceId,
                                msf.metric_key as MetricKey,
                                {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                {(data.IsText ? textValue : $"INTERPOLATE({data.Aggregate}(msf.value)) AS value")}
                        FROM {(data.IsText ? "device_metric_series_text" : "device_metric_series")} msf
                        WHERE msf._ts >= @Start
                                and msf._ts <= @End
                                and ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                {data.QualityFilter}
                        GROUP BY DeviceId, MetricKey, TimeBucket
                    ) s
                    ORDER BY s.TimeBucket desc
                    LIMIT {data.Limit};";
        };

        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, double BinSize), string>> HistogramSqlHandler
        {
            get
            {
                return new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, double BinSize), string>>
                {
                    { PostgresFunction.DEFAULT_FUNCTION, HistogramDefaultFunction },
                    { PostgresFunction.TIME_BUCKET, HistogramTimeBucketFunction },
                    { PostgresFunction.TIME_BUCKET_GAPFILL, HistogramTimeBucketGapFillFunction }
                };
            }
        }
        private static IDictionary<string, Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<Domain.Entity.DeviceMetric> Metrics, int Limit), string>> StatisticsSqlHandler
        {
            get
            {
                var dict = new Dictionary<string, Func<(string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, IEnumerable<Domain.Entity.DeviceMetric> Metrics, int Limit), string>>();
                dict.Add(PostgresFunction.DEFAULT_FUNCTION, StatisticsDefaultFunction);

                //dict.Add(PostgresFunction.TIME_BUCKET, StatisticsimeBucketFunction);
                //dict.Add(PostgresFunction.TIME_BUCKET_GAPFILL, StatisticsTimeBucketGapFillFunction);
                return dict;
            }
        }

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<Domain.Entity.DeviceMetric> Metrics, int Limit), string> StatisticsDefaultFunction = data =>
        {
            var device_IDs = string.Join(",", data.Metrics.Select(m => $"'{m.DeviceId}'"));
            var metricKeys = string.Join(",", data.Metrics.Select(m => $"'{m.MetricKey}'"));

            var sql = @$"WITH series as (
		SELECT
			dms.device_id,
			dms.metric_key,
			dms.value,
			ROW_NUMBER() OVER (partition by device_id, metric_key ORDER BY dms.value) AS row_num
			from device_metric_series dms
			where dms.device_id = any(array[{device_IDs}]) 
			and dms.metric_key = any(array[{metricKeys}]) 
			and dms._ts >= @Start and dms._ts <= @End
		), stats as (
			select dms.device_id,
				dms.metric_key,
				count(*) as n
			from device_metric_series dms
			where dms.device_id = any(array[{device_IDs}]) 
			and dms.metric_key = any(array[{metricKeys}]) 
			and dms._ts >= @Start and dms._ts <= @End
			group by dms.device_id, dms.metric_key
		)
		select s.device_id, s.metric_key,
			stddev_samp (dms.value) as STDev,
			avg (dms.value) as Mean,
			min  (dms.value) as Min,
			max (dms.value) as Max,
			percentile_cont(0.25) WITHIN GROUP (ORDER BY dms.value) AS Q1_Inc,
			percentile_cont(0.50) WITHIN GROUP (ORDER BY dms.value) AS Q2_Inc,
			percentile_cont(0.50) WITHIN GROUP (ORDER BY dms.value) AS Q2_Exc,
			percentile_cont(0.75) WITHIN GROUP (ORDER BY dms.value) AS Q3_Inc,
			percentile_cont(0.25) WITHIN GROUP (ORDER BY dms.value) filter (where row_num <= s.n - 2) AS Q1_Exc,
			percentile_cont(0.75) WITHIN GROUP (ORDER BY dms.value) filter (where row_num > 2) AS Q3_Exc
			from stats s
			inner join series dms on s.device_id = dms.device_id and s.metric_key = dms.metric_key
			group by s.device_id, s.metric_key;
                ";
            return sql;
        };
        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<Domain.Entity.DeviceMetric> Metrics, int Limit, double BinSize), string> HistogramTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"  WITH rawData AS (
                                SELECT 	msf.device_id,
                                        msf.metric_key,
                                        {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                        {data.Aggregate}(msf.value) AS value
                                FROM device_metric_series msf
                                WHERE msf._ts >= @Start
                                    AND msf._ts <= @End
                                    AND ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                GROUP BY device_id, metric_key, TimeBucket
                                LIMIT {data.Limit}
                        )
                        select  r.device_id as DeviceId
                                , r.metric_key as MetricKey
                                , t.value_from  as ValueFrom
                                , t.value_to as ValueTo
                                , histogram(r.value, t.value_from ,t.value_to ,t.total_bin) as Items
                                , t.total_bin as TotalBin
                        from rawData r
                        join (
							select device_id, metric_key,
                                    (floor(min(value)/{data.BinSize})::int * {data.BinSize}) as value_from,
                                    (floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize} as value_to,
                                    cast(((((floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize}) as int) as total_bin
							from rawData
							group by device_id, metric_key
                        ) as t on t.device_id = r.device_id and t.metric_key = r.metric_key
                        group by r.device_id, r.metric_key, t.value_from, t.value_to, t.total_bin;";
        };

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, double BinSize), string> HistogramTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            return @$"  WITH rawData AS (
                                SELECT 	msf.device_id,
                                        msf.metric_key,
                                        {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                        INTERPOLATE({data.Aggregate}(msf.value)) as value
                                FROM device_metric_series msf
                                WHERE msf._ts >= @Start
                                    AND msf._ts <= @End
                                    AND ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                GROUP BY device_id, metric_key, TimeBucket
                                LIMIT {data.Limit}
                        )
                        select  r.device_id as DeviceId
                                , r.metric_key as MetricKey
                                , t.value_from  as ValueFrom
                                , t.value_to as ValueTo
                                , histogram(r.value, t.value_from ,t.value_to ,t.total_bin) as Items
                                , t.total_bin as TotalBin
                        from rawData r
                        join (
							select device_id, metric_key,
                                    (floor(min(value)/{data.BinSize})::int * {data.BinSize}) as value_from,
                                    (floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize} as value_to,
                                    cast(((((floor(max(value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize}) as int) as total_bin
							from rawData
							group by device_id, metric_key
                        ) as t on t.device_id = r.device_id and t.metric_key = r.metric_key
                        group by r.device_id, r.metric_key, t.value_from, t.value_to, t.total_bin;";
        };

        private static Func<(string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, int Limit, double BinSize), string> HistogramDefaultFunction = data =>
        {
            var metrics = JsonConvert.SerializeObject(data.Metrics.Select(m => new { asset_id = Guid.Empty, attribute_id = Guid.Empty, device_id = m.DeviceId, metric_key = m.MetricKey }));
            var sql = @$"with vars as (
                                    select 	(floor(min(dms.value)/{data.BinSize})::int * {data.BinSize}) as min_value
                                            ,((floor(max(dms.value)/{data.BinSize})::int + 1) * {data.BinSize}) as max_value
                                            ,((((floor(max(dms.value)/{data.BinSize})::int + 1) * {data.BinSize}) - (floor(min(dms.value)/{data.BinSize})::int * {data.BinSize})) / {data.BinSize})::int  as total_bin
                                            , dms.device_id as device_id
                                            , dms.metric_key as metric_key
                                    from device_metric_series dms
                                    inner join (
                                        select asset_id, attribute_id, device_id, metric_key from json_to_recordset('{metrics}') as specs(asset_id uuid, attribute_id uuid,device_id varchar(255), metric_key VARCHAR(255))
                                    ) as m on dms.device_id = m.device_id and dms.metric_key= m.metric_key
                                    where dms._ts >= @Start and dms._ts <= @End
                                    group by dms.device_id, dms.metric_key
                                )
                            select    dms.device_id as DeviceId
                                    , dms.metric_key as MetricKey
                                    , v.min_value as ValueFrom
                                    , v.max_value as ValueTo
                                    , histogram(dms.value, v.min_value, v.max_value, v.total_bin) as Items
                                    , v.total_bin as TotalBin
                            from device_metric_series dms
                            join (
                                select asset_id, attribute_id, device_id, metric_key from json_to_recordset('{metrics}') as specs(asset_id uuid, attribute_id uuid,device_id varchar(255), metric_key VARCHAR(255))
                            ) as m on dms.device_id = m.device_id and dms.metric_key= m.metric_key
                            join vars v on dms.device_id = v.device_id and dms.metric_key = v.metric_key
                            where dms._ts >= @Start and dms._ts <= @End
                            group by dms.device_id, dms.metric_key, v.min_value, v.max_value, v.total_bin;
                ";
            return sql;
        };

        private async Task<(IEnumerable<DeviceMetricTimeseries> Timeseries, int TotalCount)> PaginationQueryNumbericSeriesDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<DeviceMetric> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality)
        {
            var qualityFilter = quality != null ? $"AND msf.signal_quality_code = {quality}" : string.Empty;
            var fromWhereSql = @$"  FROM device_metric_series msf
                                    WHERE msf._ts >= @Start
                                        AND msf._ts <= @End
                                        AND ({string.Join(" OR ", metrics.Select(metric => $"(msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key= '{metric.MetricKey.Replace("'", "''")}')"))})
                                        {qualityFilter} ";
            var getDataSql = @$"
                        SELECT
                           msf.device_id AS DeviceId,
                            msf.metric_key AS MetricKey,
                            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                            msf.value AS Value,
                            extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp,
                            msf.last_good_value AS LastGoodValue,
                            msf.signal_quality_code AS SignalQualityCode
                        {fromWhereSql}
                        ORDER BY _ts DESC
                        OFFSET {pageIndex * pageSize} ROWS
                        FETCH NEXT {pageSize} ROWS ONLY;";
            var getTotalCountSql = @$"SELECT
                                        COUNT(1) as TotalCount
                                    {fromWhereSql}";

            string[] sqlQuery = new string[] { getDataSql, getTotalCountSql };

            if (!string.IsNullOrEmpty(timegrain) && TimeSeriesSqlHandler.ContainsKey(gapfillFunction))
            {
                sqlQuery = PaginationTimeSeriesSqlHandler[gapfillFunction].Invoke((false, timezoneOffset, timegrain, aggregate, gapfillFunction, metrics, qualityFilter, pageIndex, pageSize));
            }
            var queryDataSql = sqlQuery[0];
            var queryTotalCountSql = sqlQuery[1];
            var tasks = new List<Task>
            {
                GetDbConnection().QueryPagingDataAsync<DeviceMetricTimeseries>(queryDataSql, new { Start = timeStart, End = timeEnd }, timeout),
                GetDbConnection().QueryTotalCountAsync(queryTotalCountSql, new { Start = timeStart, End = timeEnd }, timeout)
            };
            await Task.WhenAll(tasks);
            var output = ((Task<IEnumerable<DeviceMetricTimeseries>>)tasks.ElementAt(0)).Result;
            var totalCount = ((Task<int>)tasks.ElementAt(1)).Result;

            return (output, totalCount);
        }

        private async Task<(IEnumerable<DeviceMetricTimeseries> Timeseries, int TotalCount)> PaginationQueryTextSeriesDataAsync(string timezoneId, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<DeviceMetric> metrics, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality)
        {
            var qualityFilter = quality != null ? $"AND msf.signal_quality_code = {quality}" : string.Empty;
            var fromWhereSql = @$"  FROM device_metric_series_text msf
                                    WHERE msf._ts >= @Start
                                                AND msf._ts <= @End
                                                AND ({string.Join(" OR ", metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key= '{metric.MetricKey.Replace("'", "''")}')"))})
                                                {qualityFilter}";
            var getDataSql = @$"
                        SELECT
                            msf.device_id AS DeviceId,
                            msf.metric_key AS MetricKey,
                            extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                            msf.value AS ValueText,
                            extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp,
                            msf.last_good_value AS LastGoodValueText,
                            msf.signal_quality_code AS SignalQualityCode
                        {fromWhereSql}
                        ORDER BY _ts DESC
                        OFFSET {pageIndex * pageSize} ROWS
                        FETCH NEXT {pageSize} ROWS ONLY;";
            var getTotalCountSql = @$"SELECT
                                        COUNT(1) as TotalCount
                                    {fromWhereSql}";

            string[] sqlQuery = new string[] { getDataSql, getTotalCountSql };

            if (!string.IsNullOrEmpty(timegrain) && TimeSeriesSqlHandler.ContainsKey(gapfillFunction))
            {
                sqlQuery = PaginationTimeSeriesSqlHandler[gapfillFunction].Invoke((true, timezoneId, timegrain, aggregate, gapfillFunction, metrics, qualityFilter, pageIndex, pageSize));
            }
            var queryDataSql = sqlQuery[0];
            var queryTotalCountSql = sqlQuery[1];

            var tasks = new List<Task>
            {
                GetDbConnection().QueryPagingDataAsync<DeviceMetricTimeseries>(queryDataSql, new { Start = timeStart, End = timeEnd }, timeout),
                GetDbConnection().QueryTotalCountAsync(queryTotalCountSql, new { Start = timeStart, End = timeEnd }, timeout)
            };
            await Task.WhenAll(tasks);
            var output = ((Task<IEnumerable<DeviceMetricTimeseries>>)tasks.ElementAt(0)).Result;
            var totalCount = ((Task<int>)tasks.ElementAt(1)).Result;

            return (output, totalCount);
        }

        private static IDictionary<string, Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, string QualityFilter, int PageIndex, int PageSize), string[]>> PaginationTimeSeriesSqlHandler
        {
            get
            {
                return new Dictionary<string, Func<(bool isText, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction, IEnumerable<DeviceMetric> Metrics, string QualityFilter, int PageIndex, int PageSize), string[]>>()
                {
                    {PostgresFunction.TIME_BUCKET, PaginationSeriesTimeBucketFunction},
                    {PostgresFunction.TIME_BUCKET_GAPFILL, PaginationSeriesTimeBucketGapFillFunction}
                };
            }
        }

        private static Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, string QualityFilter, int PageIndex, int PageSize), string[]> PaginationSeriesTimeBucketFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            string textValue = GetSelectTextValueSql(data.IsText, data.Aggregate);

            var fromWhereSql = @$"FROM (
                                    SELECT 	msf.device_id AS DeviceId,
                                            msf.metric_key AS MetricKey,
                                            {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                            {(data.IsText ? textValue : $"{data.Aggregate}(msf.value) AS value")}
                                    FROM {(data.IsText ? "device_metric_series_text" : "device_metric_series")} msf
                                    WHERE msf._ts >= @Start
                                            AND msf._ts <= @End
                                            AND ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                            {data.QualityFilter}
                                    GROUP BY DeviceId, MetricKey, TimeBucket
                                ) s";

            var selectSql = GetSelectSql(data.IsText, offsetTime.PostgresOffsetQueryReverse);
            var getDataSql = @$"{selectSql}
                            , COUNT(*) OVER() AS TotalCount
                        {fromWhereSql}
                        ORDER BY s.TimeBucket DESC
                        OFFSET {data.PageIndex * data.PageSize} ROWS
                        FETCH NEXT {data.PageSize} ROWS ONLY;";
            var getTotalCountSql = @$"SELECT
                                        COUNT(1) AS TotalCount
                                    {fromWhereSql}";
            return new string[] { getDataSql, getTotalCountSql };
        };

        private static Func<(bool IsText, string TimezoneOffset, string Timegrain, string Aggregate, string GapfillFunction, IEnumerable<DeviceMetric> Metrics, string QualityFilter, int PageIndex, int PageSize), string[]> PaginationSeriesTimeBucketGapFillFunction = data =>
        {
            var offsetTime = data.TimezoneOffset.TimezoneOffsetToOffsetTime();
            string textValue = GetSelectTextValueSql(data.IsText, data.Aggregate);
            var fromWhereSql = @$"FROM (
                                    SELECT 	msf.device_id AS DeviceId,
                                            msf.metric_key AS MetricKey,
                                            {data.GapfillFunction}(INTERVAL '{data.Timegrain}', msf._ts {offsetTime.PostgresOffsetQuery}, @Start {offsetTime.PostgresOffsetQuery}, @End {offsetTime.PostgresOffsetQuery}) AS TimeBucket,
                                            {(data.IsText ? textValue : $"INTERPOLATE({data.Aggregate}(msf.value)) AS value")}
                                    FROM {(data.IsText ? "device_metric_series_text" : "device_metric_series")} msf
                                    WHERE msf._ts >= @Start
                                            and msf._ts <= @End
                                            and ({string.Join(" OR ", data.Metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' AND  msf.metric_key='{metric.MetricKey.Replace("'", "''")}')"))})
                                            {data.QualityFilter}
                                    GROUP BY DeviceId, MetricKey, TimeBucket
                                ) s";

            var selectSql = GetSelectSql(data.IsText, offsetTime.PostgresOffsetQueryReverse);
            var getDataSql = @$"
                            {selectSql}
                            {fromWhereSql}
                            ORDER BY s.TimeBucket DESC
                            OFFSET {data.PageIndex * data.PageSize} ROWS
                            FETCH NEXT {data.PageSize} ROWS ONLY;";

            var getTotalCountSql = @$"SELECT
                                        COUNT(1) AS TotalCount
                                    {fromWhereSql}";
            return new string[] { getDataSql, getTotalCountSql };
        };

        private static string GetSelectTextValueSql(bool isText, string aggregate)
        {
            return isText && aggregate == TimeSeriesAggregateConstants.COUNT ? $"{aggregate}(msf.value) AS value" : "last(msf.value, msf._ts) AS value";
        }

        private static string GetSelectSql(bool isText, string postgresOffsetQueryReverse)
        {
            return @$"SELECT s.DeviceId, s.MetricKey
                            , extract(epoch from s.TimeBucket {postgresOffsetQueryReverse}) * 1000 AS UnixTimestamp
                            , s.value AS {(isText ? "ValueText" : "Value")}";
        }
    }
}
