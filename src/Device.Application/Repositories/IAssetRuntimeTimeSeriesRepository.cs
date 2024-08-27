using System.Threading.Tasks;

namespace Device.Application.Repository
{
    public interface IAssetRuntimeTimeSeriesRepository : IAssetTimeSeriesRepository
    {
        Task SaveAssetAttributeValueAsync(params Domain.Entity.TimeSeries[] timeSeries);
        //Task CanSaveAssetAttributeValueAsync(params Domain.Entity.TimeSeries[] timeSeries);
    }
}
