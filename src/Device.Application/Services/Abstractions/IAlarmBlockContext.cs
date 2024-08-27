namespace Device.Application.Service.Abstraction
{
    public interface IAlarmBlockContext
    {
        IBlockEngine BlockEngine { get; }
        IAlarmBlockContext SetBlockEngine(IBlockEngine engine);
        string AlarmName { get; }
        IAlarmBlockContext SetAlarmName(string name);
        IBlockOperation BlockOperation { get; }
        IAlarmBlockContext SetBlockOperation(IBlockOperation blockOperation);

    }
}