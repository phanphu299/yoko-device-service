using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class DeleteFunctionBlockTemplate : IRequest<BaseResponse>
    {
        public IEnumerable<Guid> Ids { get; set; } = new List<Guid>();
    }
}
