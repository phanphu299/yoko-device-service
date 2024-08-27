using System;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetAssetTableData : IRequest<IEnumerable<object>>
    {
        public Guid AssetId { get; set; }
        public Guid Id { get; set; }
        public QueryCriteria QueryCriteria { get; set; }

        public GetAssetTableData(Guid assetId, Guid id, QueryCriteria queryCriteria = null)
        {
            AssetId = assetId;
            Id = id;
            QueryCriteria = queryCriteria;
        }
    }
}