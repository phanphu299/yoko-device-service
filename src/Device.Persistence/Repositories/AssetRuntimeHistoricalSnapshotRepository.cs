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
    public class AssetRuntimeHistoricalSnapshotRepository : IAssetRuntimeHistoricalSnapshotRepository
    {
        private readonly ILoggerAdapter<AssetRuntimeHistoricalSnapshotRepository> _logger;
        private readonly IReadDbConnectionFactory _readDbConnectionFactory;

        public AssetRuntimeHistoricalSnapshotRepository(ILoggerAdapter<AssetRuntimeHistoricalSnapshotRepository> logger, IReadDbConnectionFactory dbConnectionResolver)
        {
            _logger = logger;
            _readDbConnectionFactory = dbConnectionResolver;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, int timeout, string gapfillFunction)
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
            var sql = @$"select t.AssetId, t.AttributeId, t.Timestamp as UnixTimestamp,t.Value
                        from (
                            select msf.asset_id as AssetId,
                                msf.asset_attribute_id as AttributeId,
                                extract(epoch from msf._ts) * 1000 AS Timestamp,
                                msf.value as Value,
                                ROW_NUMBER() OVER(
                                    PARTITION BY msf.asset_id, msf.asset_attribute_id
                                    ORDER by msf._ts desc
                                ) as RowNum
                            FROM asset_attribute_runtime_series msf
                            WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                and msf._ts >= @Start
                                and msf._ts <= @End
                        ) t
                        where t.RowNum = 1;";

            using (var connection = GetDbConnection())
            {
                try
                {
                    var attributeIds = metrics.Select(x => x.AttributeId).ToArray();
                    var timeSeriesData = await connection.QueryAsync<TimeSeries>(sql, new { AttributeIds = attributeIds, Start = timeStart, End = timeEnd }, commandTimeout: timeout);

                    return timeSeriesData;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        private async Task<IEnumerable<TimeSeries>> QueryTextSeriesDataAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, int timeout)
        {
            /*
             Not sure timescale supports timebucket for text or not.
             If not -> please use the sql below
            */
            string sql = @$"select t.AssetId
                            , t.AttributeId
                            , t.Timestamp as UnixTimestamp
                            ,t.Value as ValueText
                        from (
                            select msf.asset_id as AssetId,
                                msf.asset_attribute_id as AttributeId,
                                extract(epoch from msf._ts) * 1000 AS Timestamp,
                                msf.value as Value,
                                ROW_NUMBER() OVER(
                                    PARTITION BY msf.asset_id, msf.asset_attribute_id
                                    ORDER by msf._ts desc
                                ) as RowNum
                            FROM asset_attribute_runtime_series msf
                            WHERE msf.asset_attribute_id = ANY(@AttributeIds)
                                and msf._ts >= @Start
                                and msf._ts <= @End
                        ) t
                        where t.RowNum = 1;";

            using (var connection = GetDbConnection())
            {
                try
                {
                    _logger.LogDebug(sql);

                    var attributeIds = metrics.Select(x => x.AttributeId).ToArray();
                    var series = await connection.QueryAsync<TimeSeries>(sql, new { AttributeIds = attributeIds, Start = timeStart, End = timeEnd }, commandTimeout: timeout);

                    return series;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        protected IDbConnection GetDbConnection() => _readDbConnectionFactory.CreateConnection();
    }
}
