using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service.Dapper.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Dapper;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.DbConnections;
using Device.Application.Helper;
using Device.Application.Repository;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;

namespace Device.Persistence.Repository
{
    public class AssetPersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Asset, Guid>, IAssetRepository, IReadAssetRepository
    {
        private readonly DeviceDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<AssetPersistenceRepository> _logger;
        private readonly ICache _cache;
        private readonly ISecurityContext _securityContext;
        private readonly IQueryService _queryService;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IEntityTagRepository<EntityTagDb> _entityTagRepository;

        private readonly string[] _retrieveTables = {
            "assets","asset_attributes","asset_attribute_alias","asset_attribute_alias_mapping","asset_attribute_commands"
            ,"asset_attribute_command_mapping","asset_attribute_dynamic","asset_attribute_dynamic_mapping","asset_attribute_integration"
            ,"asset_attribute_integration_mapping","asset_attribute_runtimes","asset_attribute_runtime_triggers","asset_attribute_runtime_mapping"
            ,"asset_attribute_static_mapping"
        };

        public AssetPersistenceRepository(DeviceDbContext context,
            ITenantContext tenantContext,
            ILoggerAdapter<AssetPersistenceRepository> logger,
            ICache cache,
            ISecurityContext securityContext,
            IEntityTagRepository<EntityTagDb> entityTagRepository,
            IQueryService queryService,
            IDbConnectionFactory dbConnectionFactory) : base(context)
        {
            _queryService = queryService;
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
            _cache = cache;
            _securityContext = securityContext;
            _dbConnectionFactory = dbConnectionFactory;
            _entityTagRepository = entityTagRepository;
        }

        public IQueryable<Asset> Assets => _context.Assets;
        public LocalView<Asset> UnSaveAssets => _context.Assets.Local;

        protected override void Update(Asset requestObject, Asset targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.ParentAssetId = requestObject.ParentAssetId;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.AssetTemplateId = requestObject.AssetTemplateId;
        }

        public IQueryable<Asset> AssetTemplateAsQueryable()
        {
            return base.AsQueryable()
                .Include(x => x.AssetTemplate);
        }

        public IQueryable<Asset> OnlyAssetAsQueryable()
        {
            return base.AsQueryable();
        }

        public override IQueryable<Asset> AsFetchable()
        {
            return Assets.AsNoTracking().Select(x => new Asset { Id = x.Id, Name = x.Name, ResourcePath = x.ResourcePath, CreatedBy = x.CreatedBy });
        }

        public async Task<Asset> FindFullAssetByIdAsync(Guid id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
                return null;

            List<Asset> childAssets;
            using (var connection = GetDbConnection())
            {
                var assetQuery = SelectAssetFull(
                    assetWarningArgs: "null, null, @startsWithResourcePath",
                    parentAssetJoinFilter: "and pa.resource_path like concat(@startsWithResourcePath, '%')",
                    assetFilter: "where a.resource_path like concat(@startsWithResourcePath, '%') and a.id != @excludedId"
                );
                var args = new
                {
                    startsWithResourcePath = asset.ResourcePath,
                    excludedId = id
                };
                childAssets = (await connection.QueryAsync<Asset, AssetWarning, AssetTemplate, Asset, AssetAttribute, EntityTagDb, Asset>(
                    assetQuery, MapAssetFullQueryResult, args)
                ).AsList();
                connection.Close();
            }

            // load asset warning
            AssetWarning assetWarning = (await FindAssetWarningAsync(new List<Guid>() { id })).FirstOrDefault();
            asset.AssetWarning = assetWarning;

            //loading some of the part depend on the type of asset.
            foreach (var childAsset in childAssets)
            {
                var assetEntry = _context.Entry(childAsset);
                if (assetEntry.State == EntityState.Detached)
                    _context.Attach(childAsset);

                // load the template
                if (childAsset.AssetTemplateId != null)
                {
                    await LoadAssetTemplateAsync(childAsset);
                }

                if (childAsset.Attributes.Any())
                {
                    // load attribute detail
                    await LoadAssetAttributeAsync(childAsset);
                }

                // az: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_queries/edit/12856
                foreach (var aliasMapping in childAsset.AssetAttributeAliasMappings)
                {
                    await PopulateAliasMappingAsync(aliasMapping);
                }

                // load the real alias asset
                foreach (var aliasAttribute in childAsset.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
                {
                    await LoadAssetAttributeAliasAsync(aliasAttribute);
                }
            }

            return asset;
        }

        // [NOTE] This one uses Dapper for performance optimization.
        // In the future we should apply this approach to FindAsync instead.
        public async Task<Asset> FindByIdAsync(Guid id)
        {
            Asset assetEntity;
            using (var connection = GetDbConnection())
            {
                var assetQuery = SelectAssetFull(
                    assetWarningArgs: "array[@assetId], null, null",
                    assetFilter: "where a.id = @assetId"
                );
                var args = new
                {
                    assetId = id
                };
                var assets = (await connection.QueryAsync<Asset, AssetWarning, AssetTemplate, Asset, AssetAttribute, EntityTagDb, Asset>(
                    assetQuery, MapAssetFullQueryResult, args)
                ).AsList();
                assetEntity = assets.FirstOrDefault();
                if (assetEntity != null)
                {
                    assetEntity.EntityTags = assets.SelectMany(x => x.EntityTags).GroupBy(x => x.TagId).Select(x => x.First()).ToList();
                }
                connection.Close();
            }

            if (assetEntity == null)
                return null;

            var assetEntry = _context.Entry(assetEntity);
            if (assetEntry.State == EntityState.Detached)
                _context.Attach(assetEntity);

            //loading some of the part depend on the type of asset.
            if (assetEntity.AssetTemplateId != null)
            {
                // load the template
                await LoadAssetTemplateAsync(assetEntity);
            }
            if (assetEntity.Attributes.Any())
            {
                // load attribute detail
                await LoadAssetAttributeAsync(assetEntity);
            }

            // az: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_queries/edit/12856
            foreach (var aliasMapping in assetEntity.AssetAttributeAliasMappings)
            {
                await PopulateAliasMappingAsync(aliasMapping);
            }

            // load the real alias asset
            foreach (var aliasAttribute in assetEntity.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                await LoadAssetAttributeAliasAsync(aliasAttribute);
            }

            //assetEntity.EntityTags = (await _entityTagRepository.GetEntityTagsAsync(assetEntity.Id, EntityTypeConstants.ASSET)).ToList();
            return assetEntity;
        }

        private string SelectAssetFull(
            string assetWarningArgs = "null, null, null",
            string parentAssetJoinFilter = "",
            string assetFilter = ""
        )
        {
            // 'null as id' is for simple Dapper splitOn without the need to specify it directly
            return $@"
            select
                a.*,
                null::int as id,
                aw.asset_id,
                aw.asset_id is not null as has_warning,
                at2.*,
                pa.*,
                aa.*,
                et.*
            from assets a
            left join find_asset_warning({assetWarningArgs}) aw ON a.id = aw.asset_id
            left join asset_templates at2 on a.asset_template_id = at2.id
            left join assets pa on a.parent_asset_id = pa.id {parentAssetJoinFilter}
            left join asset_attributes aa on a.id = aa.asset_id
            left join entity_tags et on (et.entity_id_uuid = a.id and et.entity_type = '{EntityTypeConstants.ASSET}')
            {assetFilter};";
        }

        private Asset MapAssetFullQueryResult(Asset asset, AssetWarning assetWarning, AssetTemplate assetTemplate, Asset parent, AssetAttribute assetAttribute, EntityTagDb entityTag)
        {
            asset.AssetWarning = new AssetWarning
            {
                AssetId = asset.Id,
                Asset = asset,
                HasWarning = assetWarning?.HasWarning == true
            };
            asset.AssetTemplate = assetTemplate;
            asset.ParentAsset = parent;
            asset.Attributes ??= new List<AssetAttribute>();
            if (assetAttribute != null)
                asset.Attributes.Add(assetAttribute);

            asset.EntityTags ??= new List<EntityTagDb>();
            if (entityTag != null)
                asset.EntityTags.Add(entityTag);
            return asset;
        }

        public async Task<IEnumerable<GetAssetSimpleDto>> GetAssetsByTemplateIdAsync(Guid assetTemplateId)
        {
            List<Asset> response = await AssetTemplateAsQueryable().AsNoTracking().Where(x => x.AssetTemplateId == assetTemplateId).ToListAsync();

            // load asset warning
            List<AssetWarning> assetWarnings = (await FindAssetWarningAsync(response.Select(r => r.Id))).ToList();

            foreach (Asset asset in response)
            {
                asset.AssetWarning = assetWarnings.FirstOrDefault(a => a.AssetId == asset.Id);
            }

            return response.Select(GetAssetSimpleDto.Create);
        }

        public override async Task<Asset> FindAsync(Guid id)
        {
            var assetEntity = await _context.Assets
                                    .Include(x => x.Attributes)
                                    .Include(x => x.ParentAsset)
                                    .Include(x => x.Children)
                                    .Include(x => x.EntityTags)
                                    .Where(x => !x.EntityTags.Any() || x.EntityTags.Any(a => a.EntityType == EntityTypeConstants.ASSET))
                            .FirstOrDefaultAsync(x => x.Id == id);
            if (assetEntity == null)
            {
                return null;
            }

            // load asset warning
            AssetWarning assetWarning = (await FindAssetWarningAsync(new List<Guid>() { id })).FirstOrDefault();
            assetEntity.AssetWarning = assetWarning;

            //loading some of the part depend on the type of asset.
            if (assetEntity.AssetTemplateId != null)
            {
                // load the template
                await LoadAssetTemplateAsync(assetEntity);
            }
            if (assetEntity.Attributes.Any())
            {
                // load attribute detail
                await LoadAssetAttributeAsync(assetEntity);
            }

            // az: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_queries/edit/12856
            foreach (var aliasMapping in assetEntity.AssetAttributeAliasMappings)
            {
                await PopulateAliasMappingAsync(aliasMapping);
            }

            // load the real alias asset
            foreach (var aliasAttribute in assetEntity.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                await LoadAssetAttributeAliasAsync(aliasAttribute);
            }
            return assetEntity;
        }

        public async Task<Asset> FindSnapshotAsync(Guid id)
        {
            var assetEntity = await _context.Assets
                                    .Include(x => x.Attributes)
                                    .FirstOrDefaultAsync(x => x.Id == id);
            if (assetEntity == null)
            {
                return null;
            }

            //loading some of the part depend on the type of asset.
            if (assetEntity.AssetTemplateId != null)
            {
                // load the template
                await LoadAssetTemplateAsync(assetEntity);
            }

            if (assetEntity.Attributes.Any())
            {
                // load attribute detail
                await LoadAssetAttributeAsync(assetEntity);
            }

            // load the real alias asset
            foreach (var aliasAttribute in assetEntity.Attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                await LoadAssetAttributeAliasAsync(aliasAttribute);
            }

            return assetEntity;
        }

        private async Task PopulateAliasMappingAsync(AssetAttributeAliasMapping aliasMapping)
        {
            var aliasAttributeId = aliasMapping.AliasAttributeId;
            if (!aliasAttributeId.HasValue)
            {
                return;
            }
            var targetAliasAttribute = await FindTargetAttributeAsync(aliasMapping.Id);
            if (targetAliasAttribute != null)
            {
                aliasMapping.AliasAssetName = targetAliasAttribute.AliasAssetName;
                aliasMapping.AliasAttributeName = targetAliasAttribute.AliasAttributeName;
                aliasMapping.DataType = targetAliasAttribute.DataType;
                aliasMapping.UomId = targetAliasAttribute.UomId;
                aliasMapping.DecimalPlace = targetAliasAttribute.DecimalPlace;
                aliasMapping.ThousandSeparator = targetAliasAttribute.ThousandSeparator;
            }
            else
            {
                Guid aliasAssetId = aliasMapping.AliasAssetId ?? Guid.Empty;
                var targetAsset = await FindAsync(aliasAssetId);
                if (targetAsset != null)
                {
                    aliasMapping.AliasAssetName = targetAsset.Name;
                }
            }
        }

        private async Task LoadAssetAttributeAliasAsync(AssetAttribute aliasAttribute)
        {
            if (aliasAttribute.AssetAttributeAlias == null)
            {
                // this is incomplete attribute. can be the reference has been deleted
                return;
            }
            // az: https://dev.azure.com/thanhtrungbui/yokogawa-ppm/_workitems/edit/16375
            var targetAttribute = await FindTargetAttributeAsync(aliasAttribute.Id);
            if (targetAttribute != null)
            {
                if (aliasAttribute.UomId != targetAttribute.UomId)
                {
                    aliasAttribute.Uom = targetAttribute.Uom;
                    aliasAttribute.UomId = targetAttribute.UomId;
                }
                aliasAttribute.DataType = targetAttribute.DataType;
                aliasAttribute.DecimalPlace = targetAttribute.DecimalPlace;
                aliasAttribute.ThousandSeparator = targetAttribute.ThousandSeparator;
                aliasAttribute.AssetAttributeAlias.AliasAssetName = targetAttribute.AliasAssetName;
                aliasAttribute.AssetAttributeAlias.AliasAttributeName = targetAttribute.AliasAttributeName;
            }
            else
            {
                Guid? aliasAssetId = aliasAttribute.AssetAttributeAlias.AliasAssetId;
                var targetAsset = await FindAsync(aliasAssetId ?? Guid.Empty);
                if (targetAsset != null)
                {
                    aliasAttribute.AssetAttributeAlias.AliasAssetName = targetAsset.Name;
                }
            }
        }

        private async Task LoadAssetAttributeAsync(Asset assetEntity)
        {
            await _context.Entry(assetEntity).Collection(x => x.Attributes).Query()
                     .Include(x => x.Uom)
                     .Include(x => x.AssetAttributeDynamic)
                     .Include(x => x.AssetAttributeIntegration)
                     .Include(x => x.AssetAttributeCommand)
                     .Include(x => x.AssetAttributeRuntime).ThenInclude(x => x.Triggers)
                     .Include(x => x.AssetAttributeAlias).LoadAsync();
        }

        private async Task LoadAssetTemplateAsync(Asset assetEntity)
        {
            var assetEntry = _context.Entry(assetEntity);
            await assetEntry.Navigation("AssetTemplate").LoadAsync();
            var templateEntry = _context.Entry(assetEntity.AssetTemplate);
            await templateEntry.Collection(x => x.Attributes).LoadAsync();
            var templateAttributesQuery = templateEntry.Collection(x => x.Attributes).Query();
            await templateAttributesQuery.Include(x => x.AssetAttributeDynamic).ThenInclude(x => x.AssetAttributeDynamicMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeIntegration).ThenInclude(x => x.AssetAttributeIntegrationMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeRuntime).ThenInclude(x => x.AssetAttributeRuntimeMappings).ThenInclude(x => x.Triggers).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeCommand).ThenInclude(x => x.AssetAttributeCommandMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeStaticMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeDynamicMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeIntegrationMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeCommandMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeAliasMappings).LoadAsync();
            await templateAttributesQuery.Include(x => x.Uom).LoadAsync();
            await templateAttributesQuery.Include(x => x.AssetAttributeRuntimeMappings).LoadAsync();
        }

        public async Task<Asset> AddEntityAsync(Asset entity)
        {
            await _context.Assets.AddAsync(entity);
            return entity;
        }

        public Task<Asset> UpdateEntityAsync(Asset entity)
        {
            _context.Assets.Update(entity);
            return Task.FromResult(entity);
        }

        public async Task<bool> RemoveEntityAsync(Asset entity)
        {
            //now just delete list for template asset => not deep element deleted as tree delete
            Asset trackingEntity = await OnlyAssetAsQueryable().Include(x => x.Children)
                                 .Include(x => x.Attributes)
                                 .Where(a => a.Id == entity.Id).FirstOrDefaultAsync();

            if (trackingEntity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

            if (trackingEntity.Children != null && trackingEntity.Children.Any())
                throw new EntityInvalidException(ExceptionErrorCode.ERROR_ENTITY_VALIDATION, detailCode: MessageConstants.ASSET_HAS_CHILD_ASSET);

            _context.Assets.Remove(trackingEntity);
            return true;
        }

        public async Task<AliasAttributeReference> FindTargetAttributeAsync(Guid attributeId)
        {
            var start = DateTime.UtcNow;
            var rootAttributeIdHashField = CacheKey.ALIAS_REFERENCE_ID_HASH_FIELD.GetCacheKey(attributeId);
            var rootAttributeIdHashKey = CacheKey.ALIAS_REFERENCE_ID_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

            var rootAttributeId = await _cache.GetHashByKeyInStringAsync(rootAttributeIdHashKey, rootAttributeIdHashField);

            using (var dbConnection = GetDbConnection())
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                if (rootAttributeId == null)
                {
                    var aliasReferGuid = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QuerySingleOrDefaultAsync<Guid?>("select attribute_id from find_root_alias_asset_attribute(@attributeId) order by alias_level desc limit 1", new { attributeId = attributeId })
                    );
                    if (aliasReferGuid == null)
                    {
                        // cannot find the target alias -> return null
                        return null;
                    }
                    else
                    {
                        rootAttributeId = aliasReferGuid.ToString();
                        await _cache.SetHashByKeyAsync(rootAttributeIdHashKey, rootAttributeIdHashField, rootAttributeId);
                    }
                }

                var rootAttributeHashField = CacheKey.ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_FIELD.GetCacheKey(rootAttributeId, attributeId);
                var rootAttributeKeyHashKey = CacheKey.ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

                var attributeReference = await _cache.GetHashByKeyAsync<AliasAttributeReference>(rootAttributeKeyHashKey, rootAttributeHashField);

                if (attributeReference == null)
                {
                    var queryResult = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryMultipleAsync(@$"
                                                                                        select
                                                                                            aa.attribute_id as TargetAttributeId ,
                                                                                            aa.asset_id as TargetAssetId,
                                                                                            @AttributeId as AttributeId,
                                                                                            aa.asset_id as AssetId,
                                                                                            aa.data_type as DataType,
                                                                                            aa.attribute_type as AttributeType,
                                                                                            s.device_id as DeviceId,
                                                                                            s.metric_key as MetricKey,
                                                                                            aa.uom_id as UomId,
                                                                                            aa.decimal_place as DecimalPlace,
                                                                                            aa.thousand_separator as ThousandSeparator
                                                                                        from v_asset_attributes aa
                                                                                        left join (

                                                                                                select aad.asset_attribute_id as attribute_id, sn.device_id as device_id, sn.metric_key as metric_key
                                                                                                from asset_attribute_dynamic aad
                                                                                                inner join asset_attributes aa on aad.asset_attribute_id = aa.id
                                                                                                inner join v_device_metrics sn on aad.device_id = sn.device_id and aad.metric_key = sn.metric_key

                                                                                                union
                                                                                                select  am.id as attribute_id, sn.device_id as device_id, sn.metric_key as metric_key
                                                                                                from asset_attribute_dynamic_mapping am
                                                                                                inner join asset_attribute_templates aat on am.asset_attribute_template_id  = aat.id
                                                                                                inner join v_device_metrics sn on  am.device_id  = sn.device_id and am.metric_key  = sn.metric_key

                                                                                        ) s on aa.attribute_id = s.attribute_id
                                                                                        where aa.attribute_id = @Id;

                                                                                        -- uom
                                                                                        select  u.id as Id
                                                                                                , u.name as Name
                                                                                                , u.abbreviation as Abbreviation
                                                                                        from v_asset_attributes aa
                                                                                        inner join uoms u on aa.uom_id = u.id
                                                                                        where aa.attribute_id = @Id;

                                                                                        --- alias information
                                                                                        select a.name, vaa.attribute_name
                                                                                        from v_asset_attribute_alias vaaa
                                                                                        inner join v_asset_attributes vaa on vaaa.alias_attribute_id  = vaa.attribute_id
                                                                                        inner join assets a on vaa.asset_id  = a.id
                                                                                        where vaaa.attribute_id  = @AttributeId
                                                                                        "

                        , new { Id = string.IsNullOrWhiteSpace(rootAttributeId) ? Guid.Empty : Guid.Parse(rootAttributeId), AttributeId = attributeId })
                    );
                    attributeReference = await queryResult.ReadFirstOrDefaultAsync<AliasAttributeReference>();
                    var uom = await queryResult.ReadFirstOrDefaultAsync<Uom>();
                    var (assetName, attributeName) = await queryResult.ReadFirstOrDefaultAsync<(string, string)>();
                    dbConnection.Close();
                    if (attributeReference != null)
                    {
                        attributeReference.AliasAssetName = assetName;
                        attributeReference.AliasAttributeName = attributeName;
                    }

                    await _cache.SetHashByKeyAsync(rootAttributeKeyHashKey, rootAttributeHashField, attributeReference);
                }

                _logger.LogTrace($"Query take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                return attributeReference;
            }
        }

        public async Task<IEnumerable<Guid>> GetAllRelatedAssetIdAsync(Guid assetId)
        {
            var list = new List<Guid> { assetId };
            using (var connection = GetDbConnection())
            {
                var ids = await connection.QueryAsync<Guid>($"select parent_id from fnc_sel_parent_element('{assetId}') where parent_id is not null");
                connection.Close();
                list.AddRange(ids);
            }
            return list;
        }

        public async Task<IEnumerable<Application.Asset.Command.Model.GetAssetSimpleDto>> GetAssetSimpleAsync(
            Application.Asset.Command.GetAssetByCriteria criteria, bool paging)
        {
            using (var connection = GetDbConnection())
            {
                var queryCriteria = criteria.ToQueryCriteria();
                (string query, dynamic parameters) = _queryService.CompileQuery(
                    $@"SELECT * FROM ({SelectAssetSimple()}) a",
                    queryCriteria, paging);
                parameters.assetIds = null;
                parameters.assetName = null;
                parameters.startsWithResourcePath = null;
                var results = (await connection.QueryAsync<Application.Asset.Command.Model.GetAssetSimpleDto>(query, parameters as ExpandoObject)).AsList();
                connection.Close();
                return results;
            }
        }

        public async Task<int> CountAsync(Application.Asset.Command.GetAssetByCriteria criteria)
        {
            using (var connection = GetDbConnection())
            {
                var queryCriteria = criteria.ToQueryCriteria();
                queryCriteria.Sorts = null;
                (string query, dynamic parameters) = _queryService.CompileQuery(
                    $@"SELECT COUNT(*) FROM ({SelectAssetSimple()}) a",
                    queryCriteria, paging: false);
                parameters.assetIds = null;
                parameters.assetName = null;
                parameters.startsWithResourcePath = null;
                var count = await connection.ExecuteScalarAsync<int>(query, parameters as ExpandoObject);
                connection.Close();
                return count;
            }
        }

        private string SelectAssetSimple()
        {
            return $@"
            SELECT
                a.id as ""id"",
                a.name as ""name"",
                a.retention_days as ""retentionDays"",
                a.parent_asset_id as ""parentAssetId"",
                a.asset_template_id as ""assetTemplateId"",
                a.created_utc as ""createdUtc"",
                a.updated_utc as ""updatedUtc"",
                aw.asset_id is not null as ""hasWarning"",
                a.resource_path as ""resourcePath"",
                a.created_by as ""createdBy"",
                a.is_document as ""isDocument"",
                at.name as ""assetTemplateName""
            FROM assets a
            LEFT JOIN find_asset_warning(@assetIds, @assetName, @startsWithResourcePath) aw ON a.id = aw.asset_id
            LEFT JOIN asset_templates at ON a.asset_template_id = at.id";
        }

        public async Task<IEnumerable<AssetHierarchy>> HierarchySearchAsync(string assetName, IEnumerable<long> tagIds)
        {
            using (var connection = GetDbConnection())
            {
                var dictionary = new Dictionary<Guid, AssetHierarchy>();
                var sql = @"select
                            f.id as AssetId,
                            name as AssetName,
                            has_warning as AssetHasWarning,
                            created_utc as AssetCreatedUtc,
                            asset_template_id as AssetTemplateId,
                            retention_days as AssetRetentionDays,
                            parent_asset_id as ParentAssetId,
                            is_found_result as IsFoundResult
                        from find_asset_hierarchy_by_asset_name(@assetName, @tagIds) f";

                var where = "where 1=1 ";
                if (!_securityContext.FullAccess)
                {
                    if (_securityContext.RestrictedIds != null && _securityContext.RestrictedIds.Any())
                    {
                        sql = $@"{sql}
                        left join unnest(ARRAY['{string.Join("','", _securityContext.RestrictedIds)}']) r  on f.resource_path not ilike concat('%', r , '%')";
                        where = $"{where} and r is not null ";
                    }

                    if (_securityContext.AllowedIds != null && _securityContext.AllowedIds.Any())
                    {
                        sql = $@"{sql}
                        left join unnest(ARRAY['{string.Join("','", _securityContext.AllowedIds)}']) a on f.resource_path ilike concat('%', a, '%')";
                        where = $"{where} and (a is not null or created_by = @createdBy)";
                    }
                    else
                    {
                        where = $"{where} and created_by = @createdBy";
                    }
                }

                sql = $@"{sql}
                            {where}";

                var assets = await connection.QueryAsync<AssetHierarchy>(sql, param: new
                {
                    TagIds = tagIds.ToList(),
                    AssetName = assetName?.Trim().Replace("_", "\\_").Replace("%", "\\%").Replace("[", "\\["),
                    CreatedBy = _securityContext.Upn
                });

                var foundIds = assets.Where(x => x.IsFoundResult).Select(x => x.AssetId);
                AssetHierarchy lastAsset = null;
                var foundAssets = new List<AssetHierarchy>();
                foreach (var asset in assets)
                {
                    if (asset.IsFoundResult)
                    {
                        lastAsset = asset;
                        lastAsset.Hierarchy = new List<Hierarchy>
                        {
                            Hierarchy.From(lastAsset)
                        };
                        foundAssets.Add(lastAsset);
                        continue;
                    }

                    if (foundIds.Contains(asset.AssetId))
                        asset.IsFoundResult = true;

                    if (lastAsset == null)
                        continue;

                    var hierarchy = lastAsset.Hierarchy as List<Hierarchy>;
                    var parentOfIdx = hierarchy.FindIndex(a => asset.AssetId == a.ParentAssetId);
                    hierarchy.Insert(parentOfIdx > -1 ? parentOfIdx : 0, Hierarchy.From(asset));
                }
                return foundAssets;
            }
        }

        public async Task<IEnumerable<AssetPath>> GetPathsAsync(IEnumerable<Guid> assetIds)
        {
            using (var connection = GetDbConnection())
            {
                // find_root_asset_v1 requires the write connection
                var result = await connection.QueryAsync<AssetPath>(@$"select
                                                                        id_output as id,
                                                                        asset_path_id_output as AssetPathId,
                                                                        asset_path_name_output as AssetPathName
                                                                    from find_root_asset_v1(@assetIds)",
                                                                    new { assetIds = assetIds.ToArray() });
                connection.Close();
                return result;
            }
        }

        public async Task<bool> ValidateAssetAsync(Guid id)
        {
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryFirstAsync<bool>("select coalesce((select 1 from assets where id = @Id), 0);", new { Id = id });
                connection.Close();
                return result;
            }
        }

        public Task<bool> ValidateParentExistedAsync(Guid parentAssetId)
        {
            return _context.Assets.AsQueryable().AnyAsync(x => x.Id == parentAssetId);
        }

        public async Task<IEnumerable<Guid>> GetExistingAssetIdsAsync(IEnumerable<Guid> ids)
        {
            return await _context.Assets.AsQueryable().AsNoTracking().Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        }

        public async Task<IEnumerable<Guid>> GetAllRelatedChildAssetIdAsync(Guid assetId)
        {
            var list = new List<Guid> { assetId };
            using (var connection = GetDbConnection())
            {
                var ids = await connection.QueryAsync<Guid>($"select id from fnc_sel_child_element(@AssetId) where child_level> 0", new { AssetId = assetId });
                connection.Close();
                list.AddRange(ids);
            }
            return list;
        }

        public async Task<int> GetTotalAssetAsync()
        {
            using (var connection = GetDbConnection())
            {
                var query = "select count(*) from assets";
                var count = await connection.QueryFirstOrDefaultAsync<int>(query);
                connection.Close();
                return count;
            }
        }

        public async Task UpdateAssetPathAsync(Guid id)
        {
            using (var connection = GetDbConnection())
            {
                var query = $"call fnc_udp_update_asset_resource_path('{id}')";
                _ = await connection.ExecuteScalarAsync(query);
                connection.Close();
            }
        }

        public async Task RetrieveAsync(IEnumerable<Asset> assets)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            StringBuilder builder = new StringBuilder();

            foreach (var table in _retrieveTables)
            {
                builder.Append($"ALTER TABLE {table} DISABLE TRIGGER ALL;");
            }

            string triggerSql = builder.ToString();
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
            await _context.Assets.AddRangeAsync(assets);
            await _context.SaveChangesAsync();

            triggerSql = triggerSql.Replace("DISABLE", "ENABLE");
            await _context.Database.ExecuteSqlRawAsync(triggerSql);
        }

        private async Task<IEnumerable<AssetWarning>> FindAssetWarningAsync(IEnumerable<Guid> assetIds)
        {
            using (var connection = GetDbConnection())
            {
                string assetName = null;
                string startsWithResourcePath = null;
                var result = (await connection.QueryAsync<AssetWarning>(@$"select asset_id as AssetId, true as HasWarning
                                                                    from find_asset_warning(@assetIds, @assetName, @startsWithResourcePath)",
                                                                    new { assetIds = assetIds.ToArray(), assetName, startsWithResourcePath }))
                            .ToList();
                connection.Close();

                foreach (Guid assetId in assetIds)
                {
                    if (!result.Exists(r => r.AssetId == assetId))
                    {
                        result.Add(new AssetWarning()
                        {
                            AssetId = assetId,
                            HasWarning = false
                        });
                    }
                }

                return result;
            }
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();
    }
}
