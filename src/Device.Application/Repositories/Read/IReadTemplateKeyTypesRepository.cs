using AHI.Infrastructure.Repository.Generic;
namespace Device.Application.Repository
{
    public interface IReadTemplateKeyTypesRepository : ISearchRepository<Domain.Entity.TemplateKeyType, int>
    {
    }
}