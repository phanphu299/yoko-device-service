using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Dapper;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.Extensions;

namespace Device.Persistence.Repository
{
    public class AssetHistoricalSnapshotRepository : IAssetHistoricalSnapshotRepository
    {
        private readonly ILoggerAdapter<AssetHistoricalSnapshotRepository> _logger;
        private readonly IReadDbConnectionFactory _dbConnectionFactory;

        public AssetHistoricalSnapshotRepository(ILoggerAdapter<AssetHistoricalSnapshotRepository> logger, IReadDbConnectionFactory dbConnectionFactory)
        {
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction)
        {
            var tasks = new List<Task<IEnumerable<TimeSeries>>>();
            var numbericTypeAttributes = metrics.Where(x => DataTypeConstants.NUMBERIC_TYPES.Contains(x.DataType));

            var textTypeAttributes = metrics.Where(x => DataTypeConstants.TEXT_TYPES.Contains(x.DataType));
            if (numbericTypeAttributes.Any())
            {
                tasks.Add(QueryNumbericSeriesDataAsync(timeStart, timeEnd, numbericTypeAttributes, timeout).HandleResult<TimeSeries>());
            }

            if (textTypeAttributes.Any())
            {
                tasks.Add(QueryTextSeriesDataAsync(timeStart, timeEnd, textTypeAttributes, timeout).HandleResult<TimeSeries>());
            }

            var result = await Task.WhenAll(tasks);
            var data = result.SelectMany(x => x);
            return data;
        }


        private async Task<IEnumerable<TimeSeries>> QueryNumbericSeriesDataAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout)
        {
            var sql = @$"select t.DeviceId, t.MetricKey, t.UnixTimestamp, t.ValueText, t.SignalQualityCode, t.LastGoodUnixTimestamp, t.LastGoodValueText
                        from (
                            select msf.device_id as DeviceId,
                                msf.metric_key as MetricKey,
                                extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                                msf.value as ValueText,
                                signal_quality_code as SignalQualityCode,
                                extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp,
                                msf.last_good_value as LastGoodValueText,
                                ROW_NUMBER() OVER(
                                    PARTITION BY msf.device_id, msf.metric_key
                                    ORDER by msf._ts desc
                                ) as RowNum
                            FROM device_metric_series msf
                            WHERE msf._ts >= @Start
                                    and msf._ts <= @End
                                    and ({string.Join(" or ", metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' and  msf.metric_key= '{metric.MetricKey.Replace("'", "''")}')"))})
                        ) t
                        where t.RowNum = 1;";

            using (var connection = GetDbConnection())
            {
                _logger.LogDebug(sql);

                var result = new List<TimeSeries>();
                var series = Enumerable.Empty<TimeSeries>();
                var snapshots = Enumerable.Empty<TimeSeries>();
                var attributeIds = metrics.Select(x => x.AttributeId).ToArray();
                var timeseriesData = await connection.QueryAsync<DeviceMetricTimeseries>(sql, new { AttributeIds = attributeIds, Start = timeStart, End = timeEnd }, commandTimeout: timeout);
                connection.Close();

                if (timeseriesData.Any())
                {
                    series = (from metric in metrics
                              join timeseries in timeseriesData on new { metric.DeviceId, metric.MetricKey } equals new { timeseries.DeviceId, timeseries.MetricKey }
                              select new TimeSeries()
                              {
                                  AttributeId = metric.AttributeId,
                                  AssetId = metric.AssetId,
                                  ValueText = timeseries.ValueText,
                                  UnixTimestamp = timeseries.UnixTimestamp,
                                  DataType = metric.DataType,
                                  LastGoodValueText = timeseries.LastGoodValueText,
                                  LastGoodUnixTimestamp = timeseries.LastGoodUnixTimestamp,
                                  SignalQualityCode = timeseries.SignalQualityCode,
                              });
                    result.AddRange(series);
                }

                return result.Select(timeseriesData =>
                {
                    if (DataTypeConstants.NUMBERIC_TYPES.Contains(timeseriesData.DataType))
                    {
                        if (timeseriesData.DataType == DataTypeConstants.TYPE_BOOLEAN)
                        {
                            timeseriesData.ValueBoolean = ConvertToBoolean(timeseriesData.ValueText);
                            timeseriesData.LastGoodValueBoolean = ConvertToBoolean(timeseriesData.LastGoodValueText);
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

                    return timeseriesData;
                });
            }
        }

        public bool? ConvertToBoolean(string textValue)
        {
            return textValue == "1" ? true : textValue == "0" ? false : (bool?)null;
        }

        private async Task<IEnumerable<TimeSeries>> QueryTextSeriesDataAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout)
        {
            /*
             Not sure timescale supports timebucket for text or not.
             If not -> please use the sql below
            */
            string sql = @$"select t.DeviceId
                            , t.MetricKey
                            , t.UnixTimestamp
                            , t.ValueText
                            , t.SignalQualityCode
                            , t.LastGoodUnixTimestamp
                            , t.LastGoodValueText
                        from (
                            select msf.device_id as DeviceId,
                                msf.metric_key as MetricKey,
                                extract(epoch from msf._ts) * 1000 AS UnixTimestamp,
                                msf.value as ValueText,
                                signal_quality_code as SignalQualityCode,
                                extract(epoch from msf._lts) * 1000 AS LastGoodUnixTimestamp,
                                msf.last_good_value as LastGoodValueText,
                                ROW_NUMBER() OVER(
                                    PARTITION BY msf.device_id, msf.metric_key
                                    ORDER by msf._ts desc
                                ) as RowNum
                            FROM device_metric_series_text msf
                            WHERE msf._ts >= @Start
                                    and msf._ts <= @End
                                    and ({string.Join(" or ", metrics.Select(metric => $" (msf.device_id = '{metric.DeviceId.Replace("'", "''")}' and  msf.metric_key= '{metric.MetricKey.Replace("'", "''")}')"))})
                        ) t
                        where t.RowNum = 1;";

            using (var connection = GetDbConnection())
            {
                _logger.LogDebug(sql);

                var result = new List<TimeSeries>();
                var series = Enumerable.Empty<TimeSeries>();
                var attributeIds = metrics.Select(x => x.AttributeId).ToArray();
                var timeseriesData = await connection.QueryAsync<DeviceMetricTimeseries>(sql, new { AttributeIds = attributeIds, Start = timeStart, End = timeEnd }, commandTimeout: timeout);
                connection.Close();

                if (timeseriesData.Any())
                {
                    series = (from metric in metrics
                              join timeseries in timeseriesData on new { metric.DeviceId, metric.MetricKey } equals new { timeseries.DeviceId, timeseries.MetricKey }
                              select new TimeSeries()
                              {
                                  AttributeId = metric.AttributeId,
                                  AssetId = metric.AssetId,
                                  ValueText = timeseries.ValueText,
                                  UnixTimestamp = timeseries.UnixTimestamp,
                                  DataType = metric.DataType,
                                  LastGoodValueText = timeseries.LastGoodValueText,
                                  LastGoodUnixTimestamp = timeseries.LastGoodUnixTimestamp,
                                  SignalQualityCode = timeseries.SignalQualityCode
                              });
                }

                result.AddRange(series);
                return result;
            }
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();
    }
}
