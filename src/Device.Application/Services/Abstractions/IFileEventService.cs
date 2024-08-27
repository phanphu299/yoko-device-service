using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IFileEventService
    {
        Task SendImportEventAsync(string objectType, IEnumerable<string> data, Guid? correlationId = null);
        Task SendExportEventAsync(Guid activityId, string objectType, IEnumerable<string> data);
    }
}
