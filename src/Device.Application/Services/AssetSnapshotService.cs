using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Historical.Query.Model;
using Device.Application.Historical.Query;
using Device.Application.Repository;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Abstraction;
using System.Linq;
using Device.Domain.Entity;
using System;
using Device.Application.Constant;
using AHI.Infrastructure.Exception;
using Device.Application.SignalQuality.Command.Model;
using Device.Application.Asset.Command.Model;
using System.Diagnostics;
/*
IMPORTANCE: High performance api should be reviewed and approved by technical team.
PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND
Author: Thanh Tran
Email: thanh.tran@yokogawa.com
*/
namespace Device.Application.Service
{
    public class AssetSnapshotService : IAssetSnapshotService
    {
        private readonly IAssetSnapshotRepository _snapshotRepository;
        private readonly IAssetAliasSnapshotRepository _aliasSnapshotRepository;
        private readonly IAssetService _assetService;
        private readonly ILoggerAdapter<AssetTimeSeriesService> _logger;
        private readonly IDeviceSignalQualityService _deviceSignalQualityService;

        public AssetSnapshotService(IAssetService asserService
                                    , IAssetSnapshotRepository snapshotTimeSeriesRepository
                                    , IAssetAliasSnapshotRepository aliasSnapshotRepository
                                    , ILoggerAdapter<AssetTimeSeriesService> logger
                                    , IDeviceSignalQualityService deviceSignalQualityService)
        {
            _snapshotRepository = snapshotTimeSeriesRepository;
            _assetService = asserService;
            _logger = logger;
            _aliasSnapshotRepository = aliasSnapshotRepository;
            _deviceSignalQualityService = deviceSignalQualityService;
        }

        public async Task<IEnumerable<HistoricalDataDto>> GetSnapshotDataAsync(GetHistoricalData command, CancellationToken token)
        {
            try
            {
                Guid guid = Guid.NewGuid();
                _logger.LogInformation($"Request: {guid} | AssetId: {command.AssetId} | Start {nameof(GetSnapshotDataAsync)}");
                var timer = new Stopwatch();
                timer.Start();

                GetAssetDto assetDto;

                try
                {
                    assetDto = await _assetService.FindAssetSnapshotByIdAsync(new Asset.Command.GetAssetById(command.AssetId) { UseCache = command.UseCache }, token);
                    _logger.LogInformation($"Request: {guid} | AssetId: {command.AssetId} | FindAssetSnapshotByIdAsync at {timer.ElapsedMilliseconds} ms");
                }
                catch (EntityValidationException)
                {
                    return Array.Empty<HistoricalDataDto>();
                }

                IEnumerable<HistoricalEntity> attributes;
                if (!command.AttributeIds.Any())
                {
                    // get all attributes
                    attributes = assetDto.Attributes.Select(x => AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                }
                else
                {
                    attributes = assetDto.Attributes.Where(x => command.AttributeIds.Contains(x.Id)).Select(x => AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                }

                var aliasAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
                var tasks = new List<Task<IEnumerable<TimeSeries>>>();
                if (aliasAttributes.Any())
                {
                    tasks.Add(_aliasSnapshotRepository.QueryDataAsync(command.TimezoneOffset, aliasAttributes, command.TimeoutInSecond));
                }

                var deviceSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_STATIC, AttributeTypeConstants.TYPE_DYNAMIC, AttributeTypeConstants.TYPE_INTEGRATION, AttributeTypeConstants.TYPE_RUNTIME, AttributeTypeConstants.TYPE_COMMAND }).Contains(x.AttributeType));
                if (deviceSnapshotAttributes.Any())
                {
                    tasks.Add(_snapshotRepository.QueryDataAsync(command.TimezoneOffset, deviceSnapshotAttributes, command.TimeoutInSecond));
                }

                var snapshots = await Task.WhenAll(tasks);
                var snapshotData = snapshots.SelectMany(x => x);
                _logger.LogInformation($"Request: {guid} | AssetId: {command.AssetId} | QueryDataAsync at {timer.ElapsedMilliseconds} ms");

                var signalQualities = await _deviceSignalQualityService.GetAllSignalQualityAsync();
                _logger.LogInformation($"Request: {guid} | AssetId: {command.AssetId} | GetAllSignalQualityAsync at {timer.ElapsedMilliseconds} ms");

                var result = AssetSnapshotService.Join(attributes, snapshotData, command, signalQualities);
                _logger.LogInformation($"Request: {guid} | AssetId: {command.AssetId} | End {nameof(GetSnapshotDataAsync)} at {timer.ElapsedMilliseconds} ms");

                return result;
            }
            catch (EntityNotFoundException)
            {
                return Array.Empty<HistoricalDataDto>();
            }
        }

        public static IEnumerable<HistoricalDataDto> Join(IEnumerable<HistoricalEntity> attributes, IEnumerable<TimeSeries> timeseriesData, GetHistoricalData request, SignalQualityDto[] signalQualities)
        {
            var assets = attributes.GroupBy(x => new { x.AssetId, x.AssetName, x.AssetNameNormalize });
            return assets.Select(asset => new HistoricalDataDto()
            {
                AssetId = asset.Key.AssetId,
                AssetName = asset.Key.AssetName,
                AssetNormalizeName = asset.Key.AssetNameNormalize,
                Aggregate = request.Aggregate,
                QueryType = request.RequestType,
                TimeGrain = request.TimeGrain,
                Start = request.TimeStart,
                End = request.TimeEnd,
                Attributes = asset.Select(att => new AttributeDto()
                {
                    AttributeId = att.AttributeId,
                    AttributeName = att.AttributeName,
                    AttributeNameNormalize = att.AttributeNameNormalize,
                    AttributeType = att.AttributeType,
                    Uom = att.UomId != null ? new Uom.Command.Model.GetSimpleUomDto()
                    {
                        Id = att.UomId,
                        Name = att.UomName,
                        Abbreviation = att.UomAbbreviation
                    } : null,
                    DecimalPlace = att.DecimalPlace,
                    ThousandSeparator = att.ThousandSeparator,
                    Series = timeseriesData.Where(x => x.AttributeId == att.AttributeId).Select(series => TimeSeriesDto.Create(att.DataType, series, request.IsRawData)).ToList(),
                    GapfillFunction = request.GapfillFunction,
                    DataType = att.DataType,
                    QualityCode = timeseriesData.Where(t => t.AttributeId == att.AttributeId)
                                                .OrderByDescending(t => t.DateTime)
                                                .FirstOrDefault()?
                                                .SignalQualityCode,
                    Quality = signalQualities != null ? GetQualityName(signalQualities, timeseriesData
                                                                .Where(t => t.AttributeId == att.AttributeId)
                                                                .OrderByDescending(t => t.DateTime)
                                                                .FirstOrDefault()?
                                                                .SignalQualityCode) : null,
                    AliasAttributeType = timeseriesData.FirstOrDefault(t => t.AttributeId == att.AttributeId)?.AliasAttributeType
                }).ToList()
            });
        }

        private static string GetQualityName(SignalQualityDto[] signalQualities, int? signalQualityCode)
        {
            if (signalQualityCode != null)
            {
                return signalQualities.FirstOrDefault(x => x.Id == signalQualityCode)?.Name;
            }
            return null;
        }
    }
}
