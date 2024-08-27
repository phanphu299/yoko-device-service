using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockTriggerHandler
    {
        bool CanHandle(string triggerType);
        Task RegisterAsync(Domain.Entity.FunctionBlockExecution block);
        Task UnregisterAsync(Domain.Entity.FunctionBlockExecution block);
    }
}