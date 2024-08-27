using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Device.Function.Service.Abstraction;
using Dapper;
using AHI.Device.Function.Model.ExportModel;
using System.Linq;
using System;
using System.IO;
using AHI.Infrastructure.Export.Builder;
using System.Data;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using Function.Extension;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using Newtonsoft.Json.Linq;
using DeviceContentKeys = AHI.Device.Function.Constant.JsonPayloadKeys.DeviceContent;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace AHI.Device.Function.Service
{
    public class DeviceExportHandler : IExportHandler
    {
        private readonly IParserContext _context;
        private readonly IStorageService _storageService;
        private readonly IIotBrokerService _iotBrokerService;
        private readonly ExcelExportBuider _excelExportBuidler;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly ITagService _tagService;
        private readonly ILoggerAdapter<DeviceExportHandler> _logger;

        public DeviceExportHandler(IParserContext parserContext,
                                   IStorageService storageService, ExcelExportBuider excelExportBuidler,
                                   IIotBrokerService iotBrokerService,
                                   IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory,
                                   ITagService tagService,
                                   ILoggerAdapter<DeviceExportHandler> logger)
        {
            _context = parserContext;
            _storageService = storageService;
            _iotBrokerService = iotBrokerService;
            _excelExportBuidler = excelExportBuidler;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _tagService = tagService;
            _logger = logger;
        }

        public async Task<string> HandleAsync(string workingFolder, IEnumerable<string> ids)
        {
            var datetime_format = GetDateTimeFormatInfo();
            var timezone_offset = GetTimeZoneInfo();

            var path = Path.Combine(workingFolder, "AppData", "ExportTemplate", "DeviceModel.xlsx");

            _excelExportBuidler.SetTemplate(path);

            var escapedIds = ids.Select(x => x.Replace("'", "''"));
            var devices = await GetDeviceAsync(escapedIds);
            devices.ConvertDateTimeFormat(datetime_format, timezone_offset,
                x => x.TimeStampValue.Value,
                (x, v) => x.TimeStamp = v
            );

            _excelExportBuidler.SetData<DeviceModel>(
                sheetName: "Devices",
                data: new List<DeviceModel>(devices)
            );

            var fileName = $"Devices_{DateTime.UtcNow.ToTimestamp(timezone_offset)}.xlsx";

            var uniqueFilePath = $"{StorageConstants.DefaultExportPath}/{Guid.NewGuid():N}";
            return await _storageService.UploadAsync(uniqueFilePath, fileName, _excelExportBuidler.BuildExcelStream());
        }

        private async Task<IEnumerable<DeviceModel>> GetDeviceAsync(IEnumerable<string> ids)
        {
            IEnumerable<DeviceModel> devices;
            IEnumerable<EntityTag> tags;
            var query = BuildQueries(ids);
            var tagsQuery = BuildTagQueries(ids);

            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection())
            {
                devices = await dbConnection.QueryAsync<DeviceModel>(query, commandTimeout: 600);
                tags = await dbConnection.QueryAsync<EntityTag>(tagsQuery, commandTimeout: 600);

                await dbConnection.CloseAsync();
            }

            if (!devices.Any())
                throw new InvalidOperationException(ValidationMessage.EXPORT_NOT_FOUND);

            await PopulateBrokerNameAsync(devices);

            if (tags.Any())
            {
                foreach (EntityTag entityTag in tags)
                {
                    DeviceModel deviceModel = devices.FirstOrDefault(d => d.Id == entityTag.EntityIdString);

                    if (deviceModel != null)
                    {
                        deviceModel.TagIds.Add(entityTag.TagId);
                    }
                }
            }

            await PopulateTagsAsync(devices);

            return devices;
        }

        private async Task PopulateTagsAsync(IEnumerable<DeviceModel> devices)
        {
            try
            {
                var tagIds = devices.SelectMany(d => d.TagIds).Distinct();
                var tags = await _tagService.FetchTagsAsync(tagIds);

                foreach (var device in devices.Where(d => d.TagIds.Any()))
                {
                    device.Tags = string.Join(TagConstants.TAG_IMPORT_EXPORT_SEPARATOR, tags.Where(x => device.TagIds.Contains(x.Id)).Select(x => $"{x.Key} : {x.Value}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeviceExportHandler - PopulateTagsAsync: {ex.Message}");
            }
        }

        private string BuildQueries(IEnumerable<string> ids)
        {
            return @$"SELECT 
                        d.id AS Id,
                        d.name,
                        t.name AS {nameof(DeviceModel.Template)},
                        dss.status AS {nameof(DeviceModel.Status)},
                        dss._ts AS {nameof(DeviceModel.TimeStampValue)},
                        t.total_metric AS {nameof(DeviceModel.Metrics)},
                        d.retention_days AS {nameof(DeviceModel.RetentionDays)},
                        d.device_content AS {nameof(DeviceModel.DeviceContent)},
                        d.telemetry_topic AS {nameof(DeviceModel.TelemetryTopic)},
                        d.command_topic AS {nameof(DeviceModel.CommandTopic)},
                        d.has_command AS {nameof(DeviceModel.HasCommand)}
                    FROM devices d
                    JOIN {DbName.Table.DEVICE_TEMPLATE} t ON d.device_template_id = t.id
                    JOIN v_device_snapshot dss ON d.id = dss.device_id
                    JOIN (SELECT * FROM {BuildExportIdList(ids)} AS d_list(id, ordinal)) d_list ON d.id = d_list.id
                    ORDER BY d_list.ordinal";
        }

        private string BuildTagQueries(IEnumerable<string> ids)
        {
            var exportedIds = ids.Distinct().Select(id => $"'{id}'");
            var paramString = $"({string.Join(',', exportedIds)})";

            return $@"select tag_id as TagId, entity_id_varchar as EntityIdString 
                                from {DbName.Table.ENTITY_TAG} 
                                where entity_id_varchar IN {paramString} AND entity_type = 'device'";
        }


        private string BuildExportIdList(IEnumerable<string> ids)
        {
            var ordinal = 1;
            var exportedIds = ids.Distinct().Select(id => $"'{id}'");
            return $"(VALUES {string.Join(',', exportedIds.Select(id => $"({id},{ordinal++})"))})";
        }

        private async Task PopulateBrokerNameAsync(IEnumerable<DeviceModel> devices)
        {
            try
            {
                ParseContent(devices);

                var brokers = await _iotBrokerService.SearchSharedBrokersAsync();
                if (!brokers.Any())
                {
                    foreach (var device in devices)
                        device.BrokerName = "N/A";

                    return;
                }

                var brokerInfos = brokers.ToDictionary(broker => (broker.Id, broker.ProjectId), broker => broker.Name);
                foreach (var device in devices)
                {
                    if (!device.BrokerId.HasValue)
                        continue;

                    if (brokerInfos.TryGetValue((device.BrokerId.Value, device.BrokerProjectId), out var brokerName))
                        device.BrokerName = brokerName;
                    else
                        device.BrokerName = "N/A";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeviceExportHandler - PopulateBrokerNameAsync: {ex.Message}");
            }
        }

        private void ParseContent(IEnumerable<DeviceModel> devices)
        {
            foreach (var device in devices)
            {
                if (string.IsNullOrEmpty(device.DeviceContent))
                    continue;

                var parsedContent = JObject.Parse(device.DeviceContent);
                if (!parsedContent.ContainsKey(DeviceContentKeys.BROKER_ID))
                    continue;

                var brokerId = parsedContent.Value<string>(DeviceContentKeys.BROKER_ID);
                if (string.IsNullOrEmpty(brokerId))
                    continue;

                device.BrokerId = brokerId.ToGuid();
                device.BrokerProjectId = parsedContent.Value<string>(DeviceContentKeys.BROKER_PROJECT_ID);
                device.PrimaryKey = parsedContent.Value<string>(DeviceContentKeys.PRIMARY_KEY);
                device.SasTokenDuration = parsedContent.Value<string>(DeviceContentKeys.SAS_TOKEN_DURATION).ToInt();
                device.TokenDuration = parsedContent.Value<string>(DeviceContentKeys.TOKEN_DURATION).ToInt();
            }
        }

        private string GetDateTimeFormatInfo()
        {
            return _context.GetContextFormat(ContextFormatKey.DATETIMEFORMAT) ?? DateTimeExtensions.DEFAULT_DATETIME_FORMAT;
        }

        private string GetTimeZoneInfo()
        {
            var timezone_offset = _context.GetContextFormat(ContextFormatKey.DATETIMEOFFSET) ?? DateTimeExtensions.DEFAULT_DATETIME_OFFSET;
            return DateTimeExtensions.ToValidOffset(timezone_offset);
        }
    }
}