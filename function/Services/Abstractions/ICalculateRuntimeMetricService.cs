using System.Collections.Generic;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface ICalculateRuntimeMetricService
    {
        Task CalculateRuntimeMetricAsync(IDictionary<string, object> metricDict);
    }
}