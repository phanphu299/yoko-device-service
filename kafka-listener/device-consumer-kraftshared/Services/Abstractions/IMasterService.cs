using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IMasterService
    {
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
    }
}
