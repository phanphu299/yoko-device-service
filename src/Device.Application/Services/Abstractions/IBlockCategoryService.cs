using System;
using System.Threading.Tasks;
using System.Threading;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockFunctionCategory.Command;
using Device.Application.BlockFunctionCategory.Model;
using System.Collections.Generic;
using Device.Application.BlockCategory.Model;
using Device.Application.BlockCategory.Command;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockCategoryService : ISearchService<Domain.Entity.FunctionBlockCategory, Guid, GetBlockCategoryByCriteria, GetBlockCategoryDto>
    {
        Task<BaseSearchResponse<GetBlockCategoryHierarchyDto>> HierarchySearchAsync(GetBlockCategoryHierarchy command, CancellationToken cancellationToken);
        Task<GetBlockCategoryDto> GetBlockCategoryByIdAsync(GetBlockCategoryById command, CancellationToken cancellationToken);
        Task<BlockCategoryDto> AddBlockCategoryAsync(AddBlockCategory command, CancellationToken cancellationToken);
        Task<BlockCategoryDto> UpdateBlockCategoryAsync(UpdateBlockCategory command, CancellationToken cancellationToken);
        Task<BaseResponse> DeleteBlockCategoryAsync(DeleteBlockCategory command, CancellationToken cancellationToken);
        Task<IEnumerable<GetBlockCategoryPathDto>> GetPathsAsync(GetBlockCategoryPath request, CancellationToken cancellationToken);
        Task<IEnumerable<ArchiveBlockCategoryDto>> ArchiveAsync(ArchiveBlockCategory command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveBlockCategory command, CancellationToken cancellationToken);
        Task<BaseResponse> VerifyArchiveAsync(VerifyArchiveBlockCategory command, CancellationToken cancellationToken);
    }
}
