using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Service.Dapper.Abstraction;
using AHI.Infrastructure.Service.Dapper.Extension;
using Dapper;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.Constant;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.Models;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Models;
using AHI.Infrastructure.Service.Tag.Helper;
using AHI.Infrastructure.Service.Tag.Enum;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Device.Persistence.Repository
{
    public class DevicePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Domain.Entity.Device, string>, IDeviceRepository, IReadDeviceRepository
    {
        private readonly DeviceDbContext _context;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IQueryService _queryService;
        public DevicePersistenceRepository(DeviceDbContext context, IDbConnectionFactory dbConnectionFactory, IQueryService queryService) : base(context)
        {
            _context = context;
            _dbConnectionFactory = dbConnectionFactory;
            _queryService = queryService;
        }

        protected override void Update(Domain.Entity.Device requestObject, Domain.Entity.Device targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.TelemetryTopic = requestObject.TelemetryTopic;
            targetObject.CommandTopic = requestObject.CommandTopic;
            targetObject.HasCommand = requestObject.HasCommand;
            targetObject.Description = requestObject.Description;
            targetObject.Status = requestObject.Status ?? targetObject.Status;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.DeviceContent = requestObject.DeviceContent;
            targetObject.EnableHealthCheck = requestObject.EnableHealthCheck;
            targetObject.MonitoringTime = requestObject.MonitoringTime;
            targetObject.HealthCheckMethodId = requestObject.HealthCheckMethodId;
            targetObject.SignalQualityCode = requestObject.SignalQualityCode;
            targetObject.RetentionDays = requestObject.RetentionDays;
        }

        public override IQueryable<Domain.Entity.Device> AsQueryable()
        {
            return base.AsQueryable()
                .Include(x => x.Template)
                .Include(x => x.DeviceSnaphot);
        }

        public override IQueryable<Domain.Entity.Device> AsFetchable()
        {
            return _context.Devices.AsNoTracking().Select(x => new Domain.Entity.Device { Id = x.Id, Name = x.Name });
        }

        public override Task<Domain.Entity.Device> FindAsync(string id)
        {
            return AsQueryable()
                .Include(x => x.Template).ThenInclude(x => x.Bindings)
                .Include(x => x.EntityTags)
                .Where(x => x.Id == id && (!x.EntityTags.Any() || x.EntityTags.Any(e => e.EntityType == Privileges.Device.ENTITY_NAME)))
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Point the old device's relations to the new one
        /// </summary>
        public async Task UpdateDeviceRelationNavigationsAsync(string oldDeviceId, Domain.Entity.Device newDevice)
        {
            var assetAttributeDynamics = await _context.AssetAttributeDynamic
                                                        .AsQueryable()
                                                        .Where(x => x.DeviceId == oldDeviceId).ToListAsync();
            var assetAttributeDynamicMappings = await _context.AssetAttributeDynamicMapping
                                                        .AsQueryable()
                                                        .Where(x => x.DeviceId == oldDeviceId).ToListAsync();
            var assetAttributeCommands = await _context.AssetAttributeCommands
                                                        .AsQueryable()
                                                        .Where(x => x.DeviceId == oldDeviceId).ToListAsync();
            var assetAttributeCommandMappings = await _context.AssetAttributeCommandMappings
                                                        .AsQueryable()
                                                        .Where(x => x.DeviceId == oldDeviceId).ToListAsync();

            foreach (var att in assetAttributeDynamics)
            {
                att.DeviceId = newDevice.Id;
            }

            foreach (var att in assetAttributeDynamicMappings)
            {
                att.DeviceId = newDevice.Id;
            }

            foreach (var att in assetAttributeCommands)
            {
                att.DeviceId = newDevice.Id;
            }

            foreach (var att in assetAttributeCommandMappings)
            {
                att.DeviceId = newDevice.Id;
            }
        }

        public virtual async Task<bool> RemoveEntityWithRelationAsync(string id)
        {
            // return for import
            _ = await ProcessRemoveEntityWithRelationAsync(new Domain.Entity.Device { Id = id });
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> RemoveListEntityWithRelationAsync(ICollection<Domain.Entity.Device> devices)
        {
            foreach (Domain.Entity.Device device in devices)
            {
                _ = await ProcessRemoveEntityWithRelationAsync(device);
            }
            return true;
        }

        private async Task<bool> ProcessRemoveEntityWithRelationAsync(Domain.Entity.Device device)
        {
            var trackingDevice = await AsQueryable()
                   .Include(x => x.Template)
                   .Where(a => a.Id == device.Id).FirstOrDefaultAsync();

            //entity not found
            if (trackingDevice == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

            //using in attribute(elementproperties)
            var trackingElementProperties = await _context.Set<AssetAttribute>().AsQueryable()
                    .Where(a => a.AssetAttributeRuntime.Expression.Contains($"{device.Id}."))
                    .Where(a => a.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
                    .FirstOrDefaultAsync();
            if (trackingElementProperties != null)
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_USING);

            var assetTemplateUsedDevice = await _context.AssetAttributeDynamicMapping.FirstOrDefaultAsync(x => x.DeviceId == device.Id);
            if (assetTemplateUsedDevice != null)
                throw new EntityInvalidException(detailCode: MessageConstants.DEVICE_USING);

            _context.Remove(trackingDevice);

            return true;
        }

        public async Task<IEnumerable<DeviceMetricSnapshotInfo>> GetMetricSnapshotAsync(string id)
        {
            return await _context.DeviceMetricSnapshots.AsNoTracking().Where(x => x.DeviceId == id).ToListAsync();
        }

        public Task<bool> IsDuplicateDeviceIdAsync(string id)
        {
            return _context.Devices.AsNoTracking().AnyAsync(x => x.Id.ToLower() == id.ToLower());
        }

        public async Task<IEnumerable<Domain.Entity.Device>> GetDevicesByTemplateIdAsync(Guid templateId)
        {
            return await _context.Devices.Include(x => x.Template).Include(x => x.DeviceSnaphot).Where(x => x.TemplateId == templateId).ToListAsync();
        }

        public async Task<IEnumerable<string>> ValidateDeviceIdsAsync(string[] deviceIds, bool includeDeleted = false)
        {
            var devicesRequest = deviceIds.Distinct().ToList();

            if (!devicesRequest.Any())
                return Array.Empty<string>();
            var queryBase = _context.Devices.AsQueryable();

            if (includeDeleted)
                queryBase = queryBase.IgnoreQueryFilters();

            var deviceExists = await queryBase.Where(x => deviceIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
            return devicesRequest.Except(deviceExists);
        }

        public async Task<int> GetTotalDeviceAsync()
        {
            using (var connection = GetDbConnection())
            {
                var query = "select count(1) from devices where deleted = false;";
                var count = await connection.QueryFirstOrDefaultAsync<int>(query);
                connection.Close();
                return count;
            }
        }

        public async Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string deviceId)
        {
            using (var dbConnection = GetDbConnection())
            {
                var reader = await dbConnection.QueryMultipleAsync(
                     @" select id as DeviceId, 
                                   retention_days as RetentionDays,
                                   enable_health_check as EnableHealthCheck
                            from devices 
                            where id = @deviceId;
                                           
                            select metric_key as MetricKey, 
                                   expression_compile as ExpressionCompile,
                                   data_type as DataType, 
                                   value as Value, 
                                   metric_type as MetricType
                            from v_device_metrics
                            where device_id = @deviceId;", new { deviceId = deviceId }, commandTimeout: 10000);
                var deviceInformation = await reader.ReadFirstOrDefaultAsync<DeviceInformation>();
                if (deviceInformation != null)
                    deviceInformation.Metrics = await reader.ReadAsync<DeviceMetricDataType>();

                dbConnection.Close();
                return deviceInformation;
            }
        }

        public Task RetrieveAsync(IEnumerable<Domain.Entity.Device> devices)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            return _context.Devices.AddRangeAsync(devices);
        }



        private SelectTableInfo _selectTableInfo = new SelectTableInfo("devices", "d", "id", Privileges.Device.ENTITY_NAME);
        public async Task<IEnumerable<GetDeviceDto>> GetDeviceAsync(GetDeviceByCriteria criteria)
        {
            var pagingQuery = $@"
                SELECT 
                    {_selectTableInfo.SelectEntityWildcard}.{_selectTableInfo.EntityTablePrimaryColumn} as Id
                FROM {_selectTableInfo.EntityTableName} {_selectTableInfo.SelectEntityWildcard}
                INNER JOIN device_templates dt
                    ON {_selectTableInfo.SelectEntityWildcard}.device_template_id = dt.id AND dt.deleted != true
                LEFT JOIN (
                    SELECT d.id as device_id, max(dms._ts) AS _ts,
                        CASE
                            WHEN (d.device_content ~~ '%BROKER_EMQX_COAP%'::text OR d.device_content ~~ '%BROKER_EMQX_MQTT%'::text) AND d.device_content ~~ '%""password"":""%'::text
                                THEN 'RG'
                            WHEN d.device_content ~~ '%iot.azure-devices.net%'::text
                                THEN 'RG'
                            WHEN max(dms._ts)
                                IS NOT NULL THEN 'AC'
                                ELSE 'CR'
                        END as status
                    FROM devices d
                    LEFT JOIN device_metric_snapshots dms ON dms.device_id = d.id
                    GROUP BY d.id, d.device_content
                ) dms ON dms.device_id = d.id
            ";

            var finalFilter = criteria.Filter.FromJson<SearchAndFilter>();

            var queryFilter = DapperQueryHelper.GenerateQueryByTagFilter(pagingQuery, finalFilter, _selectTableInfo, EntityIdType.entity_id_varchar);

            var cloneCriteria = JObject.FromObject(criteria).ToObject<GetDeviceByCriteria>();
            cloneCriteria.Filter = JsonConvert.SerializeObject(queryFilter.SearchAndFilter);

            var queryCriteria = cloneCriteria.ToQueryCriteria();

            var sqlBuilder = new SqlBuilder().Where($"d.deleted != true");
            (string paging, dynamic parameters) = _queryService.CompileQuery(queryFilter.PagingQuery, queryCriteria, paging: true);

            string selectQuery = SQLConstants.GET_DEVICE_SELECT;

            if (queryCriteria.HighlightedId != null)
            {
                selectQuery += @",(case
                    when d.id = @highlightedId then 0
                    else 1
                end) as highlighted";
            }

            sqlBuilder.Select(selectQuery);
            sqlBuilder = BuildGetDeviceQueryExtra(sqlBuilder, queryCriteria);

            string query = sqlBuilder.AddTemplate(SQLConstants.GET_DEVICE_SCRIPT, parameters).RawSql;
            query = PrepareGetDeviceQueryExtra(query, parameters, queryCriteria);

            //replace paging tag filter
            query = query.Replace("{{tag_pagging_query}}", paging);

            using (var connection = GetDbConnection())
            {
                var lookup = new Dictionary<string, Domain.Entity.Device>();
                var results = (await connection.QueryAsync<Domain.Entity.Device, DeviceTemplate, DeviceSnapshot, EntityTagDb, Domain.Entity.Device>(query,
                    (deviceQuery, deviceTemplate, deviceSnapshot, tag) =>
                    {
                        Domain.Entity.Device device;
                        if (!lookup.TryGetValue(deviceQuery.Id, out device))
                        {
                            device = deviceQuery;
                            device.Template = deviceTemplate;
                            device.DeviceSnaphot = deviceSnapshot;
                            device.EntityTags = new List<EntityTagDb>();
                            lookup.Add(device.Id, device);
                        }

                        if (tag != null)
                        {
                            device.EntityTags.Add(tag);
                        }
                        return device;
                    },
                    parameters as ExpandoObject, splitOn: "id, tag_id")).AsList();
                connection.Close();
                return lookup.Values.Select(GetDeviceDto.Create).ToArray();
            }
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();

        public async Task<int> CountAsync(GetDeviceByCriteria criteria)
        {
            var dbQuery = $@"SELECT COUNT({_selectTableInfo.SelectEntityWildcard}.id) 
                        FROM {_selectTableInfo.EntityTableName} {_selectTableInfo.SelectEntityWildcard}
                        INNER JOIN device_templates dt  ON {_selectTableInfo.SelectEntityWildcard}.device_template_id = dt.id AND dt.deleted != true
                        LEFT JOIN (
                            SELECT d.id as device_id, max(dms._ts) AS _ts,
                                CASE
                                    WHEN (d.device_content ~~ '%BROKER_EMQX_COAP%'::text OR d.device_content ~~ '%BROKER_EMQX_MQTT%'::text) AND d.device_content ~~ '%""password"":""%'::text
                                        THEN 'RG'
                                    WHEN d.device_content ~~ '%iot.azure-devices.net%'::text
                                        THEN 'RG'
                                    WHEN max(dms._ts)
                                        IS NOT NULL THEN 'AC'
                                        ELSE 'CR'
                                END as status
                            FROM devices d
                            LEFT JOIN device_metric_snapshots dms ON dms.device_id = d.id
                            GROUP BY d.id, d.device_content
                        ) dms ON dms.device_id = d.id
            ";
            var finalFilter = criteria.Filter.FromJson<SearchAndFilter>();
            var queryFilter = DapperQueryHelper.GenerateQueryByTagFilter(dbQuery, finalFilter, _selectTableInfo, EntityIdType.entity_id_varchar);

            var cloneCriteria = JObject.FromObject(criteria).ToObject<GetDeviceByCriteria>();
            cloneCriteria.Filter = JsonConvert.SerializeObject(queryFilter.SearchAndFilter);
            var queryCriteria = cloneCriteria.ToQueryCriteria();
            queryCriteria.Sorts = null;
            (string query, dynamic parameters) = _queryService.CompileQuery(queryFilter.PagingQuery, queryCriteria, paging: false);
            using (var connection = GetDbConnection())
            {
                var count = await connection.ExecuteScalarAsync<int>(query, parameters as ExpandoObject);
                connection.Close();
                return count;
            }
        }

        private SqlBuilder BuildGetDeviceQueryExtra(SqlBuilder sqlBuilder, GetDeviceQueryCriteria queryCriteria)
        {
            if (queryCriteria.TemplateHasBinding == true)
            {
                sqlBuilder = sqlBuilder.InnerJoin(@"(
                    select distinct device_template_id from template_bindings
                ) tb ON dt.id = tb.device_template_id");
            }

            return sqlBuilder;
        }

        private string PrepareGetDeviceQueryExtra(string query, dynamic parameters, GetDeviceQueryCriteria queryCriteria)
        {
            if (queryCriteria.HighlightedId != null)
            {
                query = query.Replace("d.highlighted_id", @"(case
                    when d.id = @highlightedId then 0
                    else 1
                end)");
                parameters.highlightedId = queryCriteria.HighlightedId;
            }

            return query;
        }
    }
}
