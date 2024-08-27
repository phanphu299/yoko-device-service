
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Linq;
using System.Net.Http;
using AHI.Infrastructure.SharedKernel.Extension;
using System.Data;
using Device.Persistence.Extensions;
using Device.Application.Constant;
using AHI.Infrastructure.MultiTenancy.Extension;

namespace Device.Persistence.Repository
{
    public class AssetIntegrationTimeSeriesRepository : IAssetIntegrationTimeSeriesRepository
    {
        private readonly ITenantContext _tenantContext;
        private readonly IHttpClientFactory _httpClientFactory;
        public AssetIntegrationTimeSeriesRepository(ITenantContext tenantContext, IHttpClientFactory httpClientFactory)
        {
            _tenantContext = tenantContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, string gapfillFunction, int limit, int? quality = null)
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.BROKER, _tenantContext);
            var tasks = metrics.Select(d => QueryIntegrationDataAsync(d, httpClient, timeStart, timeEnd, timegrain, aggregate, timeout, gapfillFunction, limit).HandleResult<TimeSeries>());
            var result = await Task.WhenAll(tasks);

            var timeSeriesData = result.SelectMany(x => x);
            return (from metric in metrics
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
        }

        private string GetIntegrationTimeGrain(string source)
        {
            // need to query the integration regardless to this entityId
            // calling into waylay api.
            // fast forward into broker-service.
            string timeGrain = "auto";
            /* 0=raw data
            1=min
            2=hour
            4=daily
            3=weekly
            5=monthly
            6=annual
            10=5 mins
            11=10 mins
            12=15 mins
            13=30 mins
            */
            switch (source)
            {
                case "1 minute":
                    timeGrain = "PT1M";
                    break;
                case "5 minutes":
                    timeGrain = "PT5M";
                    break;
                case "10 minutes":
                    timeGrain = "PT10M";
                    break;
                case "15 minutes":
                    timeGrain = "PT15M";
                    break;
                case "30 minutes":
                    timeGrain = "PT30M";
                    break;
                case "1 hour":
                    timeGrain = "PT1H";
                    break;
                case "3 hours":
                    timeGrain = "PT3H";
                    break;
                case "6 hours":
                    timeGrain = "PT6H";
                    break;
                case "8 hours":
                    timeGrain = "PT8H";
                    break;
                case "12 hours":
                    timeGrain = "PT12H";
                    break;
                case "1 day":
                    timeGrain = "P1D";
                    break;
                case "1 week":
                    timeGrain = "P7D";
                    break;
                case "1 month":
                    timeGrain = "P30D";
                    break;
                case "12 months":
                    timeGrain = "P365D";
                    break;
            }
            return timeGrain;
        }
        private async Task<IEnumerable<TimeSeries>> QueryIntegrationDataAsync(Domain.Entity.HistoricalEntity deviceExternal, HttpClient httpClient, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, int timeout, string gapfillFunction, int limit)
        {
            var timeGrain = GetIntegrationTimeGrain(timegrain);
            var integrationAggregate = GetIntegrationAggregate(aggregate);
            var result = await httpClient.GetAsync($"bkr/integrations/{deviceExternal.IntegrationId}/series?entityId={deviceExternal.DeviceId}&metricKey={deviceExternal.MetricKey}&start={new DateTimeOffset(timeStart, TimeSpan.Zero).ToUnixTimeMilliseconds()}&end={new DateTimeOffset(timeEnd, TimeSpan.Zero).ToUnixTimeMilliseconds()}&grouping={timeGrain}&aggregate={integrationAggregate}");
            if (result.IsSuccessStatusCode)
            {
                var stream = await result.Content.ReadAsByteArrayAsync();
                var integrationSeries = stream.Deserialize<IEnumerable<WaylayTimeSeries>>();
                if (integrationSeries != null)
                {
                    var timeseriesData = integrationSeries.Select(x => new TimeSeries()
                    {
                        AttributeId = deviceExternal.AttributeId,
                        AssetId = deviceExternal.AssetId,
                        ValueText = x.Value,
                        UnixTimestamp = x.Timestamp,
                    });
                    return timeseriesData;
                }
            }
            return Array.Empty<TimeSeries>();
        }
        private string GetIntegrationAggregate(string source)
        {
            string aggregate = "mean";
            switch (source)
            {
                case "avg":
                    aggregate = "mean";
                    break;
                case "min":
                    aggregate = "min";
                    break;
                case "max":
                    aggregate = "max";
                    break;
                case "sum":
                    aggregate = "sum";
                    break;
                case "std":
                    aggregate = "std";
                    break;
                case "count":
                    aggregate = "count";
                    break;
            }
            return aggregate;
        }

        public Task<TimeSeries> GetNearestDeviceMetricAsync(string timezoneId, DateTime dateTime, HistoricalEntity assetAttribute, string padding)
        {
            throw new NotImplementedException();
        }

        public Task<TimeSeries> GetNearestAssetAttributeAsync(DateTime dateTime, HistoricalEntity assetAttribute, string padding)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetLastTimeDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetLastValueDiffAssetAttributeAsync(HistoricalEntity assetAttribute)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetValueDiff2PointsAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        public Task<double> AggregateAssetAttributesValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            throw new NotImplementedException();
        }

        public Task<double> GetDurationAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCountAssetAttributeValueAsync(HistoricalEntity assetAttribute, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Histogram>> GetHistogramAsync(DateTime timeStart, DateTime timeEnd, double binSize, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            throw new NotImplementedException();
        }

        public Task<(IEnumerable<TimeSeries> Series, int TotalCount)> PaginationQueryDataAsync(string timezoneOffset, DateTime timeStart, DateTime timeEnd, string timegrain, string aggregate, HistoricalEntity historicalEntity, int timeout, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Statistics>> IAssetTimeSeriesRepository.GetStatisticsAsync(DateTime timeStart, DateTime timeEnd, IEnumerable<HistoricalEntity> metrics, string timezoneOffset, string timegrain, string aggregate, string gapfillFunction)
        {
            throw new NotImplementedException();
        }
    }
}
