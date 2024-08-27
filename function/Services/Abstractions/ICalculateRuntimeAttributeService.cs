using System.Collections.Generic;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface ICalculateRuntimeAttributeService
    {
        Task CalculateRuntimeAttributeAsync(IDictionary<string, object> metricDict);
    }
}
