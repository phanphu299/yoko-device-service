using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class DeleteBlockCategory : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
    }
}
