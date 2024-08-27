using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Domain.Entity;

/*
IMPORTANCE: High performance api should be reviewed and approved by technical team.
PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND
Author: Thanh Tran
Email: thanh.tran@yokogawa.com
*/

namespace Device.Application.Service
{
    public class AssetHistoricalSnapshotService : IAssetHistoricalSnapshotService
    {
        private readonly IAssetHistoricalSnapshotRepository _historicalSnapshotRepository;
        private readonly IAssetService _assetService;
        private readonly ILoggerAdapter<AssetHistoricalSnapshotService> _logger;
        private readonly IAssetRuntimeHistoricalSnapshotRepository _runtimeHistoricalSnapshotRepository;
        private readonly IAssetAliasHistoricalSnapshotRepository _aliasHistoricalSnapshotRepository;
        private readonly IDeviceSignalQualityService _deviceSignalQualityService;
        private readonly ITimeRangeAssetSnapshotRepository _timeRangeSnapshotRepository;
        private readonly IAssetSnapshotRepository _snapshotRepository;

        public AssetHistoricalSnapshotService(
                                      IAssetService assetService
                                    , IAssetHistoricalSnapshotRepository historicalSnapshotRepository
                                    , ILoggerAdapter<AssetHistoricalSnapshotService> logger,
                                    IAssetRuntimeHistoricalSnapshotRepository runtimeHistoricalSnapshotRepository,
                                    IAssetAliasHistoricalSnapshotRepository aliasHistoricalSnapshotRepository,
                                    IDeviceSignalQualityService deviceSignalQualityService,
                                    ITimeRangeAssetSnapshotRepository timeRangeSnapshotRepository,
                                    IAssetSnapshotRepository snapshotRepository)
        {
            _historicalSnapshotRepository = historicalSnapshotRepository;
            _assetService = assetService;
            _logger = logger;
            _runtimeHistoricalSnapshotRepository = runtimeHistoricalSnapshotRepository;
            _aliasHistoricalSnapshotRepository = aliasHistoricalSnapshotRepository;
            _deviceSignalQualityService = deviceSignalQualityService;
            _timeRangeSnapshotRepository = timeRangeSnapshotRepository;
            _snapshotRepository = snapshotRepository;
        }

        public async Task<IEnumerable<HistoricalDataDto>> GetSnapshotDataAsync(GetHistoricalData command, CancellationToken token)
        {
            try
            {
                var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeStart).DateTime;
                var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeEnd).DateTime;
                GetAssetDto assetDto;

                try
                {
                    assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(command.AssetId), token);
                }
                catch (EntityValidationException)
                {
                    return Array.Empty<HistoricalDataDto>();
                }

                var attributes = assetDto.Attributes.Where(x => command.AttributeIds.Contains(x.Id)).Select(x => Asset.Command.Model.AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                var tasks = new List<Task<IEnumerable<TimeSeries>>>();

                var runtimeSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_RUNTIME }).Contains(x.AttributeType));
                if (runtimeSnapshotAttributes.Any())
                {
                    tasks.Add(_runtimeHistoricalSnapshotRepository.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, runtimeSnapshotAttributes, command.TimeoutInSecond, command.GapfillFunction));
                }

                var commandSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_COMMAND }).Contains(x.AttributeType));
                if (commandSnapshotAttributes.Any())
                {
                    tasks.Add(_timeRangeSnapshotRepository.QueryDataAsync(command.TimezoneOffset, commandSnapshotAttributes, command.TimeoutInSecond, timeStart, timeEnd));
                }

                var staticSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_STATIC }).Contains(x.AttributeType));
                if (staticSnapshotAttributes.Any())
                {
                    _logger.LogInformation($"Static is not supported historical data");
                    // get snapshot instead
                    tasks.Add(_snapshotRepository.QueryDataAsync(command.TimezoneOffset, staticSnapshotAttributes, command.TimeoutInSecond));
                }

                var aliasSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_ALIAS }).Contains(x.AttributeType));
                if (aliasSnapshotAttributes.Any())
                {
                    tasks.Add(_aliasHistoricalSnapshotRepository.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, aliasSnapshotAttributes, command.TimeoutInSecond, command.GapfillFunction));
                }

                var deviceSnapshotAttributes = attributes.Where(x => (new[] { AttributeTypeConstants.TYPE_DYNAMIC }).Contains(x.AttributeType));
                if (deviceSnapshotAttributes.Any())
                {
                    tasks.Add(_historicalSnapshotRepository.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, deviceSnapshotAttributes, command.TimeoutInSecond, command.GapfillFunction));
                }

                var timeseries = await Task.WhenAll(tasks);
                // NOTE: Static is not supported historical data
                //       So, get snapshot instead with ts = updatedUtc
                //       Need Where query to adapt historical
                var timeseriesData = timeseries.SelectMany(x => x)
                                               .Where(x => command.TimeStart <= x.UnixTimestamp &&
                                                           x.UnixTimestamp <= command.TimeEnd);
                var signalQualities = await _deviceSignalQualityService.GetAllSignalQualityAsync();

                return AssetSnapshotService.Join(attributes, timeseriesData, command, signalQualities);
            }
            catch (EntityNotFoundException)
            {
                return Array.Empty<HistoricalDataDto>();
            }
        }
    }
}
