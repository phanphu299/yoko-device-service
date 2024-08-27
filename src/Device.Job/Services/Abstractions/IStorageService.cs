using System.Threading.Tasks;

namespace Device.Job.Service.Abstraction
{
    public interface IStorageService
    {
        Task<string> UploadFromEndpointAsync(string path, string fileName, string downloadEndpoint);
    }
}