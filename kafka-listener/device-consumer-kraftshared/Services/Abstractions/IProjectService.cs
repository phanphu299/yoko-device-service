using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IProjectService
    {
        Task<ProjectDto> GetCurrentProjectAsync();
    }
}