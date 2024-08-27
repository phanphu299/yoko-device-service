using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class PublishFunctionBlockExecution : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public bool ExceptionOnError { get; set; }
        public PublishFunctionBlockExecution(Guid id, bool exceptionOnError = false)
        {
            Id = id;
            ExceptionOnError = exceptionOnError;
        }
    }
}