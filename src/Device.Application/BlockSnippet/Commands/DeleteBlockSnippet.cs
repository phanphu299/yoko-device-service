using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockSnippet.Command
{
    public class DeleteBlockSnippet : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
    }
}
