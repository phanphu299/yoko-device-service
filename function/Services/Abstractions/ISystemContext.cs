using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface ISystemContext
    {
        Task<string> GetValueAsync(string key, string defaultValue, bool useCache = true);
    }
}
