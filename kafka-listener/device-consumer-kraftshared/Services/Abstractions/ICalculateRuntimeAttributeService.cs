using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface ICalculateRuntimeAttributeService
    {
        Task CalculateRuntimeAttributeAsync(
            string projectId,
            IngestionMessage[] ingesMessages,
            IDbConnection dbConnection);
    }
}
