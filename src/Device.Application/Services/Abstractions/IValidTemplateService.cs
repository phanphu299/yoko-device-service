
using System;
using Device.Application.AssetAttribute.Command;
using Device.Application.Template.Command.Model;
using AHI.Infrastructure.Service.Abstraction;

namespace Device.Application.Service.Abstraction
{
    public interface IValidTemplateService : ISearchService<Domain.Entity.ValidTemplate, Guid, GetValidTemplatesByCriteria, GetValidTemplateDto>
    {
    }
}
