using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Application.Historical.Query;
using Device.Application.Analytic.Query.Model;
using Device.Application.Historical.Query.Model;

/*
IMPORTANCE: High performance api should be reviewed and approved by technical team.
PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND
Author: Thanh Tran
Email: thanh.tran@yokogawa.com
*/
namespace Device.Application.Analytic.Query.Handler
{
    public class GetAssetAttributeRegressionHandler : IRequestHandler<GetAssetAttributeRegressionData, RegressionDataDto>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IRegressionAnalysis _analysis;
        private readonly int LIMIT;
        public GetAssetAttributeRegressionHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext, IConfiguration configuration, IRegressionAnalysis analysis)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _analysis = analysis;
            LIMIT = 3; // A regression of the requested order requires at least {2} samples.

        }
        public async Task<RegressionDataDto> Handle(GetAssetAttributeRegressionData request, CancellationToken cancellationToken)
        {

            var requestAssetsGroup = request.Assets
                    .GroupBy(x => new { x.RequestType, x.Aggregate, x.TimeGrain, x.Start, x.End, x.ProjectId, x.SubscriptionId, x.AssetId, x.UseCustomTimeRange, x.GapfillFunction, x.PageSize, x.Variable })
                    .Select(
                    x => new AssetAttributeRegression
                    {
                        TimeoutInSecond = request.TimeoutInSecond,
                        RequestType = x.Key.RequestType,
                        Aggregate = x.Key.Aggregate,
                        TimeGrain = x.Key.TimeGrain,
                        Start = x.Key.Start,
                        End = x.Key.End,
                        ProjectId = x.Key.ProjectId,
                        SubscriptionId = x.Key.SubscriptionId,
                        UseCustomTimeRange = x.Key.UseCustomTimeRange,
                        AssetId = x.Key.AssetId,
                        AttributeIds = x.SelectMany(s => s.AttributeIds),
                        GapfillFunction = x.Key.GapfillFunction,
                        PageSize = x.Key.PageSize,
                        Variable = x.Key.Variable,
                    });
            var ret = new RegressionDataDto();

            // New change: we do not allow to mix the request, snapshot, historical snapshot and timeries should be separate request to BE.
            ValidateRequest(requestAssetsGroup);
            //Get device client
            var tasks = requestAssetsGroup.Select(x =>
            {
                var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                // set new projectId into tenantContext
                var variable = x.Variable;

                tenantContext.RetrieveFromString(_tenantContext.TenantId, x.SubscriptionId, x.ProjectId);
                var seriesRequest = new GetHistoricalData(x.Start, x.End, x.TimeGrain, x.Aggregate, request.TimeoutInSecond, x.AssetId, x.AttributeIds, x.UseCustomTimeRange, x.RequestType, request.TimezoneOffset, x.GapfillFunction, x.PageSize, request.Quality);

                var assetTimeSeriesService = scope.ServiceProvider.GetRequiredService<IAssetTimeSeriesService>();
                return assetTimeSeriesService.GetTimeSeriesDataAsync(seriesRequest, cancellationToken);

            });

            var result = await Task.WhenAll(tasks);
            var values = result.SelectMany(x => x);
            var rs = (
                        from requestAsset in request.Assets
                        join timeseries in values on new { requestAsset.AssetId, requestAsset.Start, requestAsset.End, requestAsset.TimeGrain, requestAsset.RequestType, requestAsset.Aggregate }
                        equals new { timeseries.AssetId, timeseries.Start, timeseries.End, timeseries.TimeGrain, RequestType = timeseries.QueryType, timeseries.Aggregate } into gj
                        let asset = gj.FirstOrDefault()
                        select new HistoricalDataDtoRegression()
                        {
                            AssetId = requestAsset.AssetId,
                            AssetName = asset?.AssetName,
                            AssetNormalizeName = asset?.AssetNormalizeName,
                            Start = requestAsset.Start,
                            End = requestAsset.End,
                            TimeGrain = requestAsset.TimeGrain,
                            Aggregate = requestAsset.Aggregate,
                            Statics = requestAsset.Statics,
                            QueryType = requestAsset.RequestType,
                            TimezoneOffset = request.TimezoneOffset,
                            RequestType = requestAsset.RequestType.ToString().ToLowerInvariant(),
                            Variable = requestAsset.Variable,
                            Attributes = gj.SelectMany(g => g.Attributes).Where(metric => requestAsset.AttributeIds.Contains(metric.AttributeId) && metric.GapfillFunction == requestAsset.GapfillFunction).ToList()
                        }
            ).ToList();

            //var dependenceResult = await Task.WhenAll(dependenceVariable);
            var independenceResult = rs.Where(x => x.Variable == VARIABLETYPE.INDEPENDENTVARIABLE);
            var dependenceResult = rs.Where(x => x.Variable == VARIABLETYPE.DEPENDENTVARIABLE);
            //merge 2 serries list
            if (independenceResult.Any() && dependenceResult.Any())
            {
                var independenceAttribute = independenceResult.FirstOrDefault();
                var dependenceAttribute = dependenceResult.FirstOrDefault();

                if (independenceAttribute.Attributes.Any())
                {
                    var independenceSeries = independenceAttribute.Attributes
                                .Select(x => x.Series.Where(series => series.v != null).Select(series => new TimeSeriesDto() { ts = series.ts, v = Convert.ToDouble(series.v) }))
                                .FirstOrDefault();
                    var dependenceSeries = dependenceAttribute.Attributes
                                .Where(name => name != null)
                                .Select(x => x.Series.Where(series => series.v != null).Select(series => new TimeSeriesDto() { ts = series.ts, v = Convert.ToDouble(series.v) }))
                                .FirstOrDefault();
                    var independenceAsset = new AssetRegressionDataDto()
                    {
                        AssetId = independenceAttribute.AssetId,
                        AssetName = independenceAttribute.AssetName,
                        AssetNormalizeName = independenceAttribute.AssetNormalizeName,
                        Aggregate = independenceAttribute.Aggregate,
                        TimeGrain = independenceAttribute.TimeGrain,
                        Attributes = (from attribute in independenceAttribute.Attributes
                                      select new AssetAttributeRegressionDataDto()
                                      {
                                          AttributeId = attribute.AttributeId,
                                          AttributeName = attribute.AttributeName,
                                          AttributeNameNormalize = attribute.AttributeNameNormalize,
                                          ThousandSeparator = attribute.ThousandSeparator,
                                          Uom = attribute.Uom,
                                          DecimalPlace = attribute.DecimalPlace
                                      }),
                        Start = independenceAttribute.Start,
                        End = independenceAttribute.End,
                        QueryType = independenceAttribute.QueryType,
                        RequestType = independenceAttribute.RequestType,
                        Statics = independenceAttribute.Statics,
                        TimezoneOffset = independenceAttribute.TimezoneOffset
                    };
                    var dependenceAsset = new AssetRegressionDataDto()
                    {
                        AssetId = dependenceAttribute.AssetId,
                        AssetName = dependenceAttribute.AssetName,
                        AssetNormalizeName = dependenceAttribute.AssetNormalizeName,
                        Aggregate = dependenceAttribute.Aggregate,
                        TimeGrain = dependenceAttribute.TimeGrain,
                        Attributes = (from attribute in dependenceAttribute.Attributes
                                      select new AssetAttributeRegressionDataDto()
                                      {
                                          AttributeId = attribute.AttributeId,
                                          AttributeName = attribute.AttributeName,
                                          AttributeNameNormalize = attribute.AttributeNameNormalize,
                                          ThousandSeparator = attribute.ThousandSeparator,
                                          Uom = attribute.Uom,
                                          DecimalPlace = attribute.DecimalPlace
                                      }),
                        Start = dependenceAttribute.Start,
                        End = dependenceAttribute.End,
                        QueryType = dependenceAttribute.QueryType,
                        RequestType = dependenceAttribute.RequestType,
                        Statics = dependenceAttribute.Statics,
                        TimezoneOffset = dependenceAttribute.TimezoneOffset
                    };
                    // { ts, value}
                    var innerJoinQuery =
                    from independence in independenceSeries
                    join dependence in dependenceSeries on independence.ts equals dependence.ts
                    select new FitingPoint() { ts = independence.ts, y = (double)dependence.v, x = (double)independence.v }; //produces flat sequence
                    var distinctData = innerJoinQuery
                    .GroupBy(d => d.ts)
                    .Select(group => group.OrderBy(g => g.ts).Last());
                    var x = distinctData.Select(k => k.x);
                    var y = distinctData.Select(k => k.y);
                    if (x.Count() > LIMIT && y.Count() > LIMIT) // at leat 3 items
                    {
                        try
                        {
                            var analysisResult = _analysis.FitAnalysis(request.FitMethod, x, y, request.Order);
                            ret.IndependenceAsset = independenceAsset;
                            ret.DependenceAsset = dependenceAsset;
                            ret.Equation = analysisResult.Equation;
                            ret.GoodnessMeansures = analysisResult.GoodnessMeansures;
                            ret.Coefficients = analysisResult.Coefficients;
                        }
                        catch
                        {
                            // invalid data, cannot find fit method
                            ret.Equation = "N/A";
                        }

                    }
                    //sample plots
                    ret.SamplePlots = distinctData.Skip(0).Take(request.LimitSample);
                }
            }

            return ret;
        }


        private void ValidateRequest(IEnumerable<AssetAttributeRegression> requests)
        {
            var requestCount = requests.Count();
            var customTimeRangeRequestCount = requests.Count(x => x.UseCustomTimeRange);
            if (customTimeRangeRequestCount > 0 && customTimeRangeRequestCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Historical snapshot should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }
            var snapshotCount = requests.Count(x => x.RequestType == HistoricalDataType.SNAPSHOT);
            if (snapshotCount > 0 && snapshotCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Snapshot should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }

            var timeSeriesCount = requests.Count(x => x.RequestType == HistoricalDataType.SERIES);
            if (timeSeriesCount > 0 && timeSeriesCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Timeseries should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }
        }
    }
}
