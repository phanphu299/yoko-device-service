using System;
using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.AssetTemplate.Command
{
    public class DeleteAssetTemplate : IRequest<BaseResponse>
    {
        public Guid Id { set; get; }
        public IEnumerable<Guid> Ids { get; set; } = new List<Guid>();
    }
}