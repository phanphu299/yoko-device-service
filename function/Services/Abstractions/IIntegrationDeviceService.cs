using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IIntegrationDeviceService
    {
        Task<IEnumerable<Guid>> ProcessEventAsync(IDictionary<string, object> metricDict);
    }
}
