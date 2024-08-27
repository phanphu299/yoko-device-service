using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class AlarmBlockContext : IAlarmBlockContext
    {
        private string _alarmName;
        public string AlarmName => _alarmName;
        private IBlockEngine _engine;
        public IBlockEngine BlockEngine => _engine;
        private IBlockOperation _blockOperation;
        public IBlockOperation BlockOperation => _blockOperation;

        public IAlarmBlockContext SetBlockOperation(IBlockOperation blockOperation)
        {
            _blockOperation = blockOperation;
            return this;
        }
        public IAlarmBlockContext SetBlockEngine(IBlockEngine engine)
        {
            _engine = engine;
            return this;
        }
        public IAlarmBlockContext SetAlarmName(string name)
        {
            _alarmName = name;
            return this;
        }
    }
}