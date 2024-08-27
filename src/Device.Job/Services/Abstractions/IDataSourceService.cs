using System;
using System.Collections.Generic;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Job.Model;

namespace Device.Job.Service.Abstraction
{
    public interface IDataSourceService
    {
        IAsyncEnumerable<IEnumerable<FlattenHistoricalData>> GetDataAsync(ITenantContext tenantContext, JobInfo jobInfo);
        IAsyncEnumerable<List<FlattenHistoricalData>> GetPaginationDataAsync(GetFullAssetAttributeSeries assetTimeseries, Guid activityId, Guid widgetId);
    }
}