using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class CheckUsedBlockTemplate : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }

        public CheckUsedBlockTemplate(Guid id)
        {
            Id = id;
        }
    }
}