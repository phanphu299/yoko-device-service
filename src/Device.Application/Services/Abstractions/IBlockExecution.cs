using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockExecution
    {
        bool CanApply(IBlockOperation operation);
        Task<BlockQueryResult> ExecuteAsync(IBlockContext context);
    }
}