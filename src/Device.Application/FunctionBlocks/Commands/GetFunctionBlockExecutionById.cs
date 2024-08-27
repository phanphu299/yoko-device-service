using System;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class GetFunctionBlockExecutionById : IRequest<FunctionBlockExecutionDto>
    {
        public Guid Id { get; set; }
        public GetFunctionBlockExecutionById(Guid id)
        {
            Id = id;
        }
    }
}