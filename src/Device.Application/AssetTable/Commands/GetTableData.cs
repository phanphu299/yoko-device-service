using System;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetTableData : IRequest<IEnumerable<object>>
    {
        public Guid Id { get; set; }
        public QueryCriteria QueryCriteria { get; set; }

        public GetTableData(Guid id, QueryCriteria queryCriteria = null)
        {
            Id = id;
            QueryCriteria = queryCriteria;
        }
    }
}