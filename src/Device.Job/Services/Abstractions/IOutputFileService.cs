using System;
using System.Collections.Generic;
using Device.Job.Model;

namespace Device.Job.Service.Abstraction
{
    public interface IOutputFileService
    {
        string GetHeader(IEnumerable<FlattenHistoricalData> data);
        string GetHeader(IEnumerable<ColumnMapping> columnMappings);
        IEnumerable<string> GetData(IEnumerable<FlattenHistoricalData> data);
        List<string> GetData(List<FlattenHistoricalData> data, IEnumerable<ColumnMapping> columnMappings, string timezoneOffset, string datetimeFormat, Guid activityId, Guid widgetId);
    }
}