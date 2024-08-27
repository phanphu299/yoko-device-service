using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
namespace Device.Application.Service
{
    public abstract class BaseFunctionBlockExecutionRuntime : IFunctionBlockExecutionRuntime
    {
        //private IBlockEngine _engine;
        private IBlockVariable _blockVariable;

        protected IBlockVariable AHI => _blockVariable;
        // public void SetBlockEngine(IBlockEngine engine)
        // {
        //     _engine = engine;
        // }

        public abstract Task ExecuteAsync();

        public void SetVariable(IBlockVariable variable)
        {
            _blockVariable = variable;
        }
    }
}