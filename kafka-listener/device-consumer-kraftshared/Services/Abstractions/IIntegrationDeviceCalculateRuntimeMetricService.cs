using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IIntegrationDeviceCalculateRuntimeMetricService : IIngestionProcessEventService
    {
        Task CalculateRuntimeMetricAsync(IngestionMessage[] ingesMessages, IDbConnection readDbConnection = null);
        Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IngestionMessage[] messages, string projectId, IDbConnection dbConnection = null);
    }
}
