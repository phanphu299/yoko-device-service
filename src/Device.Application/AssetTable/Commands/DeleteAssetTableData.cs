using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class DeleteAssetTableData : IRequest<BaseResponse>
    {
        public Guid AssetId { get; set; }
        public Guid Id { get; set; }

        public IEnumerable<object> Ids { get; set; }

        public DeleteAssetTableData(Guid assetId, Guid id, IEnumerable<object> ids)
        {
            AssetId = assetId;
            Id = id;
            Ids = ids;
        }
    }
}