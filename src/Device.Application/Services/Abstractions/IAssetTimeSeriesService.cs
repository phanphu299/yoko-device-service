using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetTimeSeriesService
    {
        Task<IEnumerable<HistoricalDataDto>> GetTimeSeriesDataAsync(GetHistoricalData command, CancellationToken token);
        Task<PaginationHistoricalDataDto> PaginationGetTimeSeriesDataAsync(PaginationGetHistoricalData command, CancellationToken token);
    }
}
