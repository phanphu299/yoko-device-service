using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Application.Repository
{
    public interface IReadRepository<TE, TK> where TE : IEntity<TK>
    {
        Task<TE> FindAsync(TK id);
        IQueryable<TE> AsQueryable();
        IQueryable<TE> AsFetchable();
    }
}
