using System.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Device.Application.BlockFunction.Model;
using Device.Application.BlockFunction.Query;
using Device.Application.Constant;
using Device.Application.Events;
using AHI.Infrastructure.Exception;
using Device.Application.FunctionBlock.Command;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Model;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.SharedKernel.Abstraction;
using System.Globalization;
using System.Text;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Domain.Entity;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.BlockFunction.Trigger.Model;
using MediatR;
using Device.Application.Asset.Command.Model;
using FluentValidation;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Asset.Command;
using Device.ApplicationExtension.Extension;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Extension;
using Device.Application.Model;
namespace Device.Application.Service
{
    public class FunctionBlockExecutionService : BaseSearchService<Domain.Entity.FunctionBlockExecution, Guid, GetFunctionBlockExecutionByCriteria, FunctionBlockExecutionDto>, IFunctionBlockExecutionService
    {
        private readonly IFunctionBlockExecutionResolver _blockFunctionResolver;
        private readonly ICache _cache;
        private readonly IAssetUnitOfWork _unitOfWork;
        private readonly IBlockFunctionUnitOfWork _blockFunctionUnitOfWork;
        private readonly ITenantContext _tenantContext;
        private readonly IBlockEngine _engine;
        private readonly IBlockTriggerHandler _blockTriggerHandler;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggerAdapter<FunctionBlockExecutionService> _logger;
        private readonly IDeviceFunction _deviceFunction;
        private readonly IMediator _mediator;
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly IUserContext _userContext;
        private readonly IFunctionBlockWriterHandler _blockWriterHandler;
        private readonly IValidator<ArchiveFunctionBlockExecutionDto> _validator;
        private readonly IReadFunctionBlockRepository _readFunctionBlockRepository;
        private readonly IReadFunctionBlockExecutionRepository _readFunctionBlockExecutionRepository;
        private readonly string[] _inputTypes = { BindingTypeConstants.INPUT, BindingTypeConstants.INOUT };
        private readonly string[] _outputTypes = { BindingTypeConstants.OUTPUT, BindingTypeConstants.INOUT };
        private readonly IDictionary<string, Func<Guid, string, Task<FunctionBlockNodeMapping>>> _mappingHandler;
        private readonly IAssetTableService _assetTableService;
        public FunctionBlockExecutionService(IServiceProvider serviceProvider
                                    , IFunctionBlockExecutionResolver blockFunctionResolver
                                    , ICache cache
                                    , IAssetUnitOfWork unitOfWork
                                    , IBlockFunctionUnitOfWork blockFunctionUnitOfWork
                                    , ITenantContext tenantContext
                                    , IBlockEngine engine
                                    , IBlockTriggerHandler blockTriggerHandler
                                    , IAuditLogService auditLogService
                                    , IDeviceFunction deviceFunction
                                    , ILoggerAdapter<FunctionBlockExecutionService> logger
                                    , IMediator mediator
                                    , IDomainEventDispatcher domainEventDispatcher
                                    , IAssetSnapshotService assetSnapshotService
                                    , IUserContext userContext
                                    , IConfiguration configuration
                                    , IAssetTableService assetTableService
                                    , IFunctionBlockWriterHandler blockWriterHandler
                                    , IValidator<ArchiveFunctionBlockExecutionDto> validator
                                    , IReadFunctionBlockRepository readFunctionBlockRepository
                                    , IReadFunctionBlockExecutionRepository readFunctionBlockExecutionRepository)
            : base(FunctionBlockExecutionDto.Create, serviceProvider)
        {
            _blockFunctionResolver = blockFunctionResolver;
            _cache = cache;
            _unitOfWork = unitOfWork;
            _blockFunctionUnitOfWork = blockFunctionUnitOfWork;
            _tenantContext = tenantContext;
            _engine = engine;
            _blockTriggerHandler = blockTriggerHandler;
            _auditLogService = auditLogService;
            _logger = logger;
            _deviceFunction = deviceFunction;
            _mediator = mediator;
            _domainEventDispatcher = domainEventDispatcher;
            _userContext = userContext;
            _blockWriterHandler = blockWriterHandler;
            _mappingHandler = new Dictionary<string, Func<Guid, string, Task<FunctionBlockNodeMapping>>>()
            {
                {BindingDataTypeIdConstants.TYPE_ASSET_TABLE, HandleAssetTableMappingAsync},
                {BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE, HandleAssetAttributeMappingAsync}
            };
            _assetTableService = assetTableService;
            _validator = validator;
            _readFunctionBlockRepository = readFunctionBlockRepository;
            _readFunctionBlockExecutionRepository = readFunctionBlockExecutionRepository;
        }

        public async Task<FunctionBlockExecutionDto> AddFunctionBlockExecutionAsync(AddFunctionBlockExecution command, CancellationToken token)
        {
            var entity = AddFunctionBlockExecution.Create(command);
            try
            {
                await _blockFunctionUnitOfWork.BeginTransactionAsync();
                if (await IsDuplicateNameAsync(Guid.Empty, command.Name))
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(command.Name));
                }
                entity.CreatedBy = _userContext.Upn;
                if (entity.TemplateId.HasValue)
                {
                    await ConstructBlockExecutionMappingAsync(entity.TemplateId.Value, command.AssetMappings, command.TriggerMapping, entity);
                }
                else if (entity.FunctionBlockId.HasValue)
                {
                    await CheckExistFunctionBlockAsync((Guid)entity.FunctionBlockId);
                }

                await ConstructBlockExecutionTriggerAsync(command.TriggerMapping, entity);
                ConstructBlockExecutionMappingForUsingAssetAttribute(entity);

                var hasError = await ValidateFunctionBlockExecutionAsync(entity, DateTime.UtcNow, DateTime.UtcNow);
                if (hasError)
                {
                    entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                    LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.VALIDATION_FAIL);
                }

                if (entity.RunImmediately && entity.Status != BlockExecutionStatusConstants.STOPPED_ERROR)
                {
                    await _blockTriggerHandler.RegisterAsync(entity);
                    entity.Status = BlockExecutionStatusConstants.RUNNING; // update to publish
                                                                           // build the latest execution information

                    var snapshot = await ConstructBlockExecutionSnapshotAsync(entity);
                    entity.ExecutionContent = JsonConvert.SerializeObject(snapshot);
                    if (entity.FunctionBlockId != null)
                        await UpdateVersionByFunctionBlockAsync(entity);
                }
                else if (entity.Status != BlockExecutionStatusConstants.STOPPED_ERROR)
                {
                    entity.Status = BlockExecutionStatusConstants.STOPPED;
                }

                var trackingEntity = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AddAsync(entity);
                await _blockFunctionUnitOfWork.CommitAsync();
                entity.Mappings.Clear();
                await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Add, ActionStatus.Success, trackingEntity.Id, trackingEntity.Name, command);
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"Project: {_tenantContext.ProjectId} Add Execution Failed with unexpected exception: {e.Message}");
                await _blockFunctionUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Add, e, payload: new { Request = command, Message = e.Message });
                throw;
            }
            return FunctionBlockExecutionDto.Create(entity);
        }

        private async Task CheckExistFunctionBlockAsync(Guid functionBlockId)
        {
            var exists = await _readFunctionBlockRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == functionBlockId);
            if (!exists)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(FunctionBlockExecution.TemplateId));
            }
        }

        private async Task ConstructBlockExecutionMappingAsync(Guid templateId, IEnumerable<AssetMappingDto> assetMappings, AssetMappingDto triggerMapping, FunctionBlockExecution entity)
        {
            try
            {
                entity.Mappings.Clear();
                var template = await _mediator.Send(new BlockTemplate.Query.GetBlockTemplateById(templateId), CancellationToken.None);
                var mappings = (from node in template.Nodes
                                join m in assetMappings on node.AssetMarkupName?.ToLower() equals m.AssetMarkupName?.ToLower() into gj
                                from map in gj.DefaultIfEmpty()
                                select new { Node = node, Map = map }).ToList();
                foreach (var mapping in mappings)
                {
                    if (mapping.Map != null && mapping.Map.AssetId != null)
                    {
                        var assetId = mapping.Map.AssetId.Value;
                        var bindingType = mapping.Node.Function.Bindings.First();
                        if (_mappingHandler.ContainsKey(bindingType.DataType))
                        {
                            var entityMapping = await _mappingHandler[bindingType.DataType].Invoke(assetId, mapping.Node.TargetName);
                            entityMapping.BlockExecutionId = entity.Id;
                            entityMapping.BlockTemplateNodeId = mapping.Node.Id;
                            entityMapping.AssetMarkupName = mapping.Node.AssetMarkupName;
                            entityMapping.AssetName = mapping.Map?.AssetName;
                            entity.Mappings.Add(entityMapping);
                        }
                    }
                    else
                    {
                        // can be a primitive type
                        entity.Mappings.Add(new FunctionBlockNodeMapping()
                        {
                            BlockExecutionId = entity.Id,
                            BlockTemplateNodeId = mapping.Node.Id,
                            AssetMarkupName = mapping.Node.AssetMarkupName,
                            AssetName = mapping.Map?.AssetName,
                            Value = mapping.Node.TargetName
                        });
                    }
                }
                if (string.IsNullOrEmpty(entity.TriggerType))
                {
                    // set from template
                    entity.TriggerContent = template.TriggerContent;
                    entity.TriggerType = template.TriggerType;
                }
                entity.Version = template.Version;
            }
            catch (EntityNotFoundException)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(FunctionBlockExecution.TemplateId));
            }
        }

        private async Task ConstructBlockExecutionTriggerAsync(AssetMappingDto triggerMapping, FunctionBlockExecution entity)
        {
            switch (entity.TriggerType)
            {
                case BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT:
                    try
                    {
                        entity.TriggerAssetMarkup = triggerMapping?.AssetMarkupName;

                        var triggerDto = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(entity.TriggerContent);
                        var assetId = triggerMapping?.AssetId ?? triggerDto.AssetId;
                        if (assetId == null)
                        {
                            entity.TriggerAssetId = null;
                            entity.TriggerAttributeId = null;
                            entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                            LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.TRIGGER_ASSET_NOT_FOUND);
                            return;
                        }
                        var asset = await _mediator.Send(new Asset.Command.GetAssetById((Guid)assetId, false), CancellationToken.None);
                        entity.TriggerAssetId = asset.Id;
                        var attribute = asset.Attributes.SingleOrDefault(x => x.Name == triggerDto.AttributeName);
                        entity.TriggerAttributeId = attribute?.Id;
                        if (entity.TriggerAttributeId == null)
                        {
                            entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                            LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.TRIGGER_ATTRIBUTE_NOT_FOUND);
                        }
                        else
                        {
                            if (!entity.Mappings.Any(m => string.Equals(m.AssetMarkupName, entity.TriggerAssetMarkup, StringComparison.InvariantCultureIgnoreCase)
                                                        && string.Equals(m.AssetName, asset.Name, StringComparison.InvariantCultureIgnoreCase)
                                                        && string.Equals(m.TargetName, attribute.Name, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                entity.Mappings.Add(new FunctionBlockNodeMapping
                                {
                                    AssetId = asset.Id,
                                    AssetName = asset.Name,
                                    AssetMarkupName = entity.TriggerAssetMarkup,
                                    TargetName = attribute.Name,
                                    BlockExecutionId = entity.Id,
                                    Value = attribute.Id.ToString()
                                });
                            }
                        }
                    }
                    catch (EntityValidationException ex)
                    {
                        entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                        LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.CONSTRUCT_TRIGGER_FAIL, ex);
                    }
                    break;
            }
        }

        private void ConstructBlockExecutionMappingForUsingAssetAttribute(FunctionBlockExecution entity)
        {
            var hasError = false;
            _logger.LogDebug($"Construct mapping from diagram content for using Asset Attribute!");
            var diagramContent = JsonConvert.DeserializeObject<FunctionExecutionContent>(entity.DiagramContent);
            var configPayloads = diagramContent.Layers.Where(x => !x.IsDiagramLink)
                                                        .SelectMany(x => x.Models)
                                                        .SelectMany(x => x.Value.Ports)
                                                        .Select(x => x.Extras?.Config?.Payload);
            var dicAssetAttribute = new Dictionary<Guid, (Guid AssetId, string AssetName, string AttributeName)>();
            foreach (var config in configPayloads.Where(cp => cp != null && cp.ContainsKey(PayloadConstants.ASSET_ID) && cp.ContainsKey(PayloadConstants.ATTRIBUTE_ID)))
            {
                try
                {
                    var attributeId = Guid.Parse(config[PayloadConstants.ATTRIBUTE_ID].ToString());
                    if (!dicAssetAttribute.ContainsKey(attributeId))
                    {
                        var assetId = Guid.Parse(config[PayloadConstants.ASSET_ID].ToString());
                        var assetName = config[PayloadConstants.ASSET_NAME].ToString();
                        var attributeName = config[PayloadConstants.ATTRIBUTE_NAME].ToString();
                        dicAssetAttribute.Add(attributeId, (assetId, assetName, attributeName));
                    }
                }
                catch (System.Exception e)
                {
                    hasError = true;
                    _logger.LogDebug(e, $"Cannot construct mapping from diagram content for using Asset Attribute of config {JsonConvert.SerializeObject(config)}");
                }
            }
            foreach (var item in dicAssetAttribute)
            {
                if (!entity.Mappings.Any(m => m.AssetId == item.Value.AssetId
                                            && m.AssetName == item.Value.AssetName
                                            && m.Value == item.Key.ToString()
                                            && m.TargetName == item.Value.AttributeName))
                {
                    entity.Mappings.Add(new FunctionBlockNodeMapping
                    {
                        AssetId = item.Value.AssetId,
                        AssetName = item.Value.AssetName,
                        TargetName = item.Value.AttributeName,
                        BlockExecutionId = entity.Id,
                        Value = item.Key.ToString(),
                    });
                }
            }
            if (hasError)
            {
                entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.CONSTRUCT_MAPPING_FAIL);
            }
        }

        private async Task<FunctionBlockNodeMapping> HandleAssetTableMappingAsync(Guid assetId, string tableName)
        {
            var mapping = new FunctionBlockNodeMapping();
            try
            {
                var asset = await _mediator.Send(new Asset.Command.GetAssetById(assetId, false), CancellationToken.None);

                mapping.AssetId = asset.Id;
                mapping.AssetName = asset.Name;
                mapping.TargetName = tableName;

                var tableId = await _assetTableService.FetchAssetTableAsync(assetId, tableName);
                mapping.Value = tableId?.ToString();
            }
            catch (System.Exception e)
            {
                _logger.LogDebug(e, $"Handle Asset Table mapping failed for asset {assetId}");
            }
            return mapping;
        }

        private async Task<FunctionBlockNodeMapping> HandleAssetAttributeMappingAsync(Guid assetId, string attributeName)
        {
            var mapping = new FunctionBlockNodeMapping();
            try
            {
                var asset = await _mediator.Send(new Asset.Command.GetAssetById(assetId, false), CancellationToken.None);
                mapping.AssetId = asset.Id;
                mapping.AssetName = asset.Name;
                mapping.TargetName = attributeName;
                var attribute = asset.Attributes.FirstOrDefault(x => string.Equals(attributeName, x.Name, StringComparison.InvariantCultureIgnoreCase));
                mapping.Value = attribute?.Id.ToString();
            }
            catch (EntityValidationException) { }
            catch (System.Exception e)
            {
                _logger.LogDebug(e, $"Handle Asset Attribute mapping failed for asset {assetId}");
                throw;
            }
            return mapping;
        }

        private Task<bool> IsDuplicateNameAsync(Guid id, string name)
        {
            return _readFunctionBlockExecutionRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Name.ToLower() == name.ToLower() && x.Id != id);
        }

        private async Task<bool> ValidateFunctionBlockExecutionAsync(Domain.Entity.FunctionBlockExecution entity, DateTime dateTime, DateTime? snapshotTimestamp)
        {
            var hasError = false;
            try
            {
                var functionTemplateContentResult = await GetFunctionTemplateContentAsync(entity.TemplateId, entity.FunctionBlockId);
                var validationContext = new BlockExecutionValidationContext(entity.TriggerType, entity.TriggerContent);
                var validator = ConstructBlockExecutionValidator(_unitOfWork, _blockFunctionResolver);

                var inputs = await GetExecutionInputInformationAsync(entity, functionTemplateContentResult); // Not changed - Not using `content` so move out of the foreach.
                foreach (var content in functionTemplateContentResult.Contents)
                {
                    var outputs = GetExecutionOutputInformation(entity, content, functionTemplateContentResult);
                    var variable = PrepareExecutionVariable(inputs);

                    variable.Set(BlockExecutionConstants.SYSTEM_TRIGGER_DATETIME, dateTime);
                    if (snapshotTimestamp != null)
                    {
                        variable.Set(BlockExecutionConstants.SYSTEM_SNAPSHOT_DATETIME, snapshotTimestamp.Value);
                    }

                    validationContext.SetBlockTemplateContent(content);
                    validationContext.SetInputs(inputs);
                    validationContext.SetOutputs(outputs);
                    validationContext.SetVariable(variable);

                    validator.SetValidationContext(validationContext);
                    await validator.ValidateAsync();
                }
            }
            catch
            {
                hasError = true;
            }
            return hasError;
        }

        private void LogBlockExecutionError(Guid blockExecutionId, string rootCauseMessage, System.Exception ex = null)
        {
            _logger.LogError(ex, string.Format(BlockExecutionMessageConstants.BLOCK_EXECUTION_ERROR, blockExecutionId, rootCauseMessage));
        }

        private BaseBlockExecutionValidator ConstructBlockExecutionValidator(IAssetUnitOfWork unitOfWork, IFunctionBlockExecutionResolver blockExecutionResolver)
        {
            var codeBlockExecutionValidator = new CodeBlockExecutionValidator(unitOfWork, blockExecutionResolver, null);
            var startEndBlockExecutionValidator = new StartEndBlockExecutionValidator(unitOfWork, blockExecutionResolver, codeBlockExecutionValidator);
            var bindingBlockExecutionValidator = new BindingBlockExecutionValidator(unitOfWork, blockExecutionResolver, startEndBlockExecutionValidator);
            return bindingBlockExecutionValidator;
        }

        public async Task<FunctionBlockExecutionDto> UpdateFunctionBlockExecutionAsync(UpdateFunctionBlockExecution command, CancellationToken token)
        {
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {

                var functionBlockExecution = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable().FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken: token);
                if (functionBlockExecution == null)
                {
                    throw new EntityNotFoundException();
                }

                if (await IsDuplicateNameAsync(command.Id, command.Name))
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(command.Name));
                }

                var currentStatus = functionBlockExecution.Status;
                var entity = UpdateFunctionBlockExecution.Create(command);

                // set status
                if (currentStatus == BlockExecutionStatusConstants.STOPPED_ERROR)
                    entity.Status = BlockExecutionStatusConstants.STOPPED;
                else
                    entity.Status = currentStatus;

                entity.Version = functionBlockExecution.Version;

                if (entity.TemplateId.HasValue)
                {
                    await ConstructBlockExecutionMappingAsync(entity.TemplateId.Value, command.AssetMappings, command.TriggerMapping, entity);
                }
                else if (entity.FunctionBlockId.HasValue)
                {
                    await CheckExistFunctionBlockAsync((Guid)entity.FunctionBlockId);
                }

                await ConstructBlockExecutionTriggerAsync(command.TriggerMapping, entity);
                ConstructBlockExecutionMappingForUsingAssetAttribute(entity);
                await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpsertBlockNodeMappingAsync(entity.Mappings, command.Id);

                var hasError = await ValidateFunctionBlockExecutionAsync(entity, DateTime.UtcNow, DateTime.UtcNow);
                if (hasError)
                {
                    if (functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING || functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING_OBSOLETE)
                        await _blockTriggerHandler.UnregisterAsync(functionBlockExecution);
                    entity.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                    LogBlockExecutionError(entity.Id, BlockExecutionMessageConstants.VALIDATION_FAIL);
                }
                await UpdateStatusFunctionBlockExecutionAsync(entity, functionBlockExecution, command, currentStatus);

                var trackingEntity = await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpdateAsync(command.Id, entity);
                await _blockFunctionUnitOfWork.CommitAsync();

                var hashKey = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
                var hashField = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(command.Id);
                await _cache.DeleteHashByKeyAsync(hashKey, hashField);

                await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Update, ActionStatus.Success, trackingEntity.Id, trackingEntity.Name, command);
                return FunctionBlockExecutionDto.Create(trackingEntity);
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"Project: {_tenantContext.ProjectId} Update Execution Failed with unexpected exception: {e.Message}");
                await _blockFunctionUnitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Update, e, command.Id, command.Name, payload: new { Request = command, Message = e.Message });
                throw;
            }
        }

        private async Task UpdateStatusFunctionBlockExecutionAsync(FunctionBlockExecution entity, FunctionBlockExecution functionBlockExecution, UpdateFunctionBlockExecution command, string currentStatus)
        {
            if (entity.RunImmediately)
            {
                // check running status
                if (functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING_OBSOLETE || functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING)
                {
                    await _blockTriggerHandler.UnregisterAsync(functionBlockExecution);
                }
                // validation error -> skip register event
                if (entity.Status != BlockExecutionStatusConstants.STOPPED_ERROR)
                {
                    await _blockTriggerHandler.RegisterAsync(entity);

                    var snapshot = await ConstructBlockExecutionSnapshotAsync(entity);
                    entity.ExecutionContent = JsonConvert.SerializeObject(snapshot);
                    entity.Status = BlockExecutionStatusConstants.RUNNING;
                }
                // update block execution to function block version
                if (entity.FunctionBlockId != null)
                    await UpdateVersionByFunctionBlockAsync(entity);

            }
            else if (currentStatus == BlockExecutionStatusConstants.RUNNING && CheckExecutionChanged(functionBlockExecution, command))
            {
                entity.Status = BlockExecutionStatusConstants.RUNNING_OBSOLETE;
            }
        }

        private bool CheckExecutionChanged(FunctionBlockExecution functionBlockExecution, UpdateFunctionBlockExecution command)
        {
            if (functionBlockExecution.TemplateId != command.TemplateId)
                return true;

            var diagramChanged = CheckDiagramChanged(functionBlockExecution.DiagramContent, command.DiagramContent);
            if (diagramChanged)
                return true;

            var triggerChanged = CheckTriggerChanged(functionBlockExecution, command);
            if (triggerChanged)
                return true;

            return false;
        }

        private bool CheckDiagramChanged(string currentContent, string requestContent)
        {
            // current DiagramContent
            var currentAttributeIds = ParseDiagramContent(currentContent);

            // request DiagramContent
            var requestAttributeIds = ParseDiagramContent(requestContent);

            return !currentAttributeIds.SequenceEqual(requestAttributeIds);
        }

        private IEnumerable<string> ParseDiagramContent(string diagramContent)
        {
            var payload = JsonConvert.DeserializeObject<FunctionExecutionContent>(diagramContent);
            var nodes = payload.Layers.Where(x => !x.IsDiagramLink)
                                                        .SelectMany(x => x.Models)
                                                        .SelectMany(x => x.Value.Ports)
                                                        .Select(x => x.Extras?.Config?.Payload);
            var castToJson = JsonConvert.SerializeObject(nodes);
            var nodeParsers = JsonConvert.DeserializeObject<List<BlockExecutionPayload>>(castToJson);

            var lstAssetAttributeConnector = nodeParsers.Where(x => x != null && x.AssetId != null && x.AttributeId != null).Select(x => x.AttributeId.ToString());
            var lstAssetTableConnector = nodeParsers.Where(x => x != null && x.AssetId != null && x.TableId != null).Select(x => x.TableId.ToString());
            return lstAssetAttributeConnector.Union(lstAssetTableConnector);
        }

        private bool CheckTriggerChanged(FunctionBlockExecution functionBlockExecution, UpdateFunctionBlockExecution command)
        {
            if (!string.Equals(functionBlockExecution.TriggerType, command.TriggerType, StringComparison.InvariantCultureIgnoreCase))
                return true;

            if (command.TriggerType == BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT)
            {
                var triggerCurrent = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(functionBlockExecution.TriggerContent);
                var triggerRequest = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(command.TriggerContent);
                if (triggerCurrent.AssetMarkup != triggerRequest.AssetMarkup
                    || triggerCurrent.AssetId != triggerRequest.AssetId
                    || triggerCurrent.AttributeId != triggerRequest.AttributeId)
                {
                    return true;
                }
            }
            else
            {
                var triggerCurrent = JsonConvert.DeserializeObject<SchedulerTriggerDto>(functionBlockExecution.TriggerContent);
                var triggerRequest = JsonConvert.DeserializeObject<SchedulerTriggerDto>(command.TriggerContent);
                if (triggerCurrent.Cron != triggerRequest.Cron
                    || triggerCurrent.TimeZoneName != triggerRequest.TimeZoneName
                    || triggerCurrent.Start != triggerRequest.Start
                    || triggerCurrent.End != triggerRequest.End)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<BaseResponse> DeleteFunctionBlockExecutionAsync(DeleteFunctionBlockExecution payload, CancellationToken token)
        {
            var idsToBeDeleted = payload.BlockFunctions.Select(x => x.Id).ToList();
            var deleteNames = new List<string>();
            try
            {
                await UnpublishFunctionBlockExecutionAsync(payload, idsToBeDeleted);
            }
            catch (System.Exception e)
            {
                if (!payload.IsListDelete)
                    await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, e, new Guid[] { payload.Id }, deleteNames, payload: payload);
                else
                    await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, e, payload.BlockFunctions.Select(x => x.Id), deleteNames, payload: payload);
                throw;
            }
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {
                await RemoveFunctionBlockExecutionAsync(payload, idsToBeDeleted, deleteNames);
            }
            catch (System.Exception e)
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                if (!payload.IsListDelete)
                    await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, e, new Guid[] { payload.Id }, deleteNames, payload: payload);
                else
                    await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, e, payload.BlockFunctions.Select(x => x.Id), deleteNames, payload: payload);
                throw;
            }

            return BaseResponse.Success;
        }

        private async Task UnpublishFunctionBlockExecutionAsync(DeleteFunctionBlockExecution payload, IEnumerable<Guid> idsToBeDeleted)
        {
            if (!payload.IsListDelete)
            {
                await UnpublishFunctionBlockExecutionAsync(payload.Id);
            }
            else
            {
                foreach (var id in idsToBeDeleted)
                {
                    await UnpublishFunctionBlockExecutionAsync(id);
                }
            }
        }

        private async Task RemoveFunctionBlockExecutionAsync(DeleteFunctionBlockExecution payload, IEnumerable<Guid> idsToBeDeleted, ICollection<string> deleteNames)
        {
            if (!payload.IsListDelete)
            {
                var deletedName = await RemoveBlockFunctionProcessAsync(payload.Id);
                deleteNames.Add(deletedName);
                await _blockFunctionUnitOfWork.CommitAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, ActionStatus.Success, new Guid[] { payload.Id }, deleteNames, payload: payload);
            }
            else
            {
                if (payload.BlockFunctions != null && payload.BlockFunctions.Any())
                {
                    foreach (var id in idsToBeDeleted)
                    {
                        var deletedName = await RemoveBlockFunctionProcessAsync(id);
                        deleteNames.Add(deletedName);
                    }

                    await _blockFunctionUnitOfWork.CommitAsync();
                    await _auditLogService.SendLogAsync(ActivityEntityAction.BLOCK_FUNCTION, ActionType.Delete, ActionStatus.Success, payload.BlockFunctions.Select(x => x.Id), deleteNames, payload: payload);

                    var tasks = idsToBeDeleted.Select(id => _cache.DeleteHashByKeyAsync(CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId), CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(id)));
                    await Task.WhenAll(tasks);
                }
            }
        }

        private async Task<string> RemoveBlockFunctionProcessAsync(Guid id)
        {
            var entity = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED);

            if (entity.Status == "AC")
                throw new EntityInvalidException(detailCode: MessageConstants.BLOCK_FUNCTION_HAS_BEEN_PUBLISHED);

            await _blockFunctionUnitOfWork.FunctionBlockExecutions.RemoveAsync(id);
            return entity.Name;
        }

        private async Task<BlockExecutionSnapshot> GetBlockExecutionSnapshotAsync(Guid id)
        {
            var hashKey = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            var hashField = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(id);
            var snapshot = await _cache.GetHashByKeyAsync<BlockExecutionSnapshot>(hashKey, hashField);

            if (snapshot == null)
            {
                // get from database
                var blockContentString = await _readFunctionBlockExecutionRepository.AsQueryable().AsNoTracking().Where(x => x.Id == id).Select(x => x.ExecutionContent).FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(blockContentString))
                {
                    snapshot = JsonConvert.DeserializeObject<BlockExecutionSnapshot>(blockContentString);
                    await _cache.SetHashByKeyAsync(hashKey, hashField, snapshot);
                }
            }

            return snapshot;
        }

        public async Task<bool> ExecuteFunctionBlockExecutionAsync(Guid id, DateTime start, DateTime? snapshotDateTime)
        {
            IEnumerable<FunctionBlockOutputBinding> outputBindings = null;
            try
            {
                var snapshot = await GetBlockExecutionSnapshotAsync(id);
                if (snapshot != null)
                {
                    await ValidateTriggerAttributeAsync(snapshot);
                    var contents = snapshot.Information;
                    var tasks = contents.Select(x => RunBlockExecutionAsync(id, x, start, snapshotDateTime));
                    var runResults = await Task.WhenAll(tasks);
                    var hasError = runResults.Any(result => !result.Status);
                    await UpdateBlockExecutionPostRunStatusAsync(id, start, hasError);
                    outputBindings = runResults.SelectMany(result => result.Data);
                    if (hasError)
                    {
                        return false;
                    }
                }
                else
                {
                    _logger.LogError($"Block Execution is not found {id}");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Terrible error !!! - please check the function block execution {id}");
                await UpdateBlockExecutionPostRunStatusAsync(id, start, true);
                throw;
            }

            if (outputBindings.Any() && outputBindings.Any(x => x.Type == BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE))
            {
                // need to find the real assetId
                var finalAssetChangedList = new List<AssetAttributeDto>();
                foreach (var asset in outputBindings.Where(x => x.Type == BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE))
                {
                    if (!finalAssetChangedList.Any(x => x.ToString() == asset.ToString()))
                    {
                        var assetAttributeBiding = (AssetAttributeBinding)asset;
                        finalAssetChangedList.Add(new AssetAttributeDto()
                        {
                            AssetId = assetAttributeBiding.AssetId,
                            Id = assetAttributeBiding.AttributeId.Value
                        });
                    }
                }
                await _deviceFunction.CalculateRuntimeBasedOnTriggerAsync(finalAssetChangedList);

                var notificationTasks = new List<Task>();
                // then notify the client
                // should not notify in device function
                foreach (var assetId in outputBindings.Select(x => x.AssetId).Distinct())
                {
                    notificationTasks.Add(_domainEventDispatcher.SendAsync(new AssetAttributeChangedEvent(assetId, 0, _tenantContext)));
                }
                await Task.WhenAll(notificationTasks);
            }

            return true;
        }

        private async Task ValidateTriggerAttributeAsync(BlockExecutionSnapshot snapshot)
        {
            if (snapshot.TriggerType == BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT)
            {
                var triggerContent = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(snapshot.TriggerContent);
                if (!triggerContent.AssetId.HasValue || !triggerContent.AttributeId.HasValue)
                {
                    // invalid asset attribute type trigger content: content does not contain assetId and attributeId
                    throw new System.Exception("Block Execution Trigger Content invalid.");
                }

                var asset = await _mediator.Send(new Asset.Command.GetAssetById(triggerContent.AssetId.Value, false), CancellationToken.None);
                if (!asset.Attributes.Any(attribute => attribute.Id == triggerContent.AttributeId))
                {
                    // invalid attribute trigger : attributeId does not exist or was deleted
                    throw new System.Exception("Trigger Attribute does not existed.");
                }
            }
        }

        private async Task<(bool Status, IEnumerable<FunctionBlockOutputBinding> Data)> RunBlockExecutionAsync(Guid id, BlockExecutionInformation information, DateTime start, DateTime? snapshotDateTime)
        {
            var processSuccessful = true;
            IBlockVariable variable = null;
            try
            {
                variable = PrepareExecutionVariable(information.Inputs);
                // execute the function block
                variable.Set(BlockExecutionConstants.SYSTEM_TRIGGER_DATETIME, start);
                // add the default value to the variable.
                if (snapshotDateTime != null)
                {
                    variable.Set(BlockExecutionConstants.SYSTEM_SNAPSHOT_DATETIME, snapshotDateTime.Value);
                }

                var blockFunctionInstance = _blockFunctionResolver.ResolveInstance(information.Content);
                blockFunctionInstance.SetVariable(variable);
                await blockFunctionInstance.ExecuteAsync();
                _logger.LogDebug($"Project: {_tenantContext.ProjectId}/FB: {id} Preparing complete successful");
            }
            catch (System.Exception exc)
            {
                processSuccessful = false;
                _logger.LogError(exc, $"Project: {_tenantContext.ProjectId}/FB: {id} {exc.Message}");
            }

            // Check process successful: ERROR -> Stopped
            if (!processSuccessful)
            {
                return (false, Array.Empty<AssetAttributeBinding>());
            }
            if (variable != null && variable.GetBoolean("system_exit_code") == true)
            {
                _logger.LogDebug($"Project: {_tenantContext.ProjectId}/FB: {id} Return as Success by System Exit Code");
                return (true, Array.Empty<AssetAttributeBinding>());
            }
            // post processing
            var tasks = information.Outputs.Select(x => SinkOutputAsync(id, variable, x));
            var outputBindings = await Task.WhenAll(tasks);
            bool hasFailOutputSink = outputBindings.Any(x => x == null);
            return (!hasFailOutputSink, outputBindings.Where(x => x != null));
        }

        private async Task<FunctionBlockTemplateContentResult> GetFunctionTemplateContentAsync(Guid? templateId, Guid? functionBlockId)
        {
            if (!templateId.HasValue && !functionBlockId.HasValue)
            {
                // Mean the Template / Function Block has been deleted by others.
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(templateId));
            }

            if (templateId != null)
            {
                // this execution using template
                try
                {
                    var templateDto = await _mediator.Send(new BlockTemplate.Query.GetBlockTemplateById(templateId.Value), CancellationToken.None);
                    return new FunctionBlockTemplateContentResult
                    {
                        DesignContent = templateDto.DesignContent,
                        Version = templateDto.Version,
                        Blocks = templateDto.FunctionBlocks.Where(x => x.Type == BlockTypeConstants.TYPE_BLOCK),
                        Contents = JsonConvert.DeserializeObject<IEnumerable<FunctionBlockTemplateContent>>(templateDto.Content)
                    };
                }
                catch (EntityNotFoundException)
                {
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(templateId));
                }
            }
            else
            {
                try
                {
                    var functionDto = await _mediator.Send(new Block.Command.GetFunctionBlockById(functionBlockId.Value), CancellationToken.None);
                    var functions = new[] { functionDto };
                    var builder = new StringBuilder();
                    foreach (var binding in functionDto.Bindings.Where(x => _inputTypes.Contains(x.BindingType)))
                    {
                        builder.AppendLine($"AHI.SafetySet(\"{binding.Key}\",\"{binding.DataType}\" ,AHI.SafetyGet(\"{$"{binding.FunctionBlockId.ToString("N")}_{binding.Key}"}\", \"{binding.DataType}\"));");
                    }
                    builder.AppendLine(functionDto.BlockContent);
                    var blockContent = new FunctionBlockTemplateContent()
                    {
                        Content = builder.ToString()
                    };

                    return new FunctionBlockTemplateContentResult
                    {
                        DesignContent = null,
                        Blocks = functions,
                        Contents = new FunctionBlockTemplateContent[] { blockContent }
                    };
                }
                catch (EntityNotFoundException)
                {
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(functionBlockId));
                }
            }
        }

        private async Task<IEnumerable<BlockExecutionInputInformation>> GetExecutionInputInformationAsync(Domain.Entity.FunctionBlockExecution blockExecution, FunctionBlockTemplateContentResult functionBlockTemplateContent)
        {
            // the content should be the same with template, but addon more payload data
            var jsonPayload = JsonConvert.DeserializeObject<FunctionExecutionContent>(blockExecution.DiagramContent);
            var links = jsonPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models).Select(x => x.Value);
            var nodePorts = jsonPayload.Layers.Where(x => !x.IsDiagramLink).SelectMany(x => x.Models).SelectMany(x => x.Value.Ports);
            var portPayloads = (from link in links
                                join sourcePort in nodePorts on link.SourcePort equals sourcePort.Id
                                join targetPort in nodePorts on link.TargetPort equals targetPort.Id
                                // source ports from bindings do not have config
                                where sourcePort.Extras.Config != null
                                select new InputPortPayload
                                {
                                    TargetLabel = targetPort.Label.Replace("\"", ""), // To avoid different between m."A" vs m.A - should be same,
                                    TargetBlockBindingId = targetPort.Extras.BlockBinding.Id,
                                    TargetTemplatePortId = targetPort.Extras.TemplatePortId,
                                    SourcePayload = sourcePort.Extras.Config.Payload
                                });

            var functionBlockTemplateBindings = functionBlockTemplateContent.Blocks.SelectMany(x => x.Bindings).Where(x => _inputTypes.Contains(x.BindingType)).ToArray();
            var inputBindings = new List<BlockExecutionInputInformation>();

            if (functionBlockTemplateContent.DesignContent is null)
            {
                // can be function content
                inputBindings = (from portPayload in portPayloads
                                     // then join to function
                                 join functionBinding in functionBlockTemplateBindings on portPayload.TargetBlockBindingId equals functionBinding.Id
                                 select new BlockExecutionInputInformation
                                 {
                                     FunctionBlockId = functionBinding.FunctionBlockId,
                                     Key = functionBinding.Key,
                                     DataType = functionBinding.DataType,
                                     Payload = portPayload.SourcePayload
                                 }).ToList();
            }
            else
            {
                var templateContentPayload = JsonConvert.DeserializeObject<TemplateContent>(functionBlockTemplateContent.DesignContent);
                var templateLinks = templateContentPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models.Values);
                var templatePorts = templateContentPayload.Layers.Where(x => !x.IsDiagramLink)
                                                                .SelectMany(x => x.Models.Values)
                                                                .SelectMany(x => x.Ports.Select(p => new { Name = x.Name, Port = p }));

                var templatePortFunctionBindings = (from templateSourcePort in templatePorts
                                                    join sourceTemplateLink in templateLinks on templateSourcePort.Port.Id equals sourceTemplateLink.SourcePort
                                                    join templateTargetPort in templatePorts on sourceTemplateLink.TargetPort equals templateTargetPort.Port.Id
                                                    // then join to function
                                                    join functionBinding in functionBlockTemplateBindings on templateTargetPort.Port.BlockBinding.Id equals functionBinding.Id
                                                    select new TemplatePortFunctionBinding
                                                    {
                                                        FunctionBinding = functionBinding,
                                                        PortId = templateSourcePort.Port.Id,
                                                        Name = templateSourcePort.Name.Replace("\"", "") // To avoid different between m."A" vs m.A - should be same
                                                    }
                                                   ).ToList();

                foreach (var portPayload in portPayloads)
                {
                    ProcessInputPortPayload(portPayload, functionBlockTemplateContent, templatePortFunctionBindings, blockExecution, inputBindings);
                }

                if (templatePortFunctionBindings.Any())
                {
                    // Binding from template but missed mapping in BE
                    throw new GenericProcessFailedException
                    (
                        message: $"Missed binding from template with version {functionBlockTemplateContent.Version}",
                        detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR
                    );
                }
            }

            foreach (var inputBinding in inputBindings)
            {
                if (inputBinding.IsAssetAttributeDataType())
                    await ConstructStaticAttributeInputAsync(inputBinding);
            }

            return inputBindings.Distinct(new BlockExecutionInputComparer());
        }

        private void ProcessInputPortPayload(
            InputPortPayload portPayload,
            FunctionBlockTemplateContentResult functionBlockTemplateContent,
            ICollection<TemplatePortFunctionBinding> templatePortFunctionBindings,
            FunctionBlockExecution blockExecution,
            ICollection<BlockExecutionInputInformation> inputBindings)
        {
            if (blockExecution.Version == functionBlockTemplateContent.Version)
            {
                var bindingFromTemplate = templatePortFunctionBindings.First(b => b.PortId == portPayload.TargetTemplatePortId);

                inputBindings.Add(new BlockExecutionInputInformation
                {
                    FunctionBlockId = bindingFromTemplate.FunctionBinding.FunctionBlockId,
                    Key = bindingFromTemplate.FunctionBinding.Key,
                    DataType = bindingFromTemplate.FunctionBinding.DataType,
                    HasAutoMapping = false,
                    Payload = portPayload.SourcePayload
                });
                templatePortFunctionBindings.Remove(bindingFromTemplate);
            }
            else
            {
                var bindingFromTemplate = templatePortFunctionBindings
                                        .OrderBy(b => b.PortId == portPayload.TargetTemplatePortId ? 0 : 1)
                                        .ThenBy(b => string.Equals(b.Name, portPayload.TargetLabel, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
                                        .FirstOrDefault();
                if (bindingFromTemplate != null)
                {
                    var autoBinding = bindingFromTemplate.PortId != portPayload.TargetTemplatePortId || !string.Equals(bindingFromTemplate.Name, portPayload.TargetLabel, StringComparison.InvariantCultureIgnoreCase);
                    var newPayload = TryGetPayloadFromMarkup(bindingFromTemplate.Name, bindingFromTemplate.FunctionBinding.DataType, blockExecution.Mappings, portPayload.SourcePayload);

                    inputBindings.Add(new BlockExecutionInputInformation
                    {
                        FunctionBlockId = bindingFromTemplate.FunctionBinding.FunctionBlockId,
                        Key = bindingFromTemplate.FunctionBinding.Key,
                        DataType = bindingFromTemplate.FunctionBinding.DataType,
                        HasAutoMapping = autoBinding,
                        Payload = newPayload
                    });
                    templatePortFunctionBindings.Remove(bindingFromTemplate);
                }
                else
                {
                    // Cannot throw exception here: if the BT remove some ports/ connectors, the binding from BE will be missed from Template => just ignore those mapping
                    _logger.LogDebug($"Project: {_tenantContext.ProjectId}/FB: {blockExecution.Id} Cannot mapping the Input with label {portPayload.TargetLabel} to the new template with version: {functionBlockTemplateContent.Version}");
                }
            }
        }

        private async Task ConstructStaticAttributeInputAsync(BlockExecutionInputInformation inputBinding)
        {
            var assetAttribute = JObject.FromObject(inputBinding.Payload).ToObject<AssetAttributeBinding>();

            if (!assetAttribute.AttributeId.HasValue)
                return;

            var asset = await _mediator.Send(new Application.Asset.Command.GetAssetById(assetAttribute.AssetId, false), CancellationToken.None);
            var attribute = asset.Attributes.FirstOrDefault(x => assetAttribute.AttributeId.Value == x.Id);
            if (attribute.IsStaticAttribute())
            {
                inputBinding.Payload[PayloadConstants.ATTRIBUTE_TYPE] = attribute.AttributeType;
                inputBinding.Payload[PayloadConstants.ATTRIBUTE_DATA_TYPE] = attribute.DataType;
                // Get value from the template in case the asset inherit from a template
                inputBinding.Payload[PayloadConstants.ATTRIBUTE_STATIC_VALUE] = attribute.Payload?.Value ?? attribute.Value;
            }
        }

        private IBlockVariable PrepareExecutionVariable(IEnumerable<BlockExecutionInputInformation> inputs)
        {
            // extract the binding and then calculate it
            // TODO: this method can be cache to increase the performance of system
            var variable = new BlockVariable(_engine);
            foreach (var functionInput in inputs)
            {
                var functionBlockId = functionInput.FunctionBlockId;
                var key = functionInput.Key;
                var dataType = functionInput.DataType;
                var payload = functionInput.Payload;
                switch (dataType)
                {
                    case BindingDataTypeIdConstants.TYPE_TEXT:
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", payload["value"].ToString());
                        break;
                    case BindingDataTypeIdConstants.TYPE_BOOLEAN:
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", Convert.ToBoolean(payload["value"]));
                        break;
                    case BindingDataTypeIdConstants.TYPE_INTEGER:
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", Convert.ToInt32(payload["value"]));
                        break;
                    case BindingDataTypeIdConstants.TYPE_DOUBLE:
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", Convert.ToDouble(payload["value"]));
                        break;
                    case BindingDataTypeIdConstants.TYPE_DATETIME:
                        var dateTimeString = payload["value"]?.ToString();
                        var dateTime = DateTime.ParseExact(dateTimeString, AHI.Infrastructure.SharedKernel.Extension.Constant.DefaultDateTimeFormat, CultureInfo.InvariantCulture);
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", dateTime);
                        break;
                    case BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                        var assetAttribute = JObject.FromObject(payload).ToObject<AssetAttributeBinding>();
                        var blockContextAssetAttribute = new BlockContext(_engine);
                        blockContextAssetAttribute.Asset(assetAttribute.AssetId).Attribute(assetAttribute.AttributeId.Value);

                        if (assetAttribute.IsStaticAttribute())
                            blockContextAssetAttribute.SetValue(assetAttribute.AttributeDataType, assetAttribute.AttributeStaticValue);

                        variable.Set($"{functionBlockId.ToString("N")}_{key}", blockContextAssetAttribute);
                        break;
                    case BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                        var assetTable = JObject.FromObject(payload).ToObject<AssetTableBinding>();
                        var blockContextAssetTable = new BlockContext(_engine);
                        blockContextAssetTable.Asset(assetTable.AssetId).Table(assetTable.TableId.Value);
                        variable.Set($"{functionBlockId.ToString("N")}_{key}", blockContextAssetTable);
                        break;
                }
            }
            return variable;
        }

        private IEnumerable<BlockExecutionOutputInformation> GetExecutionOutputInformation(Domain.Entity.FunctionBlockExecution blockExecution, FunctionBlockTemplateContent content, FunctionBlockTemplateContentResult functionBlockTemplateContent)
        {
            // the content should be the same with template, but addon more payload data
            var jsonPayload = JsonConvert.DeserializeObject<FunctionExecutionContent>(blockExecution.DiagramContent);
            var executionLinks = jsonPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models).Select(x => x.Value).ToList();
            var nodePorts = jsonPayload.Layers.Where(x => !x.IsDiagramLink).SelectMany(x => x.Models).SelectMany(x => x.Value.Ports);
            var portPayloads = (from link in executionLinks
                                join sourcePort in nodePorts on link.SourcePort equals sourcePort.Id
                                join targetPort in nodePorts on link.TargetPort equals targetPort.Id
                                // source ports from bindings do not have config
                                where targetPort.Extras.Config != null
                                select new OutputPortPayload
                                {
                                    TargetPayload = targetPort.Extras.Config.Payload,
                                    SourceLabel = sourcePort.Label.Replace("\"", ""), // To avoid different between m."A" vs m.A - should be same,
                                    SourceBlockBindingId = sourcePort.Extras.BlockBinding.Id,
                                    SourceTemplatePortId = sourcePort.Extras.TemplatePortId,
                                    SourceIn = sourcePort.In
                                });

            var functionBindings = functionBlockTemplateContent.Blocks.SelectMany(x => x.Bindings).Where(x => _outputTypes.Contains(x.BindingType)).ToArray();
            var bindings = new List<BlockExecutionOutputInformation>();

            if (functionBlockTemplateContent.DesignContent is null)
            {
                // can be function content
                bindings = (from portPayload in portPayloads
                                // then join to function
                            join functionBinding in functionBindings on portPayload.SourceBlockBindingId equals functionBinding.Id
                            select new BlockExecutionOutputInformation
                            {
                                FunctionBlockId = functionBinding.FunctionBlockId,
                                Key = functionBinding.Key,
                                DataType = functionBinding.DataType,
                                HasLinkedPort = true,
                                Payload = portPayload.TargetPayload
                            }).ToList();
            }
            else
            {
                var templateContentPayload = JsonConvert.DeserializeObject<TemplateContent>(functionBlockTemplateContent.DesignContent);
                var templateLinks = templateContentPayload.Layers.Where(x => x.IsDiagramLink).SelectMany(x => x.Models.Values);
                var templateOutputNodes = templateContentPayload.Layers.Where(x => !x.IsDiagramLink).SelectMany(x => x.Models.Values);
                var templateOutputPorts = templateOutputNodes.SelectMany(x => x.Ports.Select(p => new { Name = x.Name, Port = p }));

                var templatePortFunctionBindings = (from templateSourcePort in templateOutputPorts
                                                    join targetTemplateLink in templateLinks on templateSourcePort.Port.Id equals targetTemplateLink.TargetPort
                                                    join templateTargetPort in templateOutputPorts on targetTemplateLink.SourcePort equals templateTargetPort.Port.Id
                                                    // then join to function
                                                    join functionBinding in functionBindings on templateTargetPort.Port.BlockBinding.Id equals functionBinding.Id
                                                    select new TemplatePortFunctionBinding
                                                    {
                                                        FunctionBinding = functionBinding,
                                                        PortId = templateSourcePort.Port.Id,
                                                        Name = templateSourcePort.Name.Replace("\"", "") // To avoid different between m."A" vs m.A - should be same,
                                                    }).ToList();

                foreach (var portPayload in portPayloads.Where(p => !p.SourceIn))
                {
                    ProcessOutputPortPayload(portPayload, functionBlockTemplateContent, templatePortFunctionBindings, blockExecution, content, templateOutputNodes, bindings);
                }

                if (templatePortFunctionBindings.Any())
                {
                    // Binding from template but missed mapping in BE
                    throw new GenericProcessFailedException
                    (
                        message: $"Missed binding from template with version {functionBlockTemplateContent.Version}",
                        detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR
                    );
                }
            }
            return bindings.Where(x => x.HasLinkedPort).Distinct(new BlockExecutionOutputComparer());
        }

        private void ProcessOutputPortPayload(
            OutputPortPayload portPayload,
            FunctionBlockTemplateContentResult functionBlockTemplateContent,
            ICollection<TemplatePortFunctionBinding> templatePortFunctionBindings,
            FunctionBlockExecution blockExecution,
            FunctionBlockTemplateContent content,
            IEnumerable<FunctionModel> templateOutputNodes,
            ICollection<BlockExecutionOutputInformation> outputBindings)
        {
            if (blockExecution.Version == functionBlockTemplateContent.Version)
            {
                var binding = templatePortFunctionBindings.First(b => b.PortId == portPayload.SourceTemplatePortId);
                outputBindings.Add(new BlockExecutionOutputInformation
                {
                    FunctionBlockId = binding.FunctionBinding.FunctionBlockId,
                    Key = binding.FunctionBinding.Key,
                    DataType = binding.FunctionBinding.DataType,
                    HasLinkedPort = templateOutputNodes.Any(x => x.Id == content.NodeId && Array.Exists(x.Ports, x => x.Id == binding.PortId)),
                    HasAutoMapping = false,
                    Payload = portPayload.TargetPayload
                });
                templatePortFunctionBindings.Remove(binding);
            }
            else
            {
                var binding = templatePortFunctionBindings
                                        .OrderBy(b => b.PortId == portPayload.SourceTemplatePortId ? 0 : 1)
                                        .ThenBy(b => string.Equals(b.Name, portPayload.SourceLabel, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
                                        .FirstOrDefault();
                if (binding != null)
                {
                    var autoBinding = binding.PortId != portPayload.SourceTemplatePortId || !string.Equals(binding.Name, portPayload.SourceLabel, StringComparison.InvariantCultureIgnoreCase);
                    var newPayload = TryGetPayloadFromMarkup(binding.Name, binding.FunctionBinding.DataType, blockExecution.Mappings, portPayload.TargetPayload);

                    outputBindings.Add(new BlockExecutionOutputInformation
                    {
                        FunctionBlockId = binding.FunctionBinding.FunctionBlockId,
                        Key = binding.FunctionBinding.Key,
                        DataType = binding.FunctionBinding.DataType,
                        HasLinkedPort = templateOutputNodes.Any(x => x.Id == content.NodeId && Array.Exists(x.Ports, x => x.Id == binding.PortId)),
                        HasAutoMapping = autoBinding,
                        Payload = newPayload
                    });
                    templatePortFunctionBindings.Remove(binding);
                }
                else
                {
                    // Cannot throw exception here: if the BT remove some ports/ connectors, the binding from BE will be missed from Template => just ignore those mapping
                    _logger.LogDebug($"Project: {_tenantContext.ProjectId}/FB: {blockExecution.Id} Cannot mapping the Output with label {portPayload.SourceLabel} to the new template with version: {functionBlockTemplateContent.Version}");
                }
            }
        }

        private IDictionary<string, object> TryGetPayloadFromMarkup(string bindingName, string bindingDataType, IEnumerable<FunctionBlockNodeMapping> lstMapping, IDictionary<string, object> payload)
        {
            switch (bindingDataType)
            {
                case BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                    var attributeMapping = lstMapping.FirstOrDefault(m => string.Equals(bindingName, $"{m.AssetMarkupName}.{m.TargetName}", StringComparison.InvariantCultureIgnoreCase)); // If we override the markup from BE, It can be null
                    if (attributeMapping != null) // Mapping by Markup
                    {
                        if (attributeMapping.Value == null)
                        {
                            throw new GenericProcessFailedException
                            (
                                message: $"Cannot map the Port for binding {bindingName} - data type {bindingDataType}",
                                detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR
                            );
                        }
                        payload[PayloadConstants.ASSET_ID] = attributeMapping.AssetId;
                        payload[PayloadConstants.ATTRIBUTE_NAME] = attributeMapping.TargetName;
                        payload[PayloadConstants.ATTRIBUTE_ID] = attributeMapping.Value;
                    }
                    //else Mapping with Connector Name - keep as it is
                    break;
                case BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                    var tableMapping = lstMapping.FirstOrDefault(m => string.Equals(bindingName, $"{m.AssetMarkupName}.{m.TargetName}", StringComparison.InvariantCultureIgnoreCase));
                    if (tableMapping != null) // Mapping by Markup
                    {
                        if (tableMapping.Value == null)
                        {
                            throw new GenericProcessFailedException
                            (
                                message: $"Cannot map the Port for binding {bindingName} - data type {bindingDataType}",
                                detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR
                            );
                        }
                        payload[PayloadConstants.ASSET_ID] = tableMapping.AssetId;
                        payload[PayloadConstants.TABLE_NAME] = tableMapping.TargetName;
                        payload[PayloadConstants.TABLE_ID] = tableMapping.Value;
                    }
                    //else Mapping with Connector Name - keep as it is
                    break;
                default:
                    // Add new value to payload for primitive type
                    payload[PayloadConstants.VALUE] = bindingName;
                    break;
            }
            return payload;
        }

        private async Task<FunctionBlockOutputBinding> SinkOutputAsync(Guid id, IBlockVariable variable, BlockExecutionOutputInformation binding)
        {
            // Start Sink Output
            FunctionBlockOutputBinding assetAttributeBindings = null;
            try
            {
                IBlockContext outputVariable = null;
                switch (binding.DataType)
                {
                    case BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE:
                        outputVariable = variable.GetAttribute(binding.Key);
                        var assetAttributeDataBinding = JObject.FromObject(binding.Payload).ToObject<AssetAttributeBinding>();
                        assetAttributeBindings = assetAttributeDataBinding;
                        break;
                    case BindingDataTypeIdConstants.TYPE_ASSET_TABLE:
                        var assetTableDataBinding = JObject.FromObject(binding.Payload).ToObject<AssetTableBinding>();
                        outputVariable = variable.GetTable(binding.Key).Asset(assetTableDataBinding.AssetId).Table(assetTableDataBinding.TableId.Value);
                        assetTableDataBinding.Type = BindingDataTypeIdConstants.TYPE_ASSET_TABLE;
                        assetAttributeBindings = assetTableDataBinding;
                        break;
                }

                if (outputVariable != null)
                {
                    await _blockWriterHandler.HandleWriteValueAsync(binding.DataType, binding.Payload, outputVariable);
                }
            }
            catch (EntityValidationException exc)
            {
                _logger.LogError(exc, $"Project: {_tenantContext.ProjectId}/FB: {id} {exc.DetailCode}");
                _logger.LogError($"Failures: {JsonConvert.SerializeObject(exc.Failures)}");
                if (exc.Failures.ContainsKey(nameof(SendConfigurationToDeviceIot.RowVersion)))
                {
                    // Only skip the Writer in this run - but not mean the BE is Error
                    return assetAttributeBindings;
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, $"Project: {_tenantContext.ProjectId}/FB: {id} {exc.Message}");
                return null;
            }
            _logger.LogDebug($"Project: {_tenantContext.ProjectId}/FB: {id} SinkOutputAsync completed.");
            return assetAttributeBindings;
        }

        public async Task<FunctionBlockExecutionDto> GetFunctionBlockExecutionAsync(GetFunctionBlockExecutionById request, CancellationToken cancellationToken)
        {
            // find in the repository
            // IMPORTANCE: shouldn't use the cache, because the executionDateTime update every second.
            // This method is not get all attribute of the execution to reduce the overhead of the data.
            var blockFunctionEntity = await _blockFunctionUnitOfWork.ReadFunctionBlockExecutions.AsQueryable()
                                                                                            .Include(x => x.Template)
                                                                                            .Include(x => x.FunctionBlock)
                                                                                            .Include(x => x.Mappings)
                                                                                            .AsNoTracking().Where(x => x.Id == request.Id)
            .Select(x => new Domain.Entity.FunctionBlockExecution()
            {
                Id = x.Id,
                CreatedUtc = x.CreatedUtc,
                DiagramContent = x.DiagramContent,
                ExecutedUtc = x.ExecutedUtc,
                FunctionBlockId = x.FunctionBlockId,
                TemplateId = x.TemplateId,
                Name = x.Name,
                Status = x.Status,
                TriggerContent = x.TriggerContent,
                TriggerType = x.TriggerType,
                UpdatedUtc = x.UpdatedUtc,
                CreatedBy = x.CreatedBy,
                RunImmediately = x.RunImmediately,
                Mappings = x.Mappings,
                TriggerAssetId = x.TriggerAssetId,
                TriggerAssetMarkup = x.TriggerAssetMarkup,
                TriggerAttributeId = x.TriggerAttributeId,
                Template = x.Template,
                FunctionBlock = x.FunctionBlock,
                Version = x.Version
            }).FirstOrDefaultAsync();
            if (blockFunctionEntity == null)
            {
                throw new EntityNotFoundException();
            }
            return FunctionBlockExecutionDto.Create(blockFunctionEntity);
        }

        protected override Type GetDbType()
        {
            return typeof(IFunctionBlockExecutionRepository);
        }

        public async Task<bool> PublishFunctionBlockExecutionAsync(Guid id)
        {
            var functionBlockExecution = await _blockFunctionUnitOfWork.ReadFunctionBlockExecutions.AsQueryable().Include(e => e.Mappings).FirstOrDefaultAsync(x => x.Id == id);
            if (functionBlockExecution == null)
            {
                throw new EntityNotFoundException();
            }

            if (functionBlockExecution.Status == BlockExecutionStatusConstants.STOPPED_ERROR)
            {
                throw EntityValidationExceptionHelper.GenerateException(nameof(FunctionBlockExecution.Status), nameof(FunctionBlockExecution.Status), null, detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR);
            }

            var result = BaseResponse.Success;
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {
                try
                {
                    // Trying to unpublish the previous register (if any)
                    await UnpublishSnapshotAsync(functionBlockExecution);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                }

                BlockExecutionSnapshot snapshot = null;
                try
                {
                    ValidateFunctionBlockExecutionTrigger(functionBlockExecution);
                    functionBlockExecution.Status = BlockExecutionStatusConstants.RUNNING; // update to publish

                    // build the latest execution information
                    snapshot = await ConstructBlockExecutionSnapshotAsync(functionBlockExecution);


                    if (!await ValidateConnectorsAsync(snapshot.Information) || !await ValidateTriggerContentAsync(snapshot))
                        functionBlockExecution.Status = BlockExecutionStatusConstants.STOPPED_ERROR;

                }
                catch (BaseException ex)
                {
                    // Cannot re-construct the Execution Content => Move the status to STOPPED ERROR and do not register event.
                    functionBlockExecution.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                    LogBlockExecutionError(id, BlockExecutionMessageConstants.PUBLISH_FAIL, ex);
                }

                if (functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING)
                {
                    await _blockTriggerHandler.RegisterAsync(functionBlockExecution);
                    // add new job Id to content snapshot after register success
                    if (snapshot != null)
                    {
                        snapshot.JobId = functionBlockExecution.JobId;
                        functionBlockExecution.ExecutionContent = JsonConvert.SerializeObject(snapshot);
                    }
                }

                if (functionBlockExecution.FunctionBlockId != null)
                    await UpdateVersionByFunctionBlockAsync(functionBlockExecution);

                await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpdateAsync(id, functionBlockExecution);
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Project: {_tenantContext.ProjectId}/FB: {id} Publish Failed with unexpected exception: {ex.Message}");
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }

            var hashKey = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            var hashField = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(id);
            await _cache.DeleteHashByKeyAsync(hashKey, hashField);

            return functionBlockExecution.Status == BlockExecutionStatusConstants.RUNNING;
        }

        private void ValidateFunctionBlockExecutionTrigger(FunctionBlockExecution functionBlockExecution)
        {
            // Validate required data of Block Execution for triggering
            switch (functionBlockExecution.TriggerType)
            {
                case BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT:
                    if (!functionBlockExecution.TriggerAssetId.HasValue)
                    {
                        throw EntityValidationExceptionHelper.GenerateException(nameof(FunctionBlockExecution.TriggerAssetId), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED, null, detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR);
                    }
                    if (!functionBlockExecution.TriggerAttributeId.HasValue)
                    {
                        throw EntityValidationExceptionHelper.GenerateException(nameof(FunctionBlockExecution.TriggerAttributeId), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED, null, detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR);
                    }
                    break;
                case BlockFunctionTriggerConstants.TYPE_SCHEDULER:
                    // Define more validation rule if any
                    break;
                default:
                    throw EntityValidationExceptionHelper.GenerateException(nameof(FunctionBlockExecution.TriggerType), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID, null, detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR);
            }
        }

        private async Task<BlockExecutionSnapshot> ConstructBlockExecutionSnapshotAsync(Domain.Entity.FunctionBlockExecution blockExecution)
        {
            var functionTemplateContentResult = await GetFunctionTemplateContentAsync(blockExecution.TemplateId, blockExecution.FunctionBlockId);

            var executionInformation = new List<BlockExecutionInformation>();
            var inputs = await GetExecutionInputInformationAsync(blockExecution, functionTemplateContentResult); // Not changed - Not using templateContent so move out of the foreach.
            foreach (var templateContent in functionTemplateContentResult.Contents)
            {
                var outputs = GetExecutionOutputInformation(blockExecution, templateContent, functionTemplateContentResult);
                executionInformation.Add(new BlockExecutionInformation()
                {
                    Content = templateContent.Content,
                    Inputs = inputs,
                    Outputs = outputs,
                });
            }
            var snapshot = new BlockExecutionSnapshot()
            {
                TriggerContent = blockExecution.TriggerContent,
                TriggerType = blockExecution.TriggerType,
                Information = executionInformation,
                JobId = blockExecution.JobId
            };
            return snapshot;
        }

        private async Task<bool> ValidateTriggerContentAsync(BlockExecutionSnapshot snapshot)
        {
            var success = true;

            if (snapshot.IsTriggerTypeAssetAttribute())
            {
                var asset = await _mediator.Send(new GetAssetById(snapshot.AssetId, false), CancellationToken.None);
                if (asset == null || !asset.Attributes.Any(x => x.Id == snapshot.AttributeId))
                    success = false;
            }

            return success;
        }

        private async Task<bool> ValidateConnectorsAsync(IEnumerable<BlockExecutionInformation> executionInformation)
        {
            var success = true;
            var connectors = new List<BlockExecutionBindingInformation>();

            connectors.AddRange(executionInformation.SelectMany(x => x.Inputs));
            connectors.AddRange(executionInformation.SelectMany(x => x.Outputs));

            foreach (var connector in connectors)
            {
                if (connector.IsAssetAttributeDataType())
                {
                    var asset = await _mediator.Send(new GetAssetById(connector.AssetId, false), CancellationToken.None);
                    if (asset == null || !asset.Attributes.Any(x => x.Id == connector.AttributeId))
                    {
                        success = false;
                        break;
                    }
                }

                if (connector.IsAssetTableDataType())
                {
                    var tableFetchId = await _assetTableService.FetchAssetTableByIdAsync(connector.AssetId, connector.TableId);
                    if (tableFetchId == null || tableFetchId != connector.TableId)
                    {
                        success = false;
                        break;
                    }
                }
            }

            return success;
        }

        public async Task<bool> UnpublishFunctionBlockExecutionAsync(Guid id)
        {
            var blockExecution = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
            if (blockExecution == null)
            {
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED);
            }
            await _blockFunctionUnitOfWork.BeginTransactionAsync();

            try
            {
                await UnpublishSnapshotAsync(blockExecution);
                if (blockExecution.Status != BlockExecutionStatusConstants.STOPPED_ERROR)
                {
                    blockExecution.Status = BlockExecutionStatusConstants.STOPPED; // update to unpublish
                }

                await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpdateAsync(id, blockExecution);
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"Project: {_tenantContext.ProjectId}/FB: {id} Unpublish Failed: Publish Failed with unexpected exception: {e.Message}");
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }

            var hashKey = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);
            var hashField = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(id);
            await _cache.DeleteHashByKeyAsync(hashKey, hashField);

            return blockExecution.Status == BlockExecutionStatusConstants.STOPPED;
        }

        private async Task UnpublishSnapshotAsync(FunctionBlockExecution blockExecution)
        {
            try
            {
                // Trying to Unpublishing by Execution Snapshot
                var snapshot = await GetBlockExecutionSnapshotAsync(blockExecution.Id);
                if (snapshot != null && snapshot.JobId != null)
                {
                    // unpublish the snapshot
                    var snapshotExecution = new Domain.Entity.FunctionBlockExecution
                    {
                        Name = blockExecution.Name,
                        TriggerType = snapshot.TriggerType,
                        TriggerContent = snapshot.TriggerContent,
                        JobId = snapshot.JobId,
                        Id = blockExecution.Id
                    };
                    await _blockTriggerHandler.UnregisterAsync(snapshotExecution);
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, $"Cannot unpublish the Block Execution \"{blockExecution.Name}\" ({blockExecution.Id}) by Execution snapshot... Current JobId: {blockExecution.JobId}");
            }
        }

        private async Task UpdateBlockExecutionPostRunStatusAsync(Guid id, DateTime executionDate, bool hasError)
        {
            var blockExecution = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);
            if (blockExecution != null)
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // update execution date
                    blockExecution.ExecutedUtc = executionDate;

                    // if fail, set status stop error
                    if (hasError)
                    {
                        blockExecution.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                        LogBlockExecutionError(id, BlockExecutionMessageConstants.EXECUTION_FAIL);
                    }

                    if (blockExecution.Status == BlockExecutionStatusConstants.STOPPED_ERROR)
                    {
                        // remove auto trigger
                        await _blockTriggerHandler.UnregisterAsync(blockExecution);
                    }
                    await _unitOfWork.CommitAsync();
                }
                catch (System.Exception exc)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError(exc, exc.Message);
                }
            }
            else
            {
                _logger.LogError($"Function Block Execution {id} is not found");
            }
        }

        /// <summary>
        /// The function will update Block Execution's status based on criteria
        /// </summary>
        /// <param name="filter">Condition to query block execution</param>
        /// <param name="conditionToChangeStatus">Check before updating block execution status</param>
        /// <param name="targetStatus">The status to be updated</param>
        public async Task UpdateBlockExecutionStatusAsync(Expression<Func<FunctionBlockExecution, bool>> filter, Predicate<FunctionBlockExecution> conditionToChangeStatus, string targetStatus = BlockExecutionStatusConstants.STOPPED)
        {
            var blockExecutions = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable()
                                                                                .Where(filter)
                                                                                .ToListAsync();
            if (blockExecutions.Any())
            {
                foreach (var blockExecution in blockExecutions)
                {
                    if (conditionToChangeStatus(blockExecution))
                        blockExecution.Status = targetStatus;
                }
                await _blockFunctionUnitOfWork.CommitAsync();
            }
        }

        public async Task RefreshBlockExecutionByTemplateIdAsync(Guid templateId, bool hasDiagramChanged)
        {
            var template = await _mediator.Send(new BlockTemplate.Query.GetBlockTemplateById(templateId), CancellationToken.None);

            // do the update for function block execution
            // get all execution with the template
            var executions = await _readFunctionBlockExecutionRepository.AsQueryable().Include(x => x.Mappings)
                                                                                        .Where(x => x.TemplateId == templateId && x.Version != template.Version)
                                                                                        .AsNoTracking()
                                                                                        .ToListAsync();
            try
            {
                await _blockFunctionUnitOfWork.BeginTransactionAsync();

                foreach (var execution in executions)
                {
                    try
                    {
                        // Preparing list Asset Markup Mapping.
                        var crtNodeMarkups = template.Nodes.Where(m => !string.IsNullOrEmpty(m.AssetMarkupName)).Select(n => n.AssetMarkupName.ToLower()).Distinct();
                        var crtMappingMarkupsAsset = execution.Mappings.Where(m => !string.IsNullOrEmpty(m.AssetMarkupName))
                                                                        .Select(m => new
                                                                        {
                                                                            AssetMarkupName = m.AssetMarkupName.ToLower(),
                                                                            AssetId = m.AssetId,
                                                                            AssetName = m.AssetName
                                                                        })
                                                                        .Distinct();
                        var lstMarkupAssetMapping = crtNodeMarkups.FullOuterJoin(crtMappingMarkupsAsset,
                                                                                    nodeMarkup => nodeMarkup,
                                                                                    mappingMarkupAsset => mappingMarkupAsset.AssetMarkupName,
                                                                                    (nodeMarkup, mappingMarkupAsset, idx) => new MarkupAssetMapping
                                                                                    {
                                                                                        MarkupName = string.IsNullOrEmpty(nodeMarkup)
                                                                                                ? mappingMarkupAsset.AssetMarkupName
                                                                                                : nodeMarkup,
                                                                                        AssetId = mappingMarkupAsset?.AssetId,
                                                                                        AssetName = mappingMarkupAsset?.AssetName
                                                                                    }
                                                                                    ).ToList();


                        // find and correct markup
                        var (isValid, isTriggerOverride) = await RefreshTriggerMarkupAsync(template, execution, lstMarkupAssetMapping);
                        isValid &= await RefreshAssetMarkupAsync(template, execution, lstMarkupAssetMapping);
                        RefreshBlockExecutionStatus(execution, isValid, isTriggerOverride, hasDiagramChanged);
                    }
                    catch (System.Exception ex)
                    {
                        execution.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                        LogBlockExecutionError(execution.Id, BlockExecutionMessageConstants.REFRESH_MARKUP_FAIL, ex);
                        // Skip the exception here to avoid impact to other Block Execution as we opening a transaction.
                    }
                    await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpdateAsync(execution.Id, execution);
                    // As we using Upsert for mapping, so create new Id for all mapping to avoid conflict key
                    var lstMapping = execution.Mappings.Select(m => new FunctionBlockNodeMapping
                    {
                        BlockExecutionId = execution.Id,
                        BlockTemplateNodeId = m.BlockTemplateNodeId,
                        AssetMarkupName = m.AssetMarkupName,
                        AssetId = m.AssetId,
                        AssetName = m.AssetName,
                        TargetName = m.TargetName,
                        Value = m.Value
                    });
                    await _blockFunctionUnitOfWork.FunctionBlockExecutions.UpsertBlockNodeMappingAsync(lstMapping, execution.Id);
                }
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Failed to Refresh the template id {templateId}!");
                await _blockFunctionUnitOfWork.RollbackAsync();
            }

            await Task.WhenAll(executions.Select(execution => _cache.DeleteHashByKeyAsync(CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(_tenantContext.ProjectId, execution.Id), CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(execution.Id))));
        }

        private void RefreshBlockExecutionStatus(FunctionBlockExecution execution, bool isValid, bool isTriggerOverride, bool hasDiagramChanged)
        {
            switch (execution.Status)
            {
                case BlockExecutionStatusConstants.RUNNING:
                    // 62921: Should update status to running obsolete if:
                    //          * Template has diagram changed or
                    //          * Execution does not override trigger
                    //        If Template only has trigger changed, but execution override trigger anyway, keep execution status as running
                    // 59907: Won't change the BE's status to Error, the validation will be apply when User republishing the BE.
                    if (hasDiagramChanged || !isTriggerOverride)
                    {
                        execution.Status = BlockExecutionStatusConstants.RUNNING_OBSOLETE;
                    }
                    break;
                case BlockExecutionStatusConstants.STOPPED:
                case BlockExecutionStatusConstants.STOPPED_ERROR:
                    if (isValid)
                        execution.Status = BlockExecutionStatusConstants.STOPPED;
                    else
                    {
                        execution.Status = BlockExecutionStatusConstants.STOPPED_ERROR;
                        LogBlockExecutionError(execution.Id, BlockExecutionMessageConstants.REFRESH_TEMPLATE_FAIL);
                    }

                    break;
                default:
                    break;
            }
        }

        private async Task<bool> ApplyTemplateNodeToNodeMappingAsync(FunctionBlockTemplateNodeDto node, FunctionBlockNodeMapping mapping, Guid? assetId, string assetName)
        {
            mapping.AssetMarkupName = node.AssetMarkupName;
            mapping.TargetName = node.TargetName;
            mapping.AssetId = assetId;
            mapping.AssetName = assetName;

            var dataType = node.Function.Bindings.First().DataType;
            if (_mappingHandler.ContainsKey(dataType))
            {
                if (mapping.AssetId.HasValue)
                {
                    var tmp = await _mappingHandler[dataType].Invoke(mapping.AssetId.Value, mapping.TargetName);
                    mapping.Value = tmp?.Value;
                }
                return !string.IsNullOrEmpty(mapping.Value);
            }
            return true;
        }

        private async Task<bool> RefreshAssetMarkupAsync(GetFunctionBlockTemplateDto template, FunctionBlockExecution execution, IEnumerable<MarkupAssetMapping> lstMarkupAssetMapping)
        {
            var isValid = true;

            var nodeMappings = template.Nodes.FullOuterJoin(execution.Mappings,
                                                            node => node.Id,
                                                            mapping => mapping.BlockTemplateNodeId,
                                                            (node, mapping, idx) => new
                                                            {
                                                                Node = node,
                                                                Mapping = mapping
                                                            }
                                                            ).ToList();

            foreach (var nodeMapping in nodeMappings)
            {
                if (nodeMapping.Node == null)
                {
                    // Template's node = Null ~ Node has been deleted from Template => Remove these mappings.
                    execution.Mappings.Remove(nodeMapping.Mapping);
                }
                else if (nodeMapping.Mapping == null)
                {
                    // Execution's mapping = Null ~ New Node has been created from Template => Add new mapping to Block Execution.
                    var newMapping = new FunctionBlockNodeMapping();
                    newMapping.BlockExecutionId = execution.Id;
                    newMapping.BlockTemplateNodeId = nodeMapping.Node.Id;

                    if (nodeMapping.Node.BlockType != BlockTypeConstants.TYPE_BLOCK)
                    {
                        var assetByMarkup = lstMarkupAssetMapping.FirstOrDefault(m => string.Equals(nodeMapping.Node.AssetMarkupName, m.MarkupName, StringComparison.InvariantCultureIgnoreCase));
                        isValid &= await ApplyTemplateNodeToNodeMappingAsync(nodeMapping.Node, newMapping, assetByMarkup?.AssetId, assetByMarkup?.AssetName);
                    }
                    execution.Mappings.Add(newMapping);
                }
                else if (nodeMapping.Node.BlockType != BlockTypeConstants.TYPE_BLOCK)
                {
                    // Both Node & Mapping existed => Update data from Template. (Currently, we don't need to change the Block Type = Block)
                    var assetByMarkup = lstMarkupAssetMapping.FirstOrDefault(m => string.Equals(nodeMapping.Node.AssetMarkupName, m.MarkupName, StringComparison.InvariantCultureIgnoreCase));
                    var mapping = execution.Mappings.First(m => m.Id == nodeMapping.Mapping.Id);
                    isValid &= await ApplyTemplateNodeToNodeMappingAsync(nodeMapping.Node, mapping, assetByMarkup?.AssetId, assetByMarkup?.AssetName);
                }
            }

            return isValid;
        }

        private async Task<(bool IsTriggerValid, bool IsTriggerOverride)> RefreshTriggerMarkupAsync(GetFunctionBlockTemplateDto template, FunctionBlockExecution execution, IEnumerable<MarkupAssetMapping> lstMarkupAssetMapping)
        {
            var isTriggerValid = true;
            var executionBaseTriggerContent = JsonConvert.DeserializeObject<BlockExecutionTriggerDto>(execution.TriggerContent);
            if (!executionBaseTriggerContent.OverrideTrigger) // This BE has overridden trigger => shouldn't override with template's trigger content.
            {
                var oldTriggerMapping = execution.Mappings.FirstOrDefault(m => string.Equals(m.AssetMarkupName, execution.TriggerAssetMarkup, StringComparison.InvariantCultureIgnoreCase)
                                                                            && m.BlockTemplateNodeId == null);

                switch (template.TriggerType)
                {
                    case BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT:
                        var executionAssetTriggerContent = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(execution.TriggerContent);
                        var templateAssetTriggerContent = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(template.TriggerContent);

                        try
                        {
                            executionAssetTriggerContent.AssetMarkup = templateAssetTriggerContent.AssetMarkup;
                            executionAssetTriggerContent.AttributeName = templateAssetTriggerContent.AttributeName;
                            executionAssetTriggerContent.ProjectId = templateAssetTriggerContent.ProjectId;
                            executionAssetTriggerContent.SubscriptionId = templateAssetTriggerContent.SubscriptionId;

                            // 60057 Changed from Scheduler to Asset Attribute will caused error as we didn't have TriggerAssetId from this step.
                            GetAssetDto asset = null;
                            if (string.Equals(execution.TriggerAssetMarkup, template.TriggerAssetMarkup, StringComparison.InvariantCultureIgnoreCase))
                            {
                                asset = await _mediator.Send(new Asset.Command.GetAssetById(execution.TriggerAssetId.Value, false), CancellationToken.None);
                            }
                            else
                            {
                                var assetMapping = lstMarkupAssetMapping.FirstOrDefault(m => string.Equals(m.MarkupName, template.TriggerAssetMarkup, StringComparison.InvariantCultureIgnoreCase));
                                if (assetMapping?.AssetId != null)
                                {
                                    asset = await _mediator.Send(new Asset.Command.GetAssetById(assetMapping.AssetId.Value, false), CancellationToken.None);
                                }
                            }
                            isTriggerValid = ProcessAssetAttributeTriggerMapping(executionAssetTriggerContent, templateAssetTriggerContent, execution, asset);
                        }
                        catch (EntityValidationException)
                        {
                            isTriggerValid = false;
                            executionAssetTriggerContent.AssetId = null;
                            executionAssetTriggerContent.AttributeId = null;
                        }
                        execution.TriggerAssetMarkup = templateAssetTriggerContent.AssetMarkup;
                        execution.TriggerAssetId = executionAssetTriggerContent.AssetId;
                        execution.TriggerAttributeId = executionAssetTriggerContent.AttributeId;
                        execution.TriggerContent = executionAssetTriggerContent.ToJson();
                        break;
                    case BlockFunctionTriggerConstants.TYPE_SCHEDULER:
                        var templateSchedulerTriggerContent = JsonConvert.DeserializeObject<SchedulerTriggerDto>(template.TriggerContent);
                        var executionSchedulerTriggerContent = JsonConvert.DeserializeObject<SchedulerTriggerDto>(execution.TriggerContent);
                        executionSchedulerTriggerContent.Start = templateSchedulerTriggerContent.Start;
                        executionSchedulerTriggerContent.Expire = templateSchedulerTriggerContent.Expire;
                        executionSchedulerTriggerContent.Cron = templateSchedulerTriggerContent.Cron;
                        executionSchedulerTriggerContent.TimeZoneName = templateSchedulerTriggerContent.TimeZoneName;
                        executionSchedulerTriggerContent.ProjectId = templateSchedulerTriggerContent.ProjectId;
                        executionSchedulerTriggerContent.SubscriptionId = templateSchedulerTriggerContent.SubscriptionId;

                        execution.TriggerAssetMarkup = null;
                        execution.TriggerAssetId = null;
                        execution.TriggerAttributeId = null;
                        execution.TriggerContent = executionSchedulerTriggerContent.ToJson();
                        break;
                }

                execution.TriggerType = template.TriggerType;
            }
            return (isTriggerValid, executionBaseTriggerContent.OverrideTrigger);
        }

        private bool ProcessAssetAttributeTriggerMapping(AssetAttributeTriggerDto executionAssetTriggerContent, AssetAttributeTriggerDto templateAssetTriggerContent, FunctionBlockExecution execution, GetAssetDto asset)
        {
            if (asset == null)
            {
                executionAssetTriggerContent.AssetId = null;
                executionAssetTriggerContent.AttributeId = null;
                return false;
            }

            executionAssetTriggerContent.AssetId = asset.Id;
            var attribute = asset.Attributes.FirstOrDefault(x => string.Equals(x.Name, templateAssetTriggerContent.AttributeName, StringComparison.InvariantCultureIgnoreCase));
            if (attribute == null)
            {
                executionAssetTriggerContent.AttributeId = null;
                return false;
            }

            executionAssetTriggerContent.AttributeId = attribute.Id;
            var oldTriggerMapping = execution.Mappings.FirstOrDefault(m => string.Equals(m.AssetMarkupName, execution.TriggerAssetMarkup, StringComparison.InvariantCultureIgnoreCase)
                                                                    && m.BlockTemplateNodeId == null);
            if (oldTriggerMapping == null)
            {
                execution.Mappings.Add(new FunctionBlockNodeMapping
                {
                    AssetId = asset.Id,
                    AssetName = asset.Name,
                    AssetMarkupName = templateAssetTriggerContent.AssetMarkup,
                    TargetName = attribute.Name,
                    Value = attribute.Id.ToString()
                });
            }
            else
            {
                oldTriggerMapping.AssetId = asset.Id;
                oldTriggerMapping.TargetName = attribute.Name;
                oldTriggerMapping.Value = attribute.Id.ToString();
            }
            return true;
        }

        public async Task<IEnumerable<FunctionBlockExecutionAssetAttributeDto>> GetFunctionBlockExecutionDependencyAsync(Guid[] attributeIds)
        {
            // TODO: consider to extract the function block execution without template to mapping table so that we can query more easily
            // currently, the function block execution with standalone is not extractable, need find the way to extract the information
            var attributeIdStrings = attributeIds.Select(x => $"{x}");
            var functionBlockExecutions = await _readFunctionBlockExecutionRepository.AsQueryable().AsNoTracking()
                                                    .Where(x => (x.TriggerAttributeId.HasValue && attributeIds.Contains(x.TriggerAttributeId.Value))
                                                            || x.Mappings.Any(mapping => attributeIdStrings.Contains(mapping.Value)))
                                                    .ToListAsync();
            return functionBlockExecutions.Select(FunctionBlockExecutionAssetAttributeDto.Create);
        }

        public async Task<ValidationBlockExecutionDto> ValidateBlockExecutionAsync(ValidationBlockExecution request, CancellationToken cancellationToken)
        {
            var response = new ValidationBlockExecutionDto();
            var requestAssets = request.Connectors.Select(x => x.AssetId).Distinct();

            var existsAttributeConnector = request.Connectors.Any(x => string.Equals(x.Type, BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE, StringComparison.InvariantCultureIgnoreCase));
            var existsTableConnector = request.Connectors.Any(x => string.Equals(x.Type, BindingDataTypeIdConstants.TYPE_ASSET_TABLE, StringComparison.InvariantCultureIgnoreCase));

            var tasks = new List<Task<IEnumerable<TargetConnector>>>();
            if (existsAttributeConnector)
            {
                tasks.Add(GetAssetAttributeAsync(requestAssets));
            }
            if (existsTableConnector)
            {
                tasks.Add(_assetTableService.SearchAssetTableAsync(requestAssets));
            }

            var taskCompleted = await Task.WhenAll(tasks);

            var dataMapping = taskCompleted.SelectMany(x => x.ToList());
            var connector = new List<ValidationConnectorDto>();
            foreach (var item in request.Connectors)
            {
                var result = ValidationConnectorDto.Create(item);
                var targetEntity = dataMapping.Where(x => x.AssetId == item.AssetId && x.Type == item.Type);
                var entity = targetEntity.FirstOrDefault(x => string.Equals(x.Name, item.TargetName, StringComparison.InvariantCultureIgnoreCase));
                if (entity != null)
                {
                    result.TargetId = entity.Id;
                    result.Payload = entity.Payload;
                }
                connector.Add(result);
            }
            response.Connectors = connector;

            // validate Block Execution version with Function Block version
            var blockExecution = await _blockFunctionUnitOfWork.FunctionBlockExecutions.AsQueryable().Include(x => x.FunctionBlock).AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id);
            // if Block Execution links with a Block Template, IsSyncWithBlockFunction is true by default
            response.IsSyncWithBlockFunction = true;
            if (blockExecution.FunctionBlock != null)
            {
                response.IsSyncWithBlockFunction = blockExecution.Version == blockExecution.FunctionBlock.Version;
            }
            return response;
        }

        private async Task<IEnumerable<TargetConnector>> GetAssetAttributeAsync(IEnumerable<Guid> assetIds)
        {
            var mappingConnectors = new List<TargetConnector>();
            foreach (var id in assetIds)
            {
                try
                {
                    // need to skip validate the user scope.
                    var asset = await _mediator.Send(new Asset.Command.GetAssetById(id, false), CancellationToken.None);
                    var connectors = asset.Attributes.Select(x => new TargetConnector()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        AssetId = x.AssetId,
                        Type = BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE,
                        Payload = new ValidationBlockExecutionPayloadDto()
                        {
                            AttributeType = x.AttributeType,
                            EnabledExpression = x.Payload == null ? false : x.Payload.EnabledExpression
                        }
                    }).ToList();
                    mappingConnectors.AddRange(connectors);
                }
                catch (EntityValidationException) { }
            }
            return mappingConnectors;
        }

        public async Task<IEnumerable<ArchiveFunctionBlockExecutionDto>> ArchiveAsync(ArchiveFunctionBlockExecution command, CancellationToken cancellationToken = default)
        {
            var blockExecutions = await _readFunctionBlockExecutionRepository.AsQueryable()
                                                                    .Include(x => x.Mappings)
                                                                    .Where(x => x.UpdatedUtc <= command.ArchiveTime)
                                                                    .ToListAsync();
            return blockExecutions.Select(x => ArchiveFunctionBlockExecutionDto.CreateDto(x));
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyFunctionBlockExecution command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveFunctionBlockExecutionDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);

            foreach (var item in data)
            {
                var validation = await _validator.ValidateAsync(item);
                if (!validation.IsValid)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            }

            return BaseResponse.Success;
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveFunctionBlockExecution command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveFunctionBlockExecutionDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.Any())
            {
                return BaseResponse.Success;
            }
            var entities = data.OrderBy(x => x.UpdatedUtc).Select(x => ArchiveFunctionBlockExecutionDto.CreateEntity(x, command.Upn)).ToList();
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var entity in entities)
                {
                    DecorateFunctionBlockExecution(entity);
                    if (entity.Status == BlockExecutionStatusConstants.RUNNING
                        || entity.Status == BlockExecutionStatusConstants.RUNNING_OBSOLETE)
                    {
                        await _blockTriggerHandler.RegisterAsync(entity);

                        // build the latest execution information
                        var snapshot = await ConstructBlockExecutionSnapshotAsync(entity);
                        entity.ExecutionContent = JsonConvert.SerializeObject(snapshot);
                    }
                }
                await _blockFunctionUnitOfWork.FunctionBlockExecutions.RetrieveAsync(entities);
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        private void DecorateFunctionBlockExecution(Domain.Entity.FunctionBlockExecution entity)
        {
            switch (entity.TriggerType)
            {
                case BlockFunctionTriggerConstants.TYPE_ASSET_ATTRIBUTE_EVENT:
                    var eventRequest = JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(entity.TriggerContent, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
                    eventRequest.SubscriptionId = _tenantContext.SubscriptionId;
                    eventRequest.ProjectId = _tenantContext.ProjectId;
                    entity.TriggerContent = JsonConvert.SerializeObject(eventRequest, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
                    break;
            }
        }

        private async Task UpdateVersionByFunctionBlockAsync(FunctionBlockExecution blockExecution)
        {
            if (blockExecution.FunctionBlockId != null)
            {
                var blockFunction = await _blockFunctionUnitOfWork.FunctionBlocks.AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == blockExecution.FunctionBlockId.Value);
                if (blockFunction == null)
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(FunctionBlockExecution.FunctionBlockId));
                blockExecution.Version = blockFunction.Version;
            }
        }

        private class InputPortPayload
        {
            public string TargetLabel { get; set; }
            public Guid TargetBlockBindingId { get; set; }
            public Guid? TargetTemplatePortId { get; set; }
            public IDictionary<string, object> SourcePayload { get; set; }
        }

        private class OutputPortPayload
        {
            public string SourceLabel { get; set; }
            public Guid SourceBlockBindingId { get; set; }
            public Guid? SourceTemplatePortId { get; set; }
            public IDictionary<string, object> TargetPayload { get; set; }
            public bool SourceIn { get; set; }
        }
        // SourceLabel = sourcePort.Label.Replace("\"", ""), // To avoid different between m."A" vs m.A - should be same,
        // SourceBlockBindingId = sourcePort.Extras.BlockBinding.Id,
        // SourceTemplatePortId = sourcePort.Extras.TemplatePortId,
        // SourceIn = sourcePort.In

        private class TemplatePortFunctionBinding
        {
            public BlockBinding.Command.Model.GetFunctionBlockBindingDto FunctionBinding { get; set; }
            public Guid PortId { get; set; }
            public string Name { get; set; }
        }
    }
}