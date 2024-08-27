using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Block.Command.Model;
using Device.Application.BlockTemplate.Command;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.BlockTemplate.Query;

namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockTemplateService : ISearchService<Domain.Entity.FunctionBlockTemplate, Guid, GetFunctionBlockTemplateByCriteria, FunctionBlockTemplateSimpleDto>, IFetchService<Domain.Entity.FunctionBlockTemplate, Guid, FunctionBlockTemplateSimpleDto>
    {
        Task<IEnumerable<GetFunctionBlockDto>> GetFunctionBlocksByTemplateIdAsync(GetFunctionBlockByTemplateId command, CancellationToken cancellationToken);
        Task<FunctionBlockTemplateDto> AddBlockTemplateAsync(AddFunctionBlockTemplate command, CancellationToken cancellationToken);
        Task<FunctionBlockTemplateDto> UpdateBlockTemplateAsync(UpdateFunctionBlockTemplate command, CancellationToken cancellationToken);
        Task<BaseResponse> RemoveBlockTemplatesAsync(DeleteFunctionBlockTemplate command, CancellationToken cancellationToken);
        Task<GetFunctionBlockTemplateDto> FindBlockTemplateByIdAsync(GetBlockTemplateById command, CancellationToken cancellationToken);
        FunctionBlockTemplateContentDto GetFunctionBlockTemplateContent(string content);
        Task<bool> CheckUsedBlockTemplateAsync(CheckUsedBlockTemplate command, CancellationToken cancellationToken);
        Task<bool> ValidationChangedTemplateAsync(ValidationBlockContent command, CancellationToken cancellationToken);
        Task<bool> ValidationBlockTemplateAsync(ValidationBlockTemplates command, CancellationToken cancellationToken);
        Task<IEnumerable<Guid>> UpdateBlockTemplateContentsAsync(Guid functionBlockId);
        Task<IEnumerable<ArchiveBlockTemplateDto>> ArchiveAsync(ArchiveBlockTemplate command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveBlockTemplate command, CancellationToken cancellationToken);
        Task<BaseResponse> VerifyArchiveAsync(VerifyArchiveBlockTemplate command, CancellationToken cancellationToken);
    }
}
