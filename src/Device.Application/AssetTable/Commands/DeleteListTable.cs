using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class DeleteListTable : IRequest<BaseResponse>
    {
        public Guid AssetId { get; set; }
        public IEnumerable<Guid> Ids { get; set; }

        public DeleteListTable(Guid assetId, IEnumerable<Guid> ids)
        {
            AssetId = assetId;
            Ids = ids;
        }
    }
}
