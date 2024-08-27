using System;
using System.Collections.Generic;
using Device.Application.Historical.Query.Model;
using MediatR;

namespace Device.Application.Historical.Query
{
    public class PaginationGetHistoricalData : GetHistoricalData, IRequest<IEnumerable<PaginationHistoricalDataDto>>
    {
        public int PageIndex { get; set; }

        public Guid ActivityId { get; set; }

        public Guid WidgetId { get; set; }

        public PaginationGetHistoricalData(Guid assetId, IEnumerable<Guid> attributeIds) : base(assetId, attributeIds)
        {
        }

        public PaginationGetHistoricalData(long? timeStart, long? timeEnd, string timegrain, string aggregate, int timeout, Guid assetId, IEnumerable<Guid> attributeIds, bool useCustomeTimeRange, HistoricalDataType requestType, string timezoneOffset, string gapfillFunction, int pageIndex, int pageSize, int? quality = null)
                                        : base(timeStart, timeEnd, timegrain, aggregate, timeout, assetId, attributeIds, useCustomeTimeRange, requestType, timezoneOffset, gapfillFunction, pageSize, quality)
        {
            PageIndex = pageIndex;
        }

        public static PaginationGetHistoricalData Create(AssetAttributeSeries asset, PaginationGetAssetAttributeSeries request)
        {
            return new PaginationGetHistoricalData(asset.Start, asset.End, asset.TimeGrain, asset.Aggregate, request.TimeoutInSecond, asset.AssetId, asset.AttributeIds, asset.UseCustomTimeRange, asset.RequestType, request.TimezoneOffset, asset.GapfillFunction, request.PageIndex, request.PageSize, request.Quality);
        }
    }
}