using System;
using System.Threading.Tasks;
using System.Threading;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockSnippet.Command;
using Device.Application.BlockSnippet.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockSnippetService : ISearchService<Domain.Entity.FunctionBlockSnippet, Guid, GetBlockSnippetByCriteria, BlockSnippetDto>
    {
        Task<BlockSnippetDto> GetBlockSnippetByIdAsync(GetBlockSnippetById command, CancellationToken cancellationToken);
        Task<BlockSnippetDto> AddBlockSnippetAsync(AddBlockSnippet command, CancellationToken cancellationToken);
        Task<BlockSnippetDto> UpdateBlockSnippetAsync(UpdateBlockSnippet command, CancellationToken cancellationToken);
        Task<BaseResponse> DeleteBlockSnippetAsync(DeleteBlockSnippet command, CancellationToken cancellationToken);
    }
}
