using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Models;
using Device.Application.Template.Command;
using Device.Application.Template.Command.Model;
using Device.Application.TemplateDetail.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IDeviceTemplateService : ISearchService<Domain.Entity.DeviceTemplate, Guid, GetTemplateByCriteria, GetTemplateDto>, IFetchService<Domain.Entity.DeviceTemplate, Guid, GetTemplateDto>
    {
        Task<AddTemplatesDto> AddEntityAsync(AddTemplates payload, CancellationToken token);
        Task<UpdateTemplatesDto> UpdateEntityAsync(UpdateTemplates payload, CancellationToken token);
        Task<IEnumerable<GetValidTemplateDto>> FindAllEntityWithDefaultAsync(GetTemplateByDefault payload, CancellationToken token);
        Task<GetTemplateDto> FindEntityByIdAsync(GetTemplateByID payload, CancellationToken token);
        Task<BaseResponse> DeleteEntityAsync(DeleteTemplates payload, CancellationToken token);
        Task<ActivityResponse> ExportAsync(ExportDeviceTemplate request, CancellationToken cancellationToken);
        Task<IEnumerable<GetTemplateDetailsDto>> GetTemplateMetricsByTemplateIDAsync(GetTemplateMetricsByTemplateId request);
        Task<bool> ValidationTemplateDetailsAsync(Guid id, IEnumerable<string> keys);
        Task<bool> CheckExistMetricByTemplateIdAsync(string metricKey, Guid? templateId);
        Task<BaseResponse> CheckExistDeviceTemplatesAsync(CheckExistTemplate deviceTemplates, CancellationToken cancellationToken);
        Task<IEnumerable<ArchiveTemplateDto>> ArchiveAsync(ArchiveTemplate command, CancellationToken token);
        Task<BaseResponse> RetrieveAsync(RetrieveTemplate command, CancellationToken token);
        Task<BaseResponse> VerifyArchiveAsync(VerifyTemplate command, CancellationToken token);
        Task<bool> CheckExistBindingsByTemplateIdAsync(string bindingKey, Guid? templateId);
        Task<bool> CheckMetricUsingAsync(CheckMetricUsing command, CancellationToken token);
        Task<bool> CheckBindingUsingAsync(CheckBindingUsing command, CancellationToken token);
    }
}
