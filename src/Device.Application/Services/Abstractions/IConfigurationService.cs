using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IConfigurationService
    {
        Task<Lookup> FindLookupByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}
