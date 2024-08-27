using System;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
namespace Device.Persistence.Repository
{
    public class BlockSnippetRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<FunctionBlockSnippet, Guid>, IBlockSnippetRepository, IReadBlockSnippetRepository
    {

        public BlockSnippetRepository(DeviceDbContext deviceDbContext) : base(deviceDbContext)
        {
        }

        protected override void Update(FunctionBlockSnippet requestObject, FunctionBlockSnippet targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.TemplateCode = requestObject.TemplateCode;
            targetObject.UpdatedUtc = DateTime.UtcNow;
        }
    }
}
