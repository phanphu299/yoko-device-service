
using Device.Application.TemplateKeyType.Command;
using Device.Application.TemplateKeyType.Command.Model;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Service;
using System;

namespace Device.Application.Service
{
    public class TemplateKeyTypesService : BaseSearchService<Domain.Entity.TemplateKeyType, int, GetTemplateKeyTypeByCriteria, GetTemplateKeyTypeDto>, ITemplateKeyTypesService
    {
        public TemplateKeyTypesService(IServiceProvider serviceProvider)
            : base(GetTemplateKeyTypeDto.Create, serviceProvider)
        {
        }

        protected override Type GetDbType()
        {
            return typeof(IReadTemplateKeyTypesRepository);
        }
    }
}
