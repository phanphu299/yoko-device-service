using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class UnpublishFunctionBlockExecution : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public bool ExceptionOnError { get; set; } = false;
        public UnpublishFunctionBlockExecution(Guid id, bool exceptionOnError = false)
        {
            Id = id;
            ExceptionOnError = exceptionOnError;
        }
    }
}