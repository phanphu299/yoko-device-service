using System;
using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Domain.Entity;
using Device.Application.Constant;
using Device.Persistence.Extensions;
using Device.Application.Repository;
using Dapper;
using Device.Application.DbConnections;

namespace Device.Persistence.Repository
{
    public class TimeRangeAssetSnapshotRepository : ITimeRangeAssetSnapshotRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<AssetSnapshotRepository> _logger;
        private readonly IDbConnectionResolver _dbConnectionResolver;
        public TimeRangeAssetSnapshotRepository(IConfiguration configuration, ITenantContext tenantContext, ILoggerAdapter<AssetSnapshotRepository> logger, IDbConnectionResolver dbConnectionResolver)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger;
            _dbConnectionResolver = dbConnectionResolver;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, IEnumerable<HistoricalEntity> metrics, int timeout, DateTime startDate, DateTime endDate)
        {
            var start = DateTime.UtcNow;
            var sql = BuildTimeRangeSnapshotQuery(metrics);
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryAsync<TimeSeries>(sql, new
                {
                    AttributeIds = metrics.Select(x => x.AttributeId).ToArray(),
                    StartDate = startDate,
                    EndDate = endDate
                }, commandTimeout: 30).HandleResult<TimeSeries>();

                connection.Close();

                _logger.LogTrace($"Query from database take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");

                // data returns:
                /*
                    asset_id	                            attribute_id	                        data_type	_ts	                value	            signal_quality_code	   row_num
                    e1627e07-7ae1-45fd-85e9-0cbf7948ca69	3c412678-9011-4a01-88ff-005a9be78b62	double	    2022-09-17T02:16:10	18.708294685328518	192                    -1
                    34b9ee44-a25e-4127-ac56-dd4f29204cf2	b20a95e4-08d3-49fd-9951-e7e2fa93939f	double	    2022-08-26T23:59:50	29.00936297560547	192                     1
                    e1627e07-7ae1-45fd-85e9-0cbf7948ca69	3c412678-9011-4a01-88ff-005a9be78b62	double	    2022-08-26T23:59:50	59.023957866720835	192                     1
                    34b9ee44-a25e-4127-ac56-dd4f29204cf2	b20a95e4-08d3-49fd-9951-e7e2fa93939f	double	    2022-09-17T02:16:10	54.34058660377775	192                    -1
                */

                return result.GroupBy(x => new { x.AssetId, x.AttributeId, x.DataType }).Select(x =>
                {
                    var timeseriesData = x.OrderByDescending(x => x.RowNum).First();
                    if (DataTypeConstants.NUMBERIC_TYPES.Contains(x.Key.DataType))
                    {
                        TryParseTimeSeriesNumericData(timeseriesData, x.Key.DataType);
                    }
                    return timeseriesData;
                });
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

        static string BuildTimeRangeSnapshotQuery(IEnumerable<HistoricalEntity> metrics)
        {
            var sql = @$"select
                              data_type as DataType
                            , asset_id as AssetId
                            , attribute_id as AttributeId
                            , extract(epoch from _ts) * 1000 AS UnixTimestamp
                            , value as ValueText
                            , signal_quality_code as SignalQualityCode
                            , last_good_value as LastGoodValueText
                            , extract(epoch from _lts) * 1000 AS LastGoodUnixTimestamp
                            , row_num as RowNum
                        FROM fnc_sel_asset_snapshots_by_date(@StartDate, @EndDate, {string.Join(",", metrics.Select(x => $"'{x.AttributeId}'"))})
                        ";
            return sql;
        }

        protected IDbConnection GetDbConnection() => _dbConnectionResolver.CreateConnection(isReadOnly:true);
    }
}