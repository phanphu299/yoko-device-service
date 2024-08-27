
using Device.Application.TemplateKeyType.Command;
using Device.Application.TemplateKeyType.Command.Model;
using AHI.Infrastructure.Service.Abstraction;

namespace Device.Application.Service.Abstraction
{
    public interface ITemplateKeyTypesService : ISearchService<Domain.Entity.TemplateKeyType, int, GetTemplateKeyTypeByCriteria, GetTemplateKeyTypeDto>
    {
    }
}
