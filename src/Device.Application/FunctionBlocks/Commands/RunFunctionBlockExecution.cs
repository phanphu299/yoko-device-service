using System;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class RunFunctionBlockExecution : IRequest<bool>
    {
        public Guid Id { get; set; }
        public DateTime? SnapshotDateTime { get; set; }
        public DateTime ExecutionDateTime { get; set; }
        public RunFunctionBlockExecution(Guid id, DateTime executionDateTime, DateTime? snapshotDateTime)
        {
            Id = id;
            SnapshotDateTime = snapshotDateTime;
            ExecutionDateTime = executionDateTime;
        }
    }
}