using System.Threading;
using System.Threading.Tasks;
using Device.Application.Uom.Command;
using Device.Application.Uom.Command.Model;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using System.Collections.Generic;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IUomService : ISearchService<Domain.Entity.Uom, int, GetUomByCriteria, GetUomDto>, IFetchService<Domain.Entity.Uom, int, GetUomDto>
    {
        Task<AddUomsDto> AddUomAsync(AddUom element, CancellationToken token);
        Task<GetUomDto> FindUomByIdAsync(GetUomById command, CancellationToken token);
        Task<UpdateUomsDto> UpdateUomAsync(UpdateUom command, CancellationToken token);
        Task<BaseResponse> RemoveUomAsync(DeleteUom command, CancellationToken token);
        Task<ActivityResponse> ExportAsync(ExportUom request, CancellationToken cancellationToken);
        Task<CalculationRefUomDto> CalculationRefUomAsync(CalculationRefUom request, CancellationToken cancellationToken);
        Task<BaseResponse> CheckExistUomsAsync(CheckExistUom command, CancellationToken cancellationToken);
        Task<IEnumerable<ArchiveUomDto>> ArchiveAsync(ArchiveUom command, CancellationToken cancellationToken);
        Task<BaseResponse> RetrieveAsync(RetrieveUom command, CancellationToken cancellationToken);
        Task<BaseResponse> VerifyArchiveAsync(VerifyUom command, CancellationToken cancellationToken);
    }
}
