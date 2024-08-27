using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface ITimeRangeAssetSnapshotRepository
    {
        Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, IEnumerable<Domain.Entity.HistoricalEntity> metrics, int timeout, DateTime startDate, DateTime endDate);
    }
}