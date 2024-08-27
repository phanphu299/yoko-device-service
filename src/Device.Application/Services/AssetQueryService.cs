using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Asset.Command;
using Device.Application.BlockFunction.Model;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class AssetQueryService : IAssetQueryService
    {
        private readonly IBlockExecutionRepository _repository;
        private readonly ILoggerAdapter<AssetQueryService> _logger;

        public AssetQueryService(
            IBlockExecutionRepository repository,
            ILoggerAdapter<AssetQueryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        // Note: this method will be used for ahi-sdk to call
        public async Task<BlockQueryResult> QueryAsync(AssetAttributeQuery command, CancellationToken cancellationToken)
        {
            var blockResult = (BlockQueryResult)null;
            _logger.LogDebug($"AssetQueryService - command - {command.ToJson()}");
            switch (command.Method)
            {
                case "LastValueAsync":
                case "LastValueStringAsync":
                    blockResult = await _repository.GetAssetAttributeSnapshotAsync(command.AssetId, command.AttributeId);
                    break;
                case "SearchNearestAsync":
                case "SearchNearestStringAsync":
                    blockResult = await _repository.GetNearestAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.Padding);
                    break;
                case "LastValueDiffAsync":
                    var lastValueDiff = await _repository.GetLastValueDiffAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.FilterUnit);
                    blockResult = new BlockQueryResult(lastValueDiff);
                    break;
                case "ValueDiffAsync":
                    var valueDiff = await _repository.GetValueDiff2PointsAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.End.Value);
                    blockResult = new BlockQueryResult(valueDiff);
                    break;
                case "LastTimeDiffAsync":
                    var lastTimeDiff = await _repository.GetLastTimeDiffAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.FilterUnit);
                    blockResult = new BlockQueryResult(lastTimeDiff);
                    break;
                case "TimeDiffAsync":
                    var timeDiff2Points = await _repository.GetTimeDiff2PointsAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.End.Value, command.FilterUnit);
                    blockResult = new BlockQueryResult(timeDiff2Points);
                    break;
                case "AggregateAsync":
                    var aggregate = await _repository.AggregateAssetAttributesValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.End.Value, command.Aggregate, command.FilterOperation, command.FilterValue);
                    blockResult = new BlockQueryResult(aggregate);
                    break;
                case "DurationInAsync":
                    var durationIn = await _repository.GetDurationAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.End.Value, command.FilterOperation, command.FilterValue, command.FilterUnit);
                    blockResult = new BlockQueryResult(durationIn);
                    break;
                case "CountInAsync":
                    var countIn = await _repository.GetCountAssetAttributeValueAsync(command.AssetId, command.AttributeId, command.Start.Value, command.End.Value, command.FilterOperation, command.FilterValue);
                    blockResult = new BlockQueryResult(countIn);
                    break;

            }
            _logger.LogDebug($"AssetQueryService - blockResult - {blockResult.ToJson()}");
            return blockResult;
        }
    }
}