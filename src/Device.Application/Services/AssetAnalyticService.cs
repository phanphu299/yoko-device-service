using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Analytic.Query.Model;
using Device.Application.Analytic.Query;
using Device.Application.Repository;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Exception;
using System.Linq;
using Device.Application.Constant;
using System;
using Device.Domain.Entity;
using Device.Application.Uom.Command.Model;
/*
IMPORTANCE: High performance api should be reviewed and approved by technical team.
PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND 
Author: Thanh Tran
Email: thanh.tran@yokogawa.com
*/
namespace Device.Application.Service
{
    public class AssetAnalyticService : IAssetAnalyticService
    {
        private readonly IAssetTimeSeriesRepository _timeSeriesRepository;
        private readonly IAssetService _assetService;
        private readonly IAssetAliasTimeSeriesRepository _referenceTimeSeries;
        private readonly IAssetRuntimeTimeSeriesRepository _runtimeTimeSeriesRepository;
        private readonly ILoggerAdapter<AssetTimeSeriesService> _logger;
        public AssetAnalyticService(
                      IAssetService assetService
                    , IAssetTimeSeriesRepository timeSeriesRepository
                    , IAssetAliasTimeSeriesRepository referenceTimeSeries
                    , IAssetRuntimeTimeSeriesRepository runtimeTimeSeriesRepository
                    , ILoggerAdapter<AssetTimeSeriesService> logger)
        {
            _timeSeriesRepository = timeSeriesRepository;
            _runtimeTimeSeriesRepository = runtimeTimeSeriesRepository;
            _assetService = assetService;
            _referenceTimeSeries = referenceTimeSeries;
            _logger = logger;
        }

        public async Task<AssetHistogramDataDto> GetHistogramDataAsync(AssetAttributeHistogramData command, CancellationToken token)
        {
            var start = DateTime.UtcNow;
            var dto = new AssetHistogramDataDto() { AssetId = command.AssetId };
            try
            {
                var assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(command.AssetId), token);

                dto = new AssetHistogramDataDto()
                {
                    AssetId = command.AssetId,
                    AssetName = assetDto.Name,
                    AssetNormalizeName = assetDto.NormalizeName,
                    BinSize = command.BinSize,
                    End = command.End,
                    Aggregate = command.Aggregate,
                    GapfillFunction = command.GapfillFunction,
                    ProjectId = command.ProjectId,
                    Start = command.Start,
                    SubscriptionId = command.SubscriptionId,
                    TimeGrain = command.TimeGrain,
                    TimezoneOffset = command.TimezoneOffset
                };

                // aggreagate the data from integration and internal
                var tasks = new List<Task<IEnumerable<Domain.Entity.Histogram>>>();
                var attributes = assetDto.Attributes.Where(x => command.AttributeIds.Contains(x.Id)).Select(x => Asset.Command.Model.AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                _logger.LogDebug($"Query metadata take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                start = DateTime.UtcNow;
                if (attributes.Any())
                {

                    var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(command.Start).DateTime;
                    var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(command.End).DateTime;
                    // for internal -> call into database
                    var dynamicAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
                    if (dynamicAttributes.Any())
                        tasks.Add(_timeSeriesRepository.GetHistogramAsync(timeStart, timeEnd, command.BinSize, dynamicAttributes, command.TimezoneOffset, command.TimeGrain, command.Aggregate, command.GapfillFunction));

                    // for runtime internal -> call into database with runtime table
                    var runtimeAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_RUNTIME == x.AttributeType);
                    if (runtimeAttributes.Any())
                        tasks.Add(_runtimeTimeSeriesRepository.GetHistogramAsync(timeStart, timeEnd, command.BinSize, runtimeAttributes, command.TimezoneOffset, command.TimeGrain, command.Aggregate, command.GapfillFunction));

                    // alias
                    var aliasAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
                    if (aliasAttributes.Any())
                        tasks.Add(_referenceTimeSeries.GetHistogramAsync(timeStart, timeEnd, command.BinSize, aliasAttributes, command.TimezoneOffset, command.TimeGrain, command.Aggregate, command.GapfillFunction));

                    var taskResult = await Task.WhenAll(tasks);
                    var finalResult = taskResult.SelectMany(x => x);
                    _logger.LogDebug($"Query timeseries data take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                    dto.Attributes = (from attribute in attributes
                                      join histogram in finalResult on attribute.AttributeId equals histogram.AttributeId
                                      select new AssetAttributeHistogramDataDto()
                                      {
                                          AttributeId = attribute.AttributeId,
                                          AttributeName = attribute.AttributeName,
                                          AttributeNameNormalize = attribute.AttributeNameNormalize,
                                          Distributions = SplitHistogramDistribution(command.BinSize, histogram),
                                          ThousandSeparator = attribute.ThousandSeparator,
                                          GapfillFunction = command.GapfillFunction
                                      });
                }

            }
            catch (EntityNotFoundException exc)
            {
                _logger.LogError(exc, $"Entity not found {command.AssetId}", new Dictionary<string, object>() {
                            {"attributes", command.AttributeIds
    }
});
            }
            return dto;
        }

        private IEnumerable<HistogramDistribution> SplitHistogramDistribution(double binSize, Histogram histogram)
        {
            return histogram.Items.Skip(1).Take(histogram.Items.Length - 2).Select((totalCount, index) =>
            {
                var from = (decimal)(histogram.ValueFrom + (index * binSize));
                return new HistogramDistribution((double)from, (double)(from + (decimal)binSize), totalCount);
            });
        }

        public async Task<AssetStatisticsDataDto> GetStatisticsDataAsync(AssetAttributeStatisticsData request, CancellationToken token)
        {
            var start = DateTime.UtcNow;
            var dto = new AssetStatisticsDataDto() { AssetId = request.AssetId };
            try
            {
                var assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(request.AssetId), token);

                dto = new AssetStatisticsDataDto()
                {
                    AssetId = request.AssetId,
                    AssetName = assetDto.Name,
                    AssetNormalizeName = assetDto.NormalizeName,
                    End = request.End,
                    Aggregate = request.Aggregate,
                    GapfillFunction = request.GapfillFunction,
                    ProjectId = request.ProjectId,
                    Start = request.Start,
                    SubscriptionId = request.SubscriptionId,
                    TimeGrain = request.TimeGrain,
                    TimezoneOffset = request.TimezoneOffset
                };

                // aggreagate the data from integration and internal
                var tasks = new List<Task<IEnumerable<Statistics>>>();
                var attributes = assetDto.Attributes.Where(x => request.AttributeIds.Contains(x.Id)).Select(x => Asset.Command.Model.AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                _logger.LogDebug($"Query metadata take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                start = DateTime.UtcNow;
                if (attributes.Any())
                {

                    var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(request.Start).DateTime;
                    var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(request.End).DateTime;
                    // for internal -> call into database
                    var dynamicAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC);
                    if (dynamicAttributes.Any())
                        tasks.Add(_timeSeriesRepository.GetStatisticsAsync(timeStart, timeEnd, dynamicAttributes, request.TimezoneOffset, request.TimeGrain, request.Aggregate, request.GapfillFunction));

                    // for runtime internal -> call into database with runtime table
                    var runtimeAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_RUNTIME == x.AttributeType);
                    if (runtimeAttributes.Any())
                        tasks.Add(_runtimeTimeSeriesRepository.GetStatisticsAsync(timeStart, timeEnd, runtimeAttributes, request.TimezoneOffset, request.TimeGrain, request.Aggregate, request.GapfillFunction));

                    // alias
                    var aliasAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
                    if (aliasAttributes.Any())
                        tasks.Add(_referenceTimeSeries.GetStatisticsAsync(timeStart, timeEnd, aliasAttributes, request.TimezoneOffset, request.TimeGrain, request.Aggregate, request.GapfillFunction));

                    var taskResult = await Task.WhenAll(tasks);
                    var finalResult = taskResult.SelectMany(x => x);
                    _logger.LogDebug($"Query timeseries data take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                    dto.Attributes = (from attribute in attributes
                                      join stats in finalResult on attribute.AttributeId equals stats.AttributeId

                                      select new AssetAttributeStatisticsDataDto()
                                      {
                                          AttributeId = attribute.AttributeId,
                                          AttributeName = attribute.AttributeName,
                                          AttributeNameNormalize = attribute.AttributeNameNormalize,
                                          Distributions = StatisticsDistribution.Create(stats.Q2_Inc, stats.STDev, stats.Min, stats.Max, stats.Q1_Inc, stats.Q3_Inc, stats.Q2_Exc, stats.Q1_Exc, stats.Q3_Exc),                                          
                                          ThousandSeparator = attribute.ThousandSeparator,
                                          GapfillFunction = request.GapfillFunction,
                                          Uom = new GetSimpleUomDto(attribute.UomId, attribute.UomName, attribute.UomAbbreviation)

                                      });
                }

            }
            catch (EntityNotFoundException exc)
            {
                _logger.LogError(exc, $"Entity not found {request.AssetId}", new Dictionary<string, object>() {
                            {"attributes", request.AttributeIds}
                });
            }
            return dto;

        }
    }
}
