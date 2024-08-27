using System.Threading.Tasks;
using Device.Job.Model;

namespace Device.Job.Service.Abstraction
{
    public interface IJobService
    {
        Task<JobDto> AddJobAsync(AddJob model);
        Task<JobStatusDto> GetJobStatusAsync(GetJobStatus model);
    }
}