using System;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Service;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockSnippet.Command;
using Device.Application.BlockSnippet.Model;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Device.Application.Service
{
    public class BlockSnippetService : BaseSearchService<FunctionBlockSnippet, Guid, GetBlockSnippetByCriteria, BlockSnippetDto>, IBlockSnippetService
    {
        private readonly IBlockFunctionUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly IReadBlockSnippetRepository _readBlockSnippetRepository;

        public BlockSnippetService(IServiceProvider serviceProvider, IBlockFunctionUnitOfWork blockFunctionUnitOfWork, IAuditLogService auditLogService, IReadBlockSnippetRepository readBlockSnippetRepository)
            : base(BlockSnippetDto.Create, serviceProvider)
        {
            _unitOfWork = blockFunctionUnitOfWork;
            _auditLogService = auditLogService;
            _readBlockSnippetRepository = readBlockSnippetRepository;
        }

        public async Task<BlockSnippetDto> GetBlockSnippetByIdAsync(GetBlockSnippetById command, CancellationToken cancellationToken)
        {
            var entity = await _readBlockSnippetRepository.FindAsync(command.Id);

            if (entity == null)
            {
                throw new EntityNotFoundException();
            }

            return BlockSnippetDto.Create(entity);
        }

        public async Task<BlockSnippetDto> AddBlockSnippetAsync(AddBlockSnippet command, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existName = await _readBlockSnippetRepository.AsQueryable().AnyAsync(x => x.Name == command.Name, cancellationToken);
                if (existName)
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation((nameof(AddBlockSnippet.Name)));
                }
                var entity = AddBlockSnippet.Create(command);

                await _unitOfWork.BlockSnippets.AddAsync(entity);
                await _unitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Add, ActionStatus.Success, entity.Id, entity.Name, command);

                return BlockSnippetDto.Create(entity);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Add, ActionStatus.Fail, payload: command);
                throw;
            }
        }

        public async Task<BlockSnippetDto> UpdateBlockSnippetAsync(UpdateBlockSnippet command, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var entity = await _unitOfWork.BlockSnippets.FindAsync(command.Id);

                if (entity == null)
                {
                    throw new EntityNotFoundException();
                }

                var existName = await _unitOfWork.BlockSnippets.AsQueryable()
                    .AnyAsync(x => x.Name == command.Name && x.Id != command.Id, cancellationToken);

                if (existName)
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(UpdateBlockSnippet.Name));
                }

                var blockSnippet = UpdateBlockSnippet.Create(command);
                await _unitOfWork.BlockSnippets.UpdateAsync(command.Id, blockSnippet);
                await _unitOfWork.CommitAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Update, ActionStatus.Success, blockSnippet.Id, blockSnippet.Name, payload: command);

                return BlockSnippetDto.Create(entity);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Update, ActionStatus.Fail, command.Id, payload: command);
                throw;
            }
        }

        public async Task<BaseResponse> DeleteBlockSnippetAsync(DeleteBlockSnippet command, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var entity = await _readBlockSnippetRepository.FindAsync(command.Id);

                if (entity == null)
                {
                    throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
                }

                await _unitOfWork.BlockSnippets.RemoveAsync(entity.Id);
                await _unitOfWork.CommitAsync();

                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Delete, ActionStatus.Success, entity.Id, entity.Name, payload: command);

                return BaseResponse.Success;

            }
            catch (System.Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                await _auditLogService.SendLogAsync(ActivityEntityAction.FUNCTION_BLOCK_SNIPPET, ActionType.Delete, ex, command.Id);
                throw;
            }
        }


        protected override Type GetDbType()
        {
            return typeof(IBlockSnippetRepository);
        }
    }
}
