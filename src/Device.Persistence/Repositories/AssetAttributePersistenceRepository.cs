using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Service;
using AHI.Infrastructure.Exception;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Device.Domain.Entity;
using Device.Application.DbConnections;
using Device.Domain.ValueObject;
using Device.Application.Model;

namespace Device.Persistence.Repository
{
    public class AssetAttributePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Domain.Entity.AssetAttribute, Guid>, IAssetAttributeRepository, IReadAssetAttributeRepository
    {
        private readonly DeviceDbContext _context;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public AssetAttributePersistenceRepository(DeviceDbContext context, IDbConnectionFactory dbConnectionFactory) : base(context)
        {
            _context = context;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public LocalView<Domain.Entity.AssetAttribute> UnSaveAttributes => _context.AssetAttributes.Local;
        public LocalView<Domain.Entity.AssetAttributeRuntime> UnSaveAttributeRuntimes => _context.AssetAttributeRuntimes.Local;

        protected override void Update(Domain.Entity.AssetAttribute requestObject, Domain.Entity.AssetAttribute targetObject)
        {
            targetObject.AssetId = requestObject.AssetId;
            targetObject.Name = requestObject.Name;
            targetObject.Value = requestObject.Value;
            targetObject.AttributeType = requestObject.AttributeType;
            targetObject.DataType = requestObject.DataType;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.UomId = requestObject.UomId;
            targetObject.DecimalPlace = requestObject.DecimalPlace;
            targetObject.ThousandSeparator = requestObject.ThousandSeparator;
        }

        public void ProcessUpdate(Domain.Entity.AssetAttribute requestObject, Domain.Entity.AssetAttribute targetObject)
        {
            Update(requestObject, targetObject);
        }

        public override Task<Domain.Entity.AssetAttribute> FindAsync(Guid id)
        {
            return AsQueryable()
                .Include(x => x.Asset)
                .Include(x => x.Uom)
                .Include(x => x.AssetAttributeAlias)
                .Include(x => x.AssetAttributeDynamic)
                .Include(x => x.AssetAttributeIntegration)
                .Include(x => x.AssetAttributeCommand)
            .Where(a => a.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Domain.Entity.AssetAttribute> AddEntityAsync(Domain.Entity.AssetAttribute attribute)
        {
            try
            {
                await _context.AssetAttributes.AddAsync(attribute);
                return attribute;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public Task<Domain.Entity.AssetAttribute> UpdateEntityAsync(Domain.Entity.AssetAttribute attribute)
        {
            try
            {
                _context.Update(attribute);
                return Task.FromResult(attribute);
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public void TrackMappingEntity<TEntity>(TEntity entity, EntityState entityState = EntityState.Added) where TEntity : class
        {
            _context.Entry<TEntity>(entity).State = entityState;
        }

        public async Task<IEnumerable<ValidateAssetAttribute>> QueryAssetAttributeSeriesDataAsync(IEnumerable<Guid> attributeIds)
        {
            var sql = @$"select
                            asset_id as AssetId,
                            attribute_id as AttributeId
                        from v_asset_attributes
                        where attribute_id = ANY(@AttributeIds)";
            using (var connection = GetDbConnection())
            {
                var result = await connection.QueryAsync<ValidateAssetAttribute>(sql, new { AttributeIds = attributeIds.ToArray() });
                connection.Close();
                return result;
            }
        }

        public Task AddAssetRuntimeAttributeTriggersAsync(IEnumerable<Domain.Entity.AssetAttributeRuntimeTrigger> entities)
        {
            return _context.AssetAttributeRuntimeTriggers.AddRangeAsync(entities);
        }

        public async Task RemoveAssetRuntimeAttributeTriggersAsync(Guid assetAttributeId)
        {
            var triggers = await _context.AssetAttributeRuntimeTriggers.Where(x => x.AttributeId == assetAttributeId).ToListAsync();

            if (triggers.Any())
            {
                _context.AssetAttributeRuntimeTriggers.RemoveRange(triggers);
            }
        }

        public async Task<Guid> TrackCommandHistoryAsync(Guid assetAttributeId, string deviceId, string metricKey, Guid version, string value)
        {
            // tracking the asset history
            var commandHistory = new AssetAttributeCommandHistory();
            commandHistory.AssetAttributeId = assetAttributeId;
            commandHistory.DeviceId = deviceId;
            commandHistory.MetricKey = metricKey;
            commandHistory.RowVersion = version;
            commandHistory.Value = value;
            _context.AssetAttributeCommandHistories.Add(commandHistory);
            var newRowVersion = Guid.NewGuid();
            var attributeCommand = await _context.AssetAttributeCommands.FirstOrDefaultAsync(x => x.AssetAttributeId == assetAttributeId);
            if (attributeCommand != null)
            {
                attributeCommand.Value = value;
                attributeCommand.RowVersion = newRowVersion;
                attributeCommand.Timestamp = DateTime.UtcNow;
                _context.Entry(attributeCommand).State = EntityState.Modified;
            }
            else
            {
                var attributeCommandMapping = await _context.AssetAttributeCommandMappings.FirstOrDefaultAsync(x => x.Id == assetAttributeId);
                if (attributeCommandMapping != null)
                {
                    attributeCommandMapping.Value = value;
                    attributeCommandMapping.RowVersion = newRowVersion;
                    attributeCommandMapping.Timestamp = DateTime.UtcNow;
                    _context.Entry(attributeCommandMapping).State = EntityState.Modified;
                }
                else
                {
                    //  cannot find the target command attribute -> throws exception
                    // TODO: need to update the message code
                    throw new EntityNotFoundException("Command attribute is not found");
                }
            }
            return newRowVersion;
        }

        public async Task<IEnumerable<Domain.ValueObject.AssetDependency>> GetAssetAttributeDependencyAsync(Guid[] attributeIds)
        {
            // Querying all Asset Attribute with type RunTime - Created without template
            var runtimeAttributes = await _context.AssetAttributeRuntimes
                                            .Where(a => a.Triggers.Any(t => attributeIds.Any(att => att == t.TriggerAttributeId)))
                                            .Select(a => new AssetDependency(a.AssetAttribute.AssetId, $"{a.AssetAttribute.Asset.Name}.{a.AssetAttribute.Name}"))
                                            .ToListAsync();

            // Querying all Asset Attribute with type RunTime - Created by Asset Template
            var runtimeWithTemplateAttributes = await _context.AssetAttributeRuntimeMapping
                                            .Where(a => a.Triggers.Any(t => attributeIds.Any(att => att == t.TriggerAttributeId)))
                                            .Select(a => new AssetDependency(a.Asset.Id, $"{a.Asset.Name}.{a.AssetAttributeTemplate.Name}"))
                                            .ToListAsync();

            // Should consider to include the alias as well
            var aliasAttributes = await (from aa in _context.AssetAttributeAlias.AsQueryable()
                                             //  join at in _context.AssetAttributes.AsQueryable() on aa.AliasAttributeId equals at.Id
                                         where attributeIds.Contains(aa.AliasAttributeId ?? Guid.Empty)
                                         select new AssetDependency(aa.AssetAttribute.AssetId, $"{aa.AssetAttribute.Asset.Name}.{aa.AssetAttribute.Name}")
                                 ).ToListAsync();

            // alias with template.
            var aliasWithTemplateAttributes = await (from aa in _context.AssetAttributeAliasMapping.AsQueryable()
                                                     where aa.AliasAttributeId.HasValue && attributeIds.Contains(aa.AliasAttributeId.Value)
                                                     select new AssetDependency(aa.Asset.Id, $"{aa.Asset.Name}.{aa.AssetAttributeTemplate.Name}")
                                            ).ToListAsync();
            return runtimeAttributes
                    .Union(aliasAttributes)
                    .Union(runtimeWithTemplateAttributes)
                    .Union(aliasWithTemplateAttributes);
        }
        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();
    }
}