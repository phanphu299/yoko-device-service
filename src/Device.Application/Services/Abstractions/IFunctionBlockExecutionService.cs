using System;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.BlockFunction.Query;
using Device.Application.FunctionBlock.Command;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using System.Collections.Generic;
using Device.Application.Constant;
using System.Linq.Expressions;
using Device.Domain.Entity;

namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockExecutionService
     : ISearchService<Domain.Entity.FunctionBlockExecution, Guid, GetFunctionBlockExecutionByCriteria, FunctionBlockExecutionDto>
     , IFetchService<Domain.Entity.FunctionBlockExecution, Guid, FunctionBlockExecutionDto>
    {
        Task<FunctionBlockExecutionDto> AddFunctionBlockExecutionAsync(AddFunctionBlockExecution command, CancellationToken token);
        Task<FunctionBlockExecutionDto> UpdateFunctionBlockExecutionAsync(UpdateFunctionBlockExecution command, CancellationToken token);
        Task<BaseResponse> DeleteFunctionBlockExecutionAsync(DeleteFunctionBlockExecution payload, CancellationToken token);
        Task<bool> ExecuteFunctionBlockExecutionAsync(Guid id, DateTime start, DateTime? snapshotDateTime);
        Task<bool> UnpublishFunctionBlockExecutionAsync(Guid id);
        Task<bool> PublishFunctionBlockExecutionAsync(Guid id);
        Task<FunctionBlockExecutionDto> GetFunctionBlockExecutionAsync(GetFunctionBlockExecutionById request, CancellationToken cancellationToken);
        Task RefreshBlockExecutionByTemplateIdAsync(Guid templateId, bool hasDiagramChanged);
        Task<IEnumerable<FunctionBlockExecutionAssetAttributeDto>> GetFunctionBlockExecutionDependencyAsync(Guid[] attributeIds);
        Task<ValidationBlockExecutionDto> ValidateBlockExecutionAsync(ValidationBlockExecution request, CancellationToken cancellationToken);
        //Task<bool> UpdateBlockExecutionStatusAsync(IEnumerable<Guid> ids, string status = BlockExecutionStatusConstants.STOPPED);
        Task UpdateBlockExecutionStatusAsync(Expression<Func<FunctionBlockExecution, bool>> filter, Predicate<FunctionBlockExecution> conditionToChangeStatus, string targetStatus = BlockExecutionStatusConstants.STOPPED);
        Task<IEnumerable<ArchiveFunctionBlockExecutionDto>> ArchiveAsync(ArchiveFunctionBlockExecution command, CancellationToken cancellationToken = default(CancellationToken));
        Task<BaseResponse> VerifyArchiveAsync(VerifyFunctionBlockExecution command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveFunctionBlockExecution command, CancellationToken cancellationToken);
    }
}
