using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using System;
using Device.Application.AssetAttribute.Command;
using Device.Application.Template.Command.Model;
using AHI.Infrastructure.Service;

namespace Device.Application.Service
{
    public class ValidTemplateService : BaseSearchService<Domain.Entity.ValidTemplate, Guid, GetValidTemplatesByCriteria, GetValidTemplateDto>, IValidTemplateService
    {

        public ValidTemplateService(IServiceProvider serviceProvider)
            : base(GetValidTemplateDto.Create, serviceProvider)
        {
        }

        protected override Type GetDbType() { return typeof(IValidTemplateRepository); }
    }
}
