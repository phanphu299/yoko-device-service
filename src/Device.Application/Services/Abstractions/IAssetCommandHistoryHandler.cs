using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetCommandHistoryHandler
    {
        Task SaveAssetAttributeValueAsync(params Domain.Entity.TimeSeries[] timeSeries);
    }
}