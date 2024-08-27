
using System;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
namespace Device.Persistence.Repository
{
    public class ValidTemplatePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<ValidTemplate, Guid>, IValidTemplateRepository, IReadValidTemplateRepository
    {
        public ValidTemplatePersistenceRepository(DeviceDbContext context) : base(context)
        {
        }

        protected override void Update(ValidTemplate requestObject, ValidTemplate targetObject) { }

    }
}