using System.Threading.Tasks;

namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockExecutionRuntime
    {
        //void SetBlockEngine(IBlockEngine engine);
        void SetVariable(IBlockVariable variable);
        Task ExecuteAsync();
    }
}