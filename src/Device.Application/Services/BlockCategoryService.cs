using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.UserContext.Abstraction;
using Device.Application.BlockCategory.Command;
using Device.Application.BlockCategory.Model;
using Device.Application.BlockFunctionCategory.Command;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
namespace Device.Application.Service
{
    public class BlockCategoryService : BaseSearchService<Domain.Entity.FunctionBlockCategory, Guid, GetBlockCategoryByCriteria, GetBlockCategoryDto>, IBlockCategoryService
    {
        private readonly IBlockFunctionUnitOfWork _blockFunctionUnitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly ILoggerAdapter<BlockCategoryService> _logger;
        private readonly IUserContext _userContext;
        private readonly IValidator<ArchiveBlockCategoryDto> _validator;
        private readonly IReadFunctionBlockRepository _readFunctionBlockRepository;
        private readonly IReadBlockCategoryRepository _readBlockCategoryRepository;
        public BlockCategoryService(IBlockFunctionUnitOfWork blockFunctionUnitOfWork
                        , IServiceProvider serviceProvider
                        , IAuditLogService auditLogService
                        , ILoggerAdapter<BlockCategoryService> logger
                        , IUserContext userContext
                        , IValidator<ArchiveBlockCategoryDto> validator
                        , IReadFunctionBlockRepository readFunctionBlockRepository
                        , IReadBlockCategoryRepository readBlockCategoryRepository)
            : base(GetBlockCategoryDto.Create, serviceProvider)
        {
            _blockFunctionUnitOfWork = blockFunctionUnitOfWork;
            _auditLogService = auditLogService;
            _logger = logger;
            _userContext = userContext;
            _validator = validator;
            _readFunctionBlockRepository = readFunctionBlockRepository;
            _readBlockCategoryRepository = readBlockCategoryRepository;
        }

        public async Task<GetBlockCategoryDto> GetBlockCategoryByIdAsync(GetBlockCategoryById command, CancellationToken cancellationToken)
        {
            var entity = await _readBlockCategoryRepository.FindAsync(command.Id);
            if (entity == null)
            {
                throw new EntityNotFoundException();
            }
            return GetBlockCategoryDto.Create(entity);
        }

        public async Task<BlockCategoryDto> AddBlockCategoryAsync(AddBlockCategory command, CancellationToken cancellationToken)
        {
            await _blockFunctionUnitOfWork.BeginTransactionAsync();

            try
            {
                var existName = await _readBlockCategoryRepository.AsQueryable()
                    .AnyAsync(bc => bc.ParentId == command.ParentId && bc.Name.ToLower() == command.Name.ToLower(), cancellationToken);

                if (existName)
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(AddBlockCategory.Name));
                }

                var parent = await _readBlockCategoryRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == command.ParentId);
                if (command.ParentId.HasValue && !parent)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.ParentId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }

                var blockCategory = AddBlockCategory.Create(command);

                await _blockFunctionUnitOfWork.BlockCategories.AddAsync(blockCategory);
                await _blockFunctionUnitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Add, ActionStatus.Success, blockCategory.Id, blockCategory.Name, command);

                return BlockCategoryDto.Create(blockCategory);

            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Add, ex, payload: command);
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<BlockCategoryDto> UpdateBlockCategoryAsync(UpdateBlockCategory command, CancellationToken cancellationToken)
        {
            await _blockFunctionUnitOfWork.BeginTransactionAsync();

            try
            {
                var blockCategory = await _blockFunctionUnitOfWork.BlockCategories.FindAsync(command.Id);

                if (blockCategory == null)
                {
                    throw new EntityNotFoundException();
                }

                var existName = await _readBlockCategoryRepository.AsQueryable()
                    .AnyAsync(bc => bc.ParentId == command.ParentId && bc.Name.ToLower() == command.Name.ToLower() && bc.Id != command.Id, cancellationToken);

                if (existName)
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(UpdateBlockCategory.Name));
                }
                var parent = await _readBlockCategoryRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == command.ParentId);
                if (command.ParentId.HasValue && !parent)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.ParentId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }
                await CheckRelationCategoryAsync(command);
                var entity = UpdateBlockCategory.Create(command);

                await _blockFunctionUnitOfWork.BlockCategories.UpdateAsync(command.Id, entity);
                await _blockFunctionUnitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Update, ActionStatus.Success, entity.Id, entity.Name, command);

                return BlockCategoryDto.Create(blockCategory);
            }
            catch (System.Exception ex)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Update, ex, command.Id, command.Name, payload: command);
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
        }
        public async Task CheckRelationCategoryAsync(UpdateBlockCategory command)
        {
            if (command.ParentId == null || command.ParentId == Guid.Empty)
                return;
            var blockPaths = await _readBlockCategoryRepository.GetPathsAsync((Guid)command.ParentId);
            var blockPath = blockPaths.FirstOrDefault();
            if (blockPath != null && blockPath.CategoryPathId.Contains(command.Id.ToString()))
            {
                throw EntityValidationExceptionHelper.GenerateException(nameof(command.ParentId), MessageConstants.BLOCK_FUNCTION_CATEGORY_INVALID);
            }
        }
        public async Task<BaseResponse> DeleteBlockCategoryAsync(DeleteBlockCategory command, CancellationToken cancellationToken)
        {
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            string blockCategoryName = null;
            try
            {
                var blockCategory = await _readBlockCategoryRepository.AsQueryable()
                                                    .Include(x => x.FunctionBlocks).ThenInclude(x => x.Bindings)
                                                    .Include(x => x.Children)
                                                    .Where(x => x.Id == command.Id).FirstOrDefaultAsync();
                if (blockCategory == null)
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }

                blockCategoryName = blockCategory.Name;

                if (blockCategory.Children.Any() || blockCategory.FunctionBlocks.Any())
                {
                    throw new EntityInvalidException(detailCode: MessageConstants.BLOCK_CATEGORY_HAS_CHILD);
                }

                await _blockFunctionUnitOfWork.BlockCategories.RemoveAsync(command.Id);
                await _blockFunctionUnitOfWork.CommitAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Delete, ActionStatus.Success, blockCategory.Id, blockCategory.Name, payload: command);
                return BaseResponse.Success;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Function block Id: {command.Id}");
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_CATEGORY, ActionType.Delete, ex, command.Id, blockCategoryName, payload: command);
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<BaseSearchResponse<GetBlockCategoryHierarchyDto>> HierarchySearchAsync(GetBlockCategoryHierarchy command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var blockCategoryHierarchies = await _readBlockCategoryRepository.HierarchySearchAsync(command.Name);
            var durationInMilliseconds = (long)(DateTime.UtcNow - start).TotalMilliseconds;
            return new BaseSearchResponse<GetBlockCategoryHierarchyDto>(durationInMilliseconds, blockCategoryHierarchies.Count(), command.PageSize, command.PageIndex, blockCategoryHierarchies.Select(GetBlockCategoryHierarchyDto.Create));
        }
        public async Task<IEnumerable<GetBlockCategoryPathDto>> GetPathsAsync(GetBlockCategoryPath request, CancellationToken cancellationToken)
        {
            var paths = new List<GetBlockCategoryPathDto>();
            if (request.Type == PathTypeConstants.TYPE_CATEGORY)
            {
                await AddCategoryTypePathsAsync(request.Ids, paths);
            }
            else
            {
                await AddBlockTypePathsAsync(request.Ids, paths);
            }
            return paths;
        }

        private async Task AddCategoryTypePathsAsync(IEnumerable<Guid> ids, ICollection<GetBlockCategoryPathDto> paths)
        {
            foreach (var id in ids)
            {
                var blockPaths = await _readBlockCategoryRepository.GetPathsAsync(id);
                var blockPath = blockPaths.FirstOrDefault();
                if (blockPath != null)
                {
                    var blockPathDto = new GetBlockCategoryPathDto(id, blockPath.CategoryPathId, blockPath.CategoryPathName);

                    paths.Add(blockPathDto);
                }
            }
        }

        private async Task AddBlockTypePathsAsync(IEnumerable<Guid> ids, ICollection<GetBlockCategoryPathDto> paths)
        {
            foreach (var id in ids)
            {
                var block = await _readFunctionBlockRepository.FindAsync(id);
                if (block == null)
                    throw new EntityNotFoundException();
                if (block.CategoryId == null)
                    continue;
                var blockPaths = await _readBlockCategoryRepository.GetPathsAsync(block.CategoryId);
                var blockPath = blockPaths.FirstOrDefault();
                if (blockPath != null)
                {
                    var pathId = !string.IsNullOrEmpty(blockPath.CategoryPathId) ? blockPath.CategoryPathId + "/" + block.Id : block.Id.ToString();
                    var pathName = !string.IsNullOrEmpty(blockPath.CategoryPathName) ? blockPath.CategoryPathName + "/" + block.Name : block.Name;
                    var blockPathDto = new GetBlockCategoryPathDto(id, pathId, pathName);

                    paths.Add(blockPathDto);
                }
            }
        }

        protected override Type GetDbType()
        {
            return typeof(IBlockCategoryRepository);
        }

        public async Task<IEnumerable<ArchiveBlockCategoryDto>> ArchiveAsync(ArchiveBlockCategory command, CancellationToken cancellationToken)
        {
            var categories = await _readBlockCategoryRepository.AsQueryable().AsNoTracking()
                                                                            .Where(x => !x.System && x.UpdatedUtc <= command.ArchiveTime)
                                                                            .ToListAsync();
            return categories.Select(ArchiveBlockCategoryDto.Create);
        }

        public async Task<BaseResponse> RetrieveAsync(RetrieveBlockCategory command, CancellationToken cancellationToken)
        {
            _userContext.SetUpn(command.Upn);
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveBlockCategoryDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            if (!data.Any())
            {
                return BaseResponse.Success;
            }

            var categories = data.OrderBy(x => x.CreatedUtc).Select(ArchiveBlockCategoryDto.Create);
            await _blockFunctionUnitOfWork.BeginTransactionAsync();
            try
            {
                await _blockFunctionUnitOfWork.BlockCategories.RetrieveAsync(categories);
                await _blockFunctionUnitOfWork.CommitAsync();
            }
            catch
            {
                await _blockFunctionUnitOfWork.RollbackAsync();
                throw;
            }
            return BaseResponse.Success;
        }

        public async Task<BaseResponse> VerifyArchiveAsync(VerifyArchiveBlockCategory command, CancellationToken cancellationToken)
        {
            var data = JsonConvert.DeserializeObject<IEnumerable<ArchiveBlockCategoryDto>>(command.Data, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
            foreach (var category in data)
            {
                var validation = await _validator.ValidateAsync(category);
                if (!validation.IsValid)
                {
                    throw EntityValidationExceptionHelper.GenerateException(nameof(command.Data), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
                }
            }
            return BaseResponse.Success;
        }
    }
}