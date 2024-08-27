using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class CheckUsedFunctionBlock : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }

        public CheckUsedFunctionBlock(Guid id)
        {
            Id = id;
        }
    }
}