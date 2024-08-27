using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Block.Command;
using Device.Application.Block.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockService : ISearchService<Domain.Entity.FunctionBlockTemplate, Guid, GetFunctionBlockByCriteria, GetFunctionBlockSimpleDto>, IFetchService<Domain.Entity.FunctionBlockTemplate, Guid, GetFunctionBlockSimpleDto>
    {
        Task<GetFunctionBlockDto> FindEntityByIdAsync(GetFunctionBlockById command, CancellationToken token);
        Task<AddFunctionBlockDto> AddEntityAsync(AddFunctionBlock payload, CancellationToken cancellationToken);
        Task<bool> ValidationIfUsedFunctionBlockIsChangingAsync(ValidationFunctionBlockContent request, CancellationToken cancellationToken);
        Task<UpsertFunctionBlockDto> UpsertFunctionBlockAsync(UpsertFunctionBlock request, CancellationToken cancellationToken);
        Task<UpdateFunctionBlockDto> UpdateEntityAsync(UpdateFunctionBlock payload, CancellationToken cancellationToken);
        Task<GetFunctionBlockDto> GetFunctionBlockCloneAsync(GetFunctionBlockClone command, CancellationToken cancellationToken);
        Task<BaseResponse> DeleteEntityAsync(DeleteFunctionBlock payload, CancellationToken cancellationToken);
        Task<bool> CheckUsedFunctionBlockAsync(CheckUsedFunctionBlock command, CancellationToken cancellationToken);
        Task<bool> ValidationFunctionBlockAsync(ValidationFunctionBlocks command, CancellationToken token);
        Task<IEnumerable<ArchiveFunctionBlockDto>> ArchiveAsync(ArchiveFunctionBlock command, CancellationToken cancellationToken);
        Task<BaseResponse> VerifyArchiveAsync(VerifyFunctionBlock command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveFunctionBlock command, CancellationToken cancellationToken);
    }
}
