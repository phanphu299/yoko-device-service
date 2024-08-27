using System.Collections.Generic;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IExportHandler
    {
        Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids);
    }
}