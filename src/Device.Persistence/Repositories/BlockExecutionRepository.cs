using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Domain.Entity;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.Exception;
using Device.ApplicationExtension.Extension;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Extension;
using MediatR;
using System.Threading;
namespace Device.Persistence.Repository
{
    public class BlockExecutionRepository : IBlockExecutionRepository
    {
        private readonly IMediator _mediator;
        private readonly IAssetTimeSeriesRepository _assetTimeSeriesRepository;
        private readonly IAssetAliasTimeSeriesRepository _assetAliasTimeSeriesRepository;
        private readonly IAssetRuntimeTimeSeriesRepository _assetRuntimeTimeSeriesRepository;
        private readonly IAssetCommandHistoryHandler _assetCommandHistoryHandler;

        public BlockExecutionRepository(IMediator mediator
                                        , IAssetTimeSeriesRepository assetTimeSeriesRepository
                                        , IAssetRuntimeTimeSeriesRepository assetRuntimeTimeSeriesRepository
                                        , IAssetAliasTimeSeriesRepository assetAliasTimeSeriesRepository
                                        , IAssetCommandHistoryHandler assetCommandHistoryHandler)
        {
            _mediator = mediator;
            _assetTimeSeriesRepository = assetTimeSeriesRepository;
            _assetRuntimeTimeSeriesRepository = assetRuntimeTimeSeriesRepository;
            _assetCommandHistoryHandler = assetCommandHistoryHandler;
            _assetAliasTimeSeriesRepository = assetAliasTimeSeriesRepository;
        }

        public async Task<double> AggregateAssetAttributesValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var deviceId = string.Empty;
                var metricKey = string.Empty;

                if (attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                    deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

                if (attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                    metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

                assetAttribute.DeviceId = deviceId;
                assetAttribute.MetricKey = metricKey;
                var result = await _assetTimeSeriesRepository.AggregateAssetAttributesValueAsync(assetAttribute, start, end, aggregate, filterOperation, filterValue);
                return result;
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.AggregateAssetAttributesValueAsync(assetAttribute, start, end, aggregate, filterOperation, filterValue);
                return result;
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.AggregateAssetAttributesValueAsync(assetAttribute, start, end, aggregate, filterOperation, filterValue);
                return result;
            }
            return 0;
        }

        public async Task<BlockQueryResult> GetAssetAttributeSnapshotAsync(Guid assetId, Guid attributeId)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            if (attribute.IsStaticAttribute())
            {
                return ConstructStaticBlockResult(attribute);
            }
            else
            {
                var snapshot = await _mediator.Send(new Application.Asset.Command.GetAttributeSnapshot(assetId), System.Threading.CancellationToken.None);
                if (snapshot == null)
                {
                    throw new EntityNotFoundException();
                }
                var metric = snapshot.Attributes.First(x => x.AttributeId == attributeId).Series.First();
                return new BlockQueryResult(metric.v, attribute.DataType, metric.ts.ToString().UnixTimeStampToDateTime());
            }
        }

        public async Task SetAssetAttributeValueAsync(Guid assetId, Guid attributeId, params BlockDataRequest[] values)
        {
            var timeSeries = values.Select(x =>
            {
                if (x.DataType == DataTypeConstants.TYPE_TEXT)
                {
                    return new TimeSeries()
                    {
                        DateTime = x.Timestamp,
                        ValueText = x.Value.ToString(),
                        AssetId = assetId,
                        AttributeId = attributeId
                    };
                }
                else
                {
                    return new TimeSeries()
                    {
                        DateTime = x.Timestamp,
                        Value = ParseValueToStore(x.Value, x.DataType),
                        AssetId = assetId,
                        AttributeId = attributeId
                    };
                }
            });
            var asset = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            if (asset.Attributes.FirstOrDefault(x => x.Id == attributeId)?.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
            {
                await _assetCommandHistoryHandler.SaveAssetAttributeValueAsync(timeSeries.ToArray());
            }
            else
            {
                await _assetRuntimeTimeSeriesRepository.SaveAssetAttributeValueAsync(timeSeries.ToArray());
            }
        }

        private double ParseValueToStore(object value, string dataType)
        {
            if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                return (bool)value == true ? 1 : 0;
            }
            return Convert.ToDouble(value);
        }

        public async Task<double> GetDurationAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterOperation, object filterValue, string filterUnit)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetDurationAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetDurationAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetDurationAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return ConvertToTargetUnit(filterUnit, result);
            }
            return 0;
        }

        private double ConvertToTargetUnit(string filterUnit, double value)
        {
            switch (filterUnit)
            {
                case "second":
                case "s":
                case "sec":
                    return value;
                case "m":
                case "minute":
                case "min":
                    return value / 60;
                case "h":
                case "hour":
                    return value / (60 * 60);
                case "day":
                case "d":
                    return value / (24 * 3600);
                default:
                    throw new SystemNotSupportedException(detailCode: MessageConstants.FILTER_UNIT_NOT_SUPPORTED);
            }
        }

        public async Task<BlockQueryResult> GetNearestAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime dateTime, string padding)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetNearestAssetAttributeAsync(dateTime, assetAttribute, padding);
                if (result == null)
                {
                    return null;
                }
                object resultValue = result.ValueText ?? ConvertValue(result.Value.Value, assetAttribute.DataType);
                return new BlockQueryResult(resultValue, assetAttribute.DataType, result.DateTime);
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetNearestAssetAttributeAsync(dateTime, assetAttribute, padding);
                if (result == null)
                {
                    return null;
                }
                object resultValue = result.ValueText ?? ConvertValue(result.Value.Value, assetAttribute.DataType);
                return new BlockQueryResult(resultValue, assetAttribute.DataType, result.DateTime);
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetNearestAssetAttributeAsync(dateTime, assetAttribute, padding);
                if (result == null)
                {
                    return null;
                }
                object resultValue = result.ValueText ?? ConvertValue(result.Value.Value, assetAttribute.DataType);
                return new BlockQueryResult(resultValue, assetAttribute.DataType, result.DateTime);
            }
            return new BlockQueryResult(0, assetAttribute.DataType, DateTime.UtcNow);
        }

        private static BlockQueryResult ConstructStaticBlockResult(AssetAttributeDto attribute)
        {
            if (!attribute.IsStaticAttribute())
                return default;

            var value = attribute.Value;

            // if the asset using template, need to get from the template value
            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.VALUE))
            {
                value = attribute.Payload[PayloadConstants.VALUE];
            }

            // for static, only return the current value
            return new BlockQueryResult(StringExtension.ParseValue(value?.ToString(), attribute.DataType), attribute.DataType, attribute.UpdatedUtc);
        }

        static object ConvertValue(double value, string dataType)
        {
            object resultValue = value;
            if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                if ((int)value == 1)
                {
                    resultValue = true;
                }
                else
                {
                    resultValue = false;
                }
            }
            return resultValue;
        }

        public async Task<double> GetLastTimeDiffAssetAttributeValueAsync(Guid assetId, Guid attributeId, string filterUnit)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetLastTimeDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetLastTimeDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetLastTimeDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            return 0;
        }

        public async Task<double> GetLastValueDiffAssetAttributeValueAsync(Guid assetId, Guid attributeId, string filterUnit)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
            {
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID].ToString();
            }

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
            {
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY].ToString();
            }

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetLastValueDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetLastValueDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetLastValueDiffAssetAttributeAsync(assetAttribute);
                return ConvertToTargetUnit(filterUnit, result);
            }
            return 0;
        }

        public async Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterUnit)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetTimeDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetTimeDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return ConvertToTargetUnit(filterUnit, result);
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetTimeDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return ConvertToTargetUnit(filterUnit, result);
            }
            return 0;
        }

        public async Task<double> GetValueDiff2PointsAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetValueDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return result;
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetValueDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return result;
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetValueDiff2PointsAssetAttributeValueAsync(assetAttribute, start, end);
                return result;
            }
            return 0;
        }

        public async Task<int> GetCountAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterOperation, object filterValue)
        {
            var assetDto = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
            var attribute = assetDto.Attributes.First(x => attributeId == x.Id);
            var deviceId = string.Empty;
            var metricKey = string.Empty;

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID))
                deviceId = attribute.Payload[PayloadConstants.DEVICE_ID]?.ToString();

            if (attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY))
                metricKey = attribute.Payload[PayloadConstants.METRIC_KEY]?.ToString();

            var assetAttribute = new HistoricalEntity()
            {
                AssetId = assetId,
                AttributeId = attributeId,
                DeviceId = deviceId,
                MetricKey = metricKey,
                DataType = attribute.DataType
            };

            if (attribute.IsDynamicAttribute())
            {
                var result = await _assetTimeSeriesRepository.GetCountAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return result;
            }
            else if (attribute.IsRuntimeAttribute())
            {
                var result = await _assetRuntimeTimeSeriesRepository.GetCountAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return result;
            }
            else if (attribute.IsAliasAttribute())
            {
                var result = await _assetAliasTimeSeriesRepository.GetCountAssetAttributeValueAsync(assetAttribute, start, end, filterOperation, filterValue);
                return result;
            }
            return 0;
        }

        public Task<FunctionBlockExecution> FindAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public IQueryable<FunctionBlockExecution> AsQueryable()
        {
            throw new NotImplementedException();
        }

        public IQueryable<FunctionBlockExecution> AsFetchable()
        {
            throw new NotImplementedException();
        }
    }
}