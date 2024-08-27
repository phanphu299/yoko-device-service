using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetSnapshotService
    {
        Task<IEnumerable<HistoricalDataDto>> GetSnapshotDataAsync(GetHistoricalData command, CancellationToken token);
    }
}
