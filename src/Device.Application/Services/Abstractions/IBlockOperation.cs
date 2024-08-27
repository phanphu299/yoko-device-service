using System;
using Device.Application.Enum;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockOperation
    {
        DateTime StartTime { get; }
        DateTime EndTime { get; }
        string AggregateMethod { get; }
        string TimeGrain { get; }
        int Offset { get; }
        string Padding { get; }
        object FilterValue { get; }
        string FilterOperator { get; }
        string FilterUnit { get; }
        BlockOperator Operator { get; }
        IBlockOperation SetOperator(BlockOperator blockOperator);
        IBlockOperation SetStartTime(DateTime startTime);
        IBlockOperation SetEndTime(DateTime endTime);
        IBlockOperation SetAggregrateMethod(string method);
        IBlockOperation SetTimeGrain(string timegrain);
        IBlockOperation SetOffset(int offset);
        IBlockOperation SetFilterValue(object filterValue);
        IBlockOperation SetFilterOperator(string filterOperator);
        IBlockOperation SetFilterUnit(string filterUnit);
        IBlockOperation SetPadding(string padding);
    }
}