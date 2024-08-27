using System;
using System.Collections.Generic;
using Device.Application.Historical.Query.Model;
using MediatR;

namespace Device.Application.Historical.Query
{
    public class PaginationGetAssetAttributeSeries : GetAssetAttributeSeriesModel, IRequest<List<PaginationHistoricalDataDto>>
    {
        public int PageIndex { get; set; } = 0;

        public int PageSize { get; set; }

        public bool BePaging { get; set; }
    }
}