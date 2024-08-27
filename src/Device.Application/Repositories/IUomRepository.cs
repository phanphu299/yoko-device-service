using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IUomRepository : IRepository<Domain.Entity.Uom, int>
    {
        Task<bool> RemoveListEntityWithRelationAsync(ICollection<Domain.Entity.Uom> uoms);
        Task<Domain.Entity.Uom> UpdateAsync(Domain.Entity.Uom uom);
        Task RetrieveAsync(IEnumerable<Domain.Entity.Uom> uoms);
    }
}
