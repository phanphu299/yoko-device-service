using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.Block.Command;
using Device.Application.Block.Command.Model;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Constant;
using Device.Application.Events;
using Device.Application.Repositories;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class FunctionBlockService : BaseSearchService<Domain.Entity.FunctionBlock, Guid, GetFunctionBlockByCriteria, GetFunctionBlockSimpleDto>, IFunctionBlockService
    {
        private readonly IBlockFunctionUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly DeviceBackgroundService _deviceBackgroundService;
        private readonly ITenantContext _tenantContext;
        private readonly ICache _cache;
        private readonly IFunctionBlockExecutionResolver _blockFunctionResolver;
        private readonly IFunctionBlockTemplateService _templateService;
        private readonly IUserContext _userContext;
        private readonly IValidator<ArchiveFunctionBlockDto> _validator;
        private readonly IReadFunctionBlockRepository _readFunctionBlockRepository;
        private readonly IReadBlockCategoryRepository _readBlockCategoryRepository;
        private readonly IReadFunctionBlockTemplateRepository _readFunctionBlockTemplateRepository;
        private readonly IReadFunctionBlockExecutionRepository _readFunctionBlockExecutionRepository;
        private readonly string[] _outBinding = { BindingTypeConstants.OUTPUT, BindingTypeConstants.INOUT };

        public FunctionBlockService(IServiceProvider serviceProvider
            , IBlockFunctionUnitOfWork unitOfWork
            , IAuditLogService auditLogService
            , DeviceBackgroundService deviceBackgroundService
            , ITenantContext tenantContext
            , ICache cache
            , IFunctionBlockExecutionResolver blockFunctionResolver
            , IFunctionBlockTemplateService templateService
            , IUserContext userContext
            , IValidator<ArchiveFunctionBlockDto> validator
            , IReadFunctionBlockRepository readFunctionBlockRepository
            , IReadBlockCategoryRepository readBlockCategoryRepository
            , IReadFunctionBlockTemplateRepository readFunctionBlockTemplateRepository
            , IReadFunctionBlockExecutionRepository readFunctionBlockExecutionRepository) : base(GetFunctionBlockSimpleDto.Create, serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
            _deviceBackgroundService = deviceBackgroundService;
            _tenantContext = tenantContext;
            _cache = cache;
            _blockFunctionResolver = blockFunctionResolver;
            _templateService = templateService;
            _userContext = userContext;
            _validator = validator;
            _readFunctionBlockRepository = readFunctionBlockRepository;
            _readBlockCategoryRepository = readBlockCategoryRepository;
            _readFunctionBlockTemplateRepository = readFunctionBlockTemplateRepository;
            _readFunctionBlockExecutionRepository = readFunctionBlockExecutionRepository;
        }

        public async Task<GetFunctionBlockDto> FindEntityByIdAsync(GetFunctionBlockById command, CancellationToken token)
        {
            var key = CacheKey.FUNCTION_BLOCK.GetCacheKey(_tenantContext.ProjectId, command.Id);
            var dto = await _cache.GetAsync<GetFunctionBlockDto>(key);

            if (dto == null)
            {
                var entity = await _readFunctionBlockRepository.FindAsync(command.Id);
                if (entity == null)
                    throw new EntityNotFoundException();
                dto = GetFunctionBlockDto.Create(entity);

                if (dto != null)
                {
                    await _cache.StoreAsync(key, dto);
                }
            }
            return dto;
        }

        public Task<bool> CheckUsedFunctionBlockAsync(CheckUsedFunctionBlock command, CancellationToken cancellationToken)
        {
            return _readFunctionBlockExecutionRepository.AsQueryable().AnyAsync(x => x.FunctionBlockId != null && x.FunctionBlockId.Value == command.Id && (x.Status == BlockExecutionStatusConstants.RUNNING || x.Status == BlockExecutionStatusConstants.RUNNING_OBSOLETE));
        }

        public async Task<AddFunctionBlockDto> AddEntityAsync(AddFunctionBlock payload, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var block = AddFunctionBlock.Create(payload);
                block.CreatedBy = _userContext.Upn;

                await ValidateFunctionBlockNameAsync(Guid.Empty, payload.Name, payload.CategoryId);
                await ValidateFunctionBlockCategoryAsync(payload.CategoryId);
                await ValidateBlockCodeAsync(block);

                var entityResult = await _unitOfWork.FunctionBlocks.AddEntityWithRelationAsync(block);
                await _unitOfWork.CommitAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Add, ActionStatus.Success, entityResult.Id, entityResult.Name, payload);

                return AddFunctionBlockDto.Create(entityResult);
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Add, ex, entityName: payload.Name, payload: payload);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private Task ValidateBlockCodeAsync(Domain.Entity.FunctionBlock block)
        {
            var outputBindings = block.Bindings.Where(x => _outBinding.Contains(x.BindingType) && BlockExecutionConstants.RESERVE_KEYWORDS.Contains(x.Key)).Select(x => x.Key);

            if (outputBindings.Any())
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(block.Bindings));
            }

            try
            {
                _blockFunctionResolver.ResolveInstance(block.BlockContent);
            }
            catch (GenericProcessFailedException ex)
            {
                throw EntityValidationExceptionHelper.GenerateException(nameof(block.BlockContent), ex.Message, detailCode: ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            }
            return Task.CompletedTask;
        }

        public async Task<UpdateFunctionBlockDto> UpdateEntityAsync(UpdateFunctionBlock payload, CancellationToken cancellationToken)
        {
            var targetBlockEntity = await _readFunctionBlockRepository.AsQueryable().Include(x => x.Bindings).AsNoTracking().FirstOrDefaultAsync(x => x.Id == payload.Id);
            if (targetBlockEntity == null)
                throw new EntityNotFoundException();

            var requestedBlockEntity = UpdateFunctionBlock.Create(payload);
            requestedBlockEntity.Version = targetBlockEntity.Version;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ValidateFunctionBlockNameAsync(payload.Id, payload.Name, payload.CategoryId);
                await ValidateFunctionBlockCategoryAsync(payload.CategoryId);
                await ValidateBlockCodeAsync(requestedBlockEntity);

                // get template design content
                var conTentTemplateUsingBlock = await GetLinksByBlockIdAsync(payload.Id);
                var bindingIds = GetBindingIdsUsed(conTentTemplateUsingBlock);
                if (CheckBindingChanged(payload, targetBlockEntity) || CheckContentChanged(payload, targetBlockEntity))
                {
                    requestedBlockEntity.Version = Guid.NewGuid();
                }

                await _unitOfWork.FunctionBlocks.UpdateEntityWithRelationAsync(payload.Id, requestedBlockEntity, bindingIds);
                await _unitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Update, ActionStatus.Success, payload.Id, payload.Name, payload: payload);
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanFunctionBlockCache(payload.Id));
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Update, ex, payload.Id, payload.Name, payload: payload);
                await _unitOfWork.RollbackAsync();
                throw;
            }

            // Perform updating related Block Templates & Block Executions in the background when the block code was changed
            var requestedMd5 = requestedBlockEntity.BlockContent.CalculateMd5Hash();
            var targetMd5 = targetBlockEntity.BlockContent.CalculateMd5Hash();
            if (!string.Equals(requestedMd5, targetMd5))
                await _deviceBackgroundService.QueueAsync(_tenantContext, payload);

            return UpdateFunctionBlockDto.Create(targetBlockEntity);
        }

        private bool CheckBindingChanged(UpdateFunctionBlock command, Domain.Entity.FunctionBlock entity)
        {
            bool hasNew = command.Bindings.Any(x => !entity.Bindings.Any(y => y.Id == x.Id));
            bool hasDelete = entity.Bindings.Any(x => !command.Bindings.Any(y => y.Id == x.Id));
            bool hasBindingChanged = entity.Bindings.Any(x =>
            {
                var newBinding = command.Bindings.FirstOrDefault(y => y.Id == x.Id);
                if (newBinding == null)
                    return false;
                return newBinding.Key != x.Key
                    || newBinding.BindingType != x.BindingType
                    || newBinding.DataType != x.DataType
                    || newBinding.DefaultValue != x.DefaultValue;
            });
            return hasNew || hasDelete || hasBindingChanged;
        }

        private bool CheckContentChanged(UpdateFunctionBlock command, Domain.Entity.FunctionBlock entity)
        {
            return !string.Equals(command.BlockContent.CalculateMd5Hash(), entity.BlockContent.CalculateMd5Hash());
        }

        public async Task<bool> ValidationFunctionBlockAsync(ValidationFunctionBlocks command, CancellationToken token)
        {
            var isUsed = false;
            var templateIds = new List<Guid>();

            // Execution used Execution
            var executionUsedBlocks = await _readFunctionBlockExecutionRepository.AsQueryable().AnyAsync(x => x.FunctionBlockId != null && command.Ids.Contains((Guid)x.FunctionBlockId));
            if (executionUsedBlocks)
                return true;

            foreach (var id in command.Ids)
            {
                var functionblock = await _readFunctionBlockRepository.AsQueryable().Include(x => x.BlockTemplateMappings).FirstOrDefaultAsync(x => x.Id == id);
                if (functionblock == null || functionblock.Deleted == true)
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

                templateIds.AddRange(functionblock.BlockTemplateMappings.Select(x => x.BlockTemplateId));
            }
            // Execution used Template
            var usedTemplates = templateIds.Distinct();
            var executionsUsed = await _readFunctionBlockExecutionRepository.AsQueryable().AnyAsync(x => x.TemplateId != null && usedTemplates.Contains((Guid)x.TemplateId));

            if (executionsUsed)
                return true;

            return isUsed;
        }

        public async Task<GetFunctionBlockDto> GetFunctionBlockCloneAsync(GetFunctionBlockClone command, CancellationToken cancellationToken)
        {
            Domain.Entity.FunctionBlock cloneFunctionBlock = null;
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var block = await _readFunctionBlockRepository.AsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == command.Id);
                if (block == null)
                    throw new EntityNotFoundException();

                cloneFunctionBlock = await CloneFunctionBlockByIdAsync(block);
                await _unitOfWork.FunctionBlocks.AddAsync(cloneFunctionBlock);
                await _unitOfWork.CommitAsync();
            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActivitiesLogEventAction.Clone, ex, payload: command);
                throw;
            }
            await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActivitiesLogEventAction.Clone, ActionStatus.Success, cloneFunctionBlock.Id, cloneFunctionBlock.Name, payload: command);

            return GetFunctionBlockDto.Create(cloneFunctionBlock);
        }

        private async Task<Domain.Entity.FunctionBlock> CloneFunctionBlockByIdAsync(Domain.Entity.FunctionBlock block, bool appendCopyToName = true)
        {
            var name = block.Name;
            if (appendCopyToName)
            {
                name = await BuildCopyNameAsync(block);
            }
            var root = new Domain.Entity.FunctionBlock
            {
                Name = name,
                Type = block.Type,
                BlockContent = block.BlockContent,
                Deleted = block.Deleted,
                CategoryId = block.CategoryId,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };
            var blockBindings = block.Bindings.Where(x => !x.Deleted).ToList();
            foreach (var binding in blockBindings)
            {
                var newBinding = binding;
                newBinding.Id = Guid.NewGuid();
                newBinding.FunctionBlockId = root.Id;
                root.Bindings.Add(newBinding);
            }
            return root;
        }

        private async Task<string> BuildCopyNameAsync(Domain.Entity.FunctionBlock block)
        {
            var targetName = string.Concat(block.Name, " copy");
            var categoryId = block.CategoryId;
            var names = await _readFunctionBlockRepository.AsQueryable().AsNoTracking().Where(x => x.CategoryId == categoryId && x.Name.StartsWith(targetName)).Select(x => x.Name).ToListAsync();
            if (names.Any())
            {
                targetName = string.Concat(names.OrderBy(x => x.Length).Last(), " copy");
            }

            if (targetName.Length > NameConstants.NAME_MAX_LENGTH)
                throw new EntityInvalidException(detailCode: MessageConstants.BLOCK_FUNCTION_CLONE_NAME_MAX_LENGTH);
            return targetName;
        }

        private IEnumerable<Guid> GetBindingIdsUsed(IEnumerable<FunctionBlockLinkDto> contentTemplateUsingBlock)
        {
            var ids = new List<Guid>();
            foreach (var item in contentTemplateUsingBlock)
            {
                ids.Add(item.Output.BindingId);
                ids.AddRange(item.Inputs.Select(x => x.BindingId));
            }
            return ids.Distinct();
        }

        private async Task<IEnumerable<FunctionBlockLinkDto>> GetLinksByBlockIdAsync(Guid id)
        {
            var blockLinks = new List<FunctionBlockLinkDto>();
            var templateIds = new List<Guid>();
            var block = await _readFunctionBlockRepository.AsQueryable().Include(x => x.BlockTemplateMappings).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (block == null)
                return blockLinks;
            templateIds.AddRange(block.BlockTemplateMappings.Select(x => x.BlockTemplateId));
            foreach (var templateId in templateIds)
            {
                var template = await _readFunctionBlockTemplateRepository.FindAsync(templateId);
                if (template == null)
                    continue;
                var contentDetailDto = _templateService.GetFunctionBlockTemplateContent(template.DesignContent);
                blockLinks.AddRange(contentDetailDto.Links);
            }
            return blockLinks;
        }

        public async Task<BaseResponse> DeleteEntityAsync(DeleteFunctionBlock payload, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            var deleteNames = new List<string>();
            var templateIds = new List<Guid>();
            var excutionsUsing = new List<FunctionBlockExecution>();
            try
            {
                foreach (var id in payload.Ids)
                {
                    var deletedEntity = await _unitOfWork.FunctionBlocks.AsQueryable().Include(x => x.BlockTemplateMappings).FirstOrDefaultAsync(x => x.Id == id);
                    if (deletedEntity == null)
                        throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

                    deleteNames.Add(deletedEntity.Name);
                    // validation: block is being used in template content
                    if (deletedEntity.BlockTemplateMappings.Any())
                    {
                        templateIds.AddRange(deletedEntity.BlockTemplateMappings.Select(x => x.BlockTemplateId));
                    }

                    deletedEntity.Deleted = true;
                    deletedEntity.Bindings.Clear();
                }
                var executionUsedBlock = await _unitOfWork.FunctionBlockExecutions.AsQueryable().Where(x => x.FunctionBlockId != null && payload.Ids.Contains((Guid)x.FunctionBlockId)).ToListAsync();
                excutionsUsing.AddRange(executionUsedBlock);

                if (templateIds.Any())
                {
                    var executions = await _unitOfWork.FunctionBlockExecutions.AsQueryable().Where(x => x.TemplateId != null && templateIds.Contains((Guid)x.TemplateId)).ToListAsync();
                    excutionsUsing.AddRange(executions);
                }
                // update relationship
                await UpdateBlockExecutionRelationAsync(excutionsUsing);

                await _unitOfWork.CommitAsync();
                await _deviceBackgroundService.QueueAsync(_tenantContext, new CleanFunctionBlockCache(payload.Ids.ToArray()));
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Delete, ActionStatus.Success, payload.Ids, deleteNames, payload: payload);
                return BaseResponse.Success;
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK, ActionType.Delete, ex, payload.Ids, deleteNames, payload: payload);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        private async Task UpdateBlockExecutionRelationAsync(IEnumerable<FunctionBlockExecution> executions)
        {
            foreach (var exe in executions)
            {
                //Bug: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/56222
                //If the block execution is running, the status is changed to Running (Obsolete)
                //If the block execution is Stoped/Stopped (Error), the status is not changed.
                if (exe.Status == BlockExecutionStatusConstants.RUNNING)
                {
                    exe.Status = BlockExecutionStatusConstants.RUNNING_OBSOLETE;
                }
                await _unitOfWork.FunctionBlockExecutions.UpdateAsync(exe.Id, exe);
            }
        }

        protected override Type GetDbType()
        {
            return typeof(IFunctionBlockRepository);
        }

        public async Task<UpsertFunctionBlockDto> UpsertFunctionBlockAsync(UpsertFunctionBlock request, CancellationToken cancellationToken)
        {
            JsonPatchDocument document = request.Data;
            string path;
            List<Operation> operations = document.Operations;
            UpsertFunctionBlockDto result = new UpsertFunctionBlockDto();
            var resultModels = new List<SharedKernel.BaseJsonPathDocument>();
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                foreach (Operation operation in operations)
                {
                    var resultModel = new SharedKernel.BaseJsonPathDocument
                    {
                        OP = operation.op,
                        Path = operation.path
                    };
                    switch (operation.op)
                    {
                        case "edit_category":
                            path = operation.path.Replace("/", "");
                            if (Guid.TryParse(path, out var editBlockId))
                            {
                                var updateBlock = JObject.FromObject(operation.value).ToObject<UpdateFunctionBlock>();
                                updateBlock.Id = editBlockId;
                                var editCategory = await ChangeBlockCategoryAsync(updateBlock, cancellationToken);
                                resultModel.Values = editCategory;
                            }
                            break;
                    }

                    resultModels.Add(resultModel);
                }
                result.Data = resultModels;
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            return result;
        }

        private async Task<GetFunctionBlockSimpleDto> ChangeBlockCategoryAsync(UpdateFunctionBlock command, CancellationToken cancellationToken)
        {
            var blockTracking = await _unitOfWork.FunctionBlocks.AsQueryable().FirstOrDefaultAsync(x => x.Id == command.Id);
            await ValidateFunctionBlockNameAsync(command.Id, blockTracking.Name, command.CategoryId);
            blockTracking.CategoryId = command.CategoryId;
            await _unitOfWork.FunctionBlocks.UpdateAsync(command.Id, blockTracking);
            var result = new GetFunctionBlockSimpleDto() { Id = blockTracking.Id, Name = blockTracking.Name, CategoryId = blockTracking.CategoryId };
            return result;
        }

        private async Task ValidateFunctionBlockNameAsync(Guid functionBlockId, string functionBlockName, Guid categoryId)
        {
            if (await _readFunctionBlockRepository.AsQueryable().AnyAsync(x => x.Id != functionBlockId && x.Name.ToLower() == functionBlockName.ToLower() && x.CategoryId == categoryId))
                throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(AddFunctionBlock.Name));
        }

        private async Task ValidateFunctionBlockCategoryAsync(Guid categoryId)
        {
            if (!await _readBlockCategoryRepository.AsQueryable().AnyAsync(x => x.Id == categoryId))
                throw EntityValidationExceptionHelper.GenerateException(nameof(AddFunctionBlock.CategoryId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
        }

        public async Task<bool> ValidationIfUsedFunctionBlockIsChangingAsync(ValidationFunctionBlockContent request, CancellationToken cancellationToken)
        {
            var trackedFunctionBlock = await _unitOfWork.FunctionBlocks.FindAsync(request.Id);
            if (trackedFunctionBlock == null)
                throw new EntityNotFoundException();

            //Used in Block Template & Block Template has Execution
            var functionBlockUsed = await _readFunctionBlockTemplateRepository
                                                     .AsQueryable()
                                                     .AsNoTracking()
                                                     .AnyAsync(bt => bt.Nodes.Any(n => n.FunctionBlockId == request.Id) && bt.Executions.Any());
            //Used in Block Execution directly
            functionBlockUsed |= await _readFunctionBlockExecutionRepository
                                                    .AsQueryable()
                                                    .AsNoTracking()
                                                    .AnyAsync(x => x.FunctionBlockId == request.Id);
            if (!functionBlockUsed)
            {
                return false;
            }
            return ValidateUsedFunctionBlockChanged(request, trackedFunctionBlock);
        }

        private bool ValidateUsedFunctionBlockChanged(ValidationFunctionBlockContent request, Domain.Entity.FunctionBlock trackedFunctionBlock)
        {
            var isChanged = true;
            if (!string.Equals(trackedFunctionBlock.BlockContent, request.BlockContent, StringComparison.InvariantCulture))
            {
                return isChanged;
            }

            if (request.Bindings.Count() != trackedFunctionBlock.Bindings.Count())
            {
                return isChanged;
            }
            foreach (var trackedBinding in trackedFunctionBlock.Bindings)
            {
                var binding = request.Bindings.SingleOrDefault(b => b.Id == trackedBinding.Id);
                if (binding == null)
                    return isChanged;
                if (!string.Equals(binding.Key, trackedBinding.Key)
                || !string.Equals(binding.DataType, trackedBinding.DataType)
                || !string.Equals(binding.DefaultValue, trackedBinding.DefaultValue)
                || !string.Equals(binding.BindingType, trackedBinding.BindingType)
                || !string.Equals(binding.Description, trackedBinding.Description)
                )
                {
                    return isChanged;
                }
            }
            return !isChanged;
        }

        public async Task<IEnumerable<ArchiveFunctionBlockDto>> ArchiveAsync(ArchiveFunctionBlock command, CancellationToken cancellationToken)
        {
            // Need to archive function block include soft deleted for retrieve function block execution
            var functionBlocks = await _readFunctionBlockRepository.AsQueryable().IgnoreQueryFilters().AsNoTracking()
                                            .Include(x => x.Bindings)
                                            .Where(x => !x.System && x.UpdatedUtc <= command.ArchiveTime)
                                            .ToListAsync();
            return functionBlocks.Select(x => ArchiveFunctionBlockDto.CreateDto(x));
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyFunctionBlock command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveFunctionBlockDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);

            foreach (var item in data)
            {
                var validation = await _validator.ValidateAsync(item);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }

            return BaseResponse.Success;
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveFunctionBlock command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveFunctionBlockDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.Any())
            {
                return BaseResponse.Success;
            }
            var entities = data.OrderBy(x => x.CreatedUtc).Select(x => ArchiveFunctionBlockDto.CreateEntity(x, command.Upn));
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.FunctionBlocks.RetrieveAsync(entities);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }
    }
}