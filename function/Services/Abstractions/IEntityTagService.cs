using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IEntityTagService
    {
        Task<IEnumerable<EntityTagDto>> GetEntityIdsByTagIdsAsync(long[] tagIds);
        Task RemoveCachesAsync(IEnumerable<EntityTagDto> entityTags);
    }
}