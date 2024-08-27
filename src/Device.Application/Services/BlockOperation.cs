using System;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class BlockOperation : IBlockOperation
    {
        private BlockOperator _operator;
        public BlockOperator Operator => _operator;
        private DateTime _startTime;
        public DateTime StartTime => _startTime;
        private DateTime _endTime;
        public DateTime EndTime => _endTime;
        private string _aggregateMethod;
        public string AggregateMethod => _aggregateMethod;
        private string _timegrain;
        public string TimeGrain => _timegrain;
        private int _offset = 0;
        public int Offset => _offset;
        private object _filterValue = 0;
        public object FilterValue => _filterValue;
        public string _filterOperator;
        public string FilterOperator => _filterOperator;
        public string _filterUnit;
        public string FilterUnit => _filterUnit;
        public string _padding;
        public string Padding => _padding;
        public IBlockOperation SetAggregrateMethod(string method)
        {
            _aggregateMethod = method;
            return this;
        }

        public IBlockOperation SetEndTime(DateTime endTime)
        {
            _endTime = endTime;
            return this;
        }

        public IBlockOperation SetOperator(BlockOperator blockOperator)
        {
            _operator = blockOperator;
            return this;
        }

        public IBlockOperation SetStartTime(DateTime startTime)
        {
            _startTime = startTime;
            return this;
        }

        public IBlockOperation SetTimeGrain(string timegrain)
        {
            _timegrain = timegrain;
            return this;
        }
        public IBlockOperation SetOffset(int offset)
        {
            _offset = offset;
            return this;
        }
        public IBlockOperation SetFilterValue(object filterValue)
        {
            _filterValue = filterValue;
            return this;
        }

        public IBlockOperation SetFilterOperator(string filterOperator)
        {
            _filterOperator = filterOperator;
            return this;
        }

        public IBlockOperation SetFilterUnit(string filterUnit)
        {
            _filterUnit = filterUnit;
            return this;
        }
        public IBlockOperation SetPadding(string padding)
        {
            _padding = padding;
            return this;
        }
    }
}