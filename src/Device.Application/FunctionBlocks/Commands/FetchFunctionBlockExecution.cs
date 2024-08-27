using System;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class FetchFunctionBlockExecution : IRequest<FunctionBlockExecutionDto>
    {
        public Guid Id { get; set; }

        public FetchFunctionBlockExecution(Guid id)
        {
            Id = id;
        }
    }
}