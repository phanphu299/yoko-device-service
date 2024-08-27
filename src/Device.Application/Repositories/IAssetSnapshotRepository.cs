using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetSnapshotRepository
    {
        Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, IEnumerable<HistoricalEntity> metrics, int timeout);
    }
}
