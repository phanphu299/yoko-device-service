using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface ISystemContext
    {
        Task<string> GetValueAsync(string key, string defaultValue, bool useCache = true);
    }
}