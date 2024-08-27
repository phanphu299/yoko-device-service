using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Device.Job.Model;
using Device.Job.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Job.Extension;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace Device.Job.Service
{
    public class CsvOutputFileService : IOutputFileService
    {
        private readonly ILoggerAdapter<CsvOutputFileService> _logger;

        public CsvOutputFileService(ILoggerAdapter<CsvOutputFileService> logger)
        {
            _logger = logger;
        }

        public string GetHeader(IEnumerable<FlattenHistoricalData> data)
        {
            var sb = new StringBuilder();
            var columnNames = new List<string>();
            var attributes = data.DistinctBy(x => new { x.AttributeId, x.AttributeName }).Select(x => new Job.Model.Attribute(x.AttributeId, x.AttributeName)).ToDictionary(x => x.Id, y => y.Name);

            columnNames.Add("AssetId");
            columnNames.Add("Timestamp");
            columnNames.AddRange(attributes.Values.Select(name => name));

            var header = string.Join(",", columnNames);

            return $"{header}\n";
        }

        public string GetHeader(IEnumerable<ColumnMapping> columnMappings)
        {
            var sb = new StringBuilder();
            var columnNames = new List<string>();
            var timestampColumnDefined = columnMappings.Where(x => x.AttributeId != null && x.AttributeId == Guid.Empty).FirstOrDefault();
            if (timestampColumnDefined == null)
            {
                columnNames.Add("Timestamp");
            }
            columnNames.AddRange(columnMappings.Select(x => FormatCsvString(x.Name)));
            var header = string.Join(",", columnNames);
            return $"{header}\n";
        }

        public IEnumerable<string> GetData(IEnumerable<FlattenHistoricalData> data)
        {
            var asset = data.FirstOrDefault();
            var attributes = data.DistinctBy(x => new { x.AttributeId, x.AttributeName }).Select(x => new Job.Model.Attribute(x.AttributeId, x.AttributeName)).ToDictionary(x => x.Id, y => y.Name);

            var rows = data.GroupBy(x => x.UnixTimestamp)
                             .Select(rowGroup => new
                             {
                                 Key = rowGroup.Key,
                                 Values = rowGroup
                             }).ToList();

            foreach (var row in rows)
            {
                var values = new List<object>();

                values.Add(asset.AssetId);
                values.Add(row.Key);

                var attributeValues = attributes.Select(x => ValueSelector(x.Key, row.Values));

                values.AddRange(attributeValues);

                yield return string.Join(",", values);
            }
        }

        public List<string> GetData(List<FlattenHistoricalData> data, IEnumerable<ColumnMapping> columnMappings, string timezoneOffset, string datetimeFormat, Guid activityId, Guid widgetId)
        {
            var start = DateTime.UtcNow;
            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start GetData at {DateTime.UtcNow}, total FlattenHistoricalData: {data.Count}");

            var result = new List<string>();
            var rows = data
            .GroupBy(x => x.UnixTimestamp)
            .Select(rowGroup => new
            {
                rowGroup.Key,
                Values = rowGroup
            }).ToList();

            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | GetData - End GroupBy by UnixTimestamp, total grouped FlattenHistoricalData: {rows.Count} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            start = DateTime.UtcNow;

            var columnMappingsList = columnMappings.ToList();
            var timestampColumnDefined = columnMappingsList.Find(x => x.AttributeId != null && x.AttributeId == Guid.Empty);

            foreach (var row in rows)
            {
                var values = new List<object>();
                var dateTime = row.Key.ToString().UnixTimeStampToDateTime().AddTimezoneOffset(timezoneOffset);
                var dateTimeStr = $"\"{dateTime.ToString(datetimeFormat)}\"";

                if (timestampColumnDefined == null)
                {
                    values.Add(dateTimeStr);
                }

                foreach (var columnMapping in columnMappingsList)
                {
                    object value = "";
                    if (columnMapping.AttributeId.HasValue)
                    {
                        value = columnMapping.AttributeId.Value == Guid.Empty ? dateTimeStr : ValueSelector(columnMapping.AttributeId.Value, row.Values);
                    }
                    values.Add(value);
                }

                result.Add(string.Join(",", values));
            }

            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | End GetData after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} at {DateTime.UtcNow}");
            return result;
        }

        private object ValueSelector(Guid attributeId, IEnumerable<FlattenHistoricalData> values)
        {
            var att = values.FirstOrDefault(x => x.AttributeId == attributeId);
            if (att != null)
            {
                if (att.Value is string)
                    return FormatCsvString(att.Value.ToString());
                return att.Value;
            }
            return null;
        }

        /// <summary>
        /// Cover string by double quotes. Replace double quotes = 2 double quotes.
        /// </summary>
        /// <param name="value">Ex: a"b</param>
        /// <returns>Ex: "a""b"</returns>
        private string FormatCsvString(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}