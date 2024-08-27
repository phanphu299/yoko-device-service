namespace Device.Application.Service.Abstraction
{
    public interface IFunctionBlockExecutionResolver
    {
        IFunctionBlockExecutionRuntime ResolveInstance(string content);
    }
}
