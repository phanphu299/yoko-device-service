using System.Threading;
using System.Threading.Tasks;
using Device.Application.Analytic.Query;
using Device.Application.Analytic.Query.Model;
namespace Device.Application.Service.Abstraction
{
    public interface IAssetAnalyticService
    {
        Task<AssetHistogramDataDto> GetHistogramDataAsync(AssetAttributeHistogramData command, CancellationToken token);
        Task<AssetStatisticsDataDto> GetStatisticsDataAsync(AssetAttributeStatisticsData request, CancellationToken token);

    }
}
