using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Internal;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Device.Persistence.Repository
{
    public class AssetAttributeTemplatePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<Domain.Entity.AssetAttributeTemplate, Guid>, IAssetAttributeTemplateRepository, IReadAssetAttributeTemplateRepository
    {
        private readonly DeviceDbContext _context;

        public AssetAttributeTemplatePersistenceRepository(DeviceDbContext context) : base(context)
        {
            _context = context;
        }

        public LocalView<Domain.Entity.AssetAttributeTemplate> UnSaveAttributes => _context.AssetAttributeTemplates.Local;

        protected override void Update(Domain.Entity.AssetAttributeTemplate requestObject, Domain.Entity.AssetAttributeTemplate targetObject)
        {
            targetObject.Name = requestObject.Name;
            //targetObject.Expression = requestObject.Expression;
            targetObject.AttributeType = requestObject.AttributeType;
            targetObject.DataType = requestObject.DataType;
            targetObject.UpdatedUtc = DateTime.UtcNow;
            targetObject.UomId = requestObject.UomId;
        }

        public void ProcessUpdate(Domain.Entity.AssetAttributeTemplate requestObject, Domain.Entity.AssetAttributeTemplate targetObject)
        {
            Update(requestObject, targetObject);
        }

        public override Task<Domain.Entity.AssetAttributeTemplate> FindAsync(Guid id)
        {
            return AsQueryable()
                     .Include(x => x.Uom)
                     .Include(x => x.AssetAttributeDynamic).ThenInclude(x => x.AssetAttributeDynamicMappings)
                     .Include(x => x.AssetAttributeIntegration).ThenInclude(x => x.AssetAttributeIntegrationMappings)
                     .Include(x => x.AssetAttributeCommand).ThenInclude(x => x.AssetAttributeCommandMappings)
                     .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Guid>> GetAssetAttributeIdsAsync(IEnumerable<Guid> assetAttributeTemplateId)
        {
            return await _context.AssetAttributeAliasMapping.Where(x => assetAttributeTemplateId.Contains(x.AssetAttributeTemplateId)).Select(x => x.Id)
                .Union(_context.AssetAttributeDynamicMapping.Where(x => assetAttributeTemplateId.Contains(x.AssetAttributeTemplateId)).Select(x => x.Id))
                .Union(_context.AssetAttributeStaticMapping.Where(x => assetAttributeTemplateId.Contains(x.AssetAttributeTemplateId)).Select(x => x.Id))
                .Union(_context.AssetAttributeIntegrationMapping.Where(x => assetAttributeTemplateId.Contains(x.AssetAttributeTemplateId)).Select(x => x.Id))
                .Union(_context.AssetAttributeRuntimeMapping.Where(x => assetAttributeTemplateId.Contains(x.AssetAttributeTemplateId)).Select(x => x.Id)).ToListAsync();
        }

        public async Task<Domain.Entity.AssetAttributeTemplate> AddEntityAsync(Domain.Entity.AssetAttributeTemplate entity)
        {
            try
            {
                // if (entity.Id != Guid.Empty)
                //     entity.Id = Guid.NewGuid();
                await _context.AssetAttributeTemplates.AddAsync(entity);
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public Task<Domain.Entity.AssetAttributeTemplate> UpdateEntityAsync(Domain.Entity.AssetAttributeTemplate entity)
        {
            entity.UpdatedUtc = DateTime.UtcNow;
            _context.Entry(entity).State = EntityState.Modified;
            return Task.FromResult(entity);
        }

        public async Task<bool> RemoveEntityAsync(Guid id, IEnumerable<Guid> deletedAttributeIds)
        {
            var trackingEntity = await AsQueryable().Where(a => a.Id == id).FirstOrDefaultAsync();

            if (trackingEntity == null)
                throw new EntityNotFoundException();

            var runtimeAttributes = await AsQueryable()
                .Include(x => x.AssetAttributeRuntime)
                .Where(x => x.AssetTemplateId == trackingEntity.AssetTemplateId &&
                            x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME &&
                            x.AssetAttributeRuntime.TriggerAttributeId == id &&
                            !deletedAttributeIds.Contains(x.Id))
                .ToListAsync();

            foreach (var attribute in runtimeAttributes)
            {
                attribute.AssetAttributeRuntime.TriggerAttributeId = null;
            }

            _context.UpdateRange(runtimeAttributes);

            var triggers = await (from am in
                _context.AssetAttributeDynamicMapping.Where(x => x.AssetAttributeTemplateId == id).Select(x => x.Id)
                .Union(_context.AssetAttributeRuntimeMapping.Where(x => x.AssetAttributeTemplateId == id).Select(x => x.Id))
                                  join trg in _context.AssetAttributeRuntimeTriggers.Include(x => x.AssetAttributeRuntimeMapping) on am equals trg.TriggerAttributeId
                                  select trg).ToListAsync();

            var attributes = triggers.Select(x => x.AssetAttributeRuntimeMapping);

            foreach (var attribute in attributes)
            {
                attribute.IsTriggerVisibility = false;
                _context.Update(attribute);
            }

            //await ValidateRemoveAttributeAsync(id);

            _context.AssetAttributeTemplates.Remove(trackingEntity);

            return true;
        }

        public async Task<IEnumerable<Domain.Entity.AssetAttributeTemplate>> GetDependenciesInsideTemplateAsync(Guid attributeId)
        {
            return await AsQueryable()
                    .AsNoTracking()
                    .Include(x => x.AssetAttributeRuntime)
                    .Where(x => x.AssetAttributeRuntime != null
                                && x.AssetAttributeRuntime.Expression.Contains($"{{{attributeId}}}"))
                    .ToListAsync();
        }

        public async Task ValidateRemoveAttributeAsync(Guid attributeId)
        {
            var existReferenceStaticAlias = await (from aa in _context.AssetAttributeAlias
                                                   join sta in _context.AssetAttributeStaticMapping on aa.AliasAttributeId equals sta.Id
                                                   join at in _context.AssetAttributeTemplates on sta.AssetAttributeTemplateId equals at.Id
                                                   where at.Id == attributeId
                                                   select 1
                                                        ).AnyAsync();
            var existReferenceDynamicAlias = await (from aa in _context.AssetAttributeAlias
                                                    join sta in _context.AssetAttributeDynamicMapping on aa.AliasAttributeId equals sta.Id
                                                    join at in _context.AssetAttributeTemplates on sta.AssetAttributeTemplateId equals at.Id
                                                    where at.Id == attributeId
                                                    select 1
                                             ).AnyAsync();
            var existReferenceIntegrationAlias = await (from aa in _context.AssetAttributeAlias
                                                        join sta in _context.AssetAttributeIntegrationMapping on aa.AliasAttributeId equals sta.Id
                                                        join at in _context.AssetAttributeTemplates on sta.AssetAttributeTemplateId equals at.Id
                                                        where at.Id == attributeId
                                                        select 1
                                             ).AnyAsync();
            var existReferenceRuntimeAlias = await (from aa in _context.AssetAttributeAlias
                                                    join sta in _context.AssetAttributeRuntimeMapping on aa.AliasAttributeId equals sta.Id
                                                    join at in _context.AssetAttributeTemplates on sta.AssetAttributeTemplateId equals at.Id
                                                    where at.Id == attributeId
                                                    select 1
                                             ).AnyAsync();
            var existReferenceRuntimeExpression = await (from aat in _context.AssetAttributeTemplates
                                                         where aat.AssetAttributeRuntime.Expression.Contains($"{{{attributeId.ToString()}}}")
                                                         select 1
                                                 ).AnyAsync();
            var existReferenceAssetRuntime = await (from arm in _context.AssetAttributeDynamicMapping.Select(x => new { x.Id, x.AssetAttributeTemplateId, x.AssetId })
                                                                .Union(_context.AssetAttributeIntegrationMapping.Select(x => new { x.Id, x.AssetAttributeTemplateId, x.AssetId }))
                                                                .Union(_context.AssetAttributeRuntimeMapping.Select(x => new { x.Id, x.AssetAttributeTemplateId, x.AssetId }))
                                                                .Union(_context.AssetAttributeStaticMapping.Select(x => new { x.Id, x.AssetAttributeTemplateId, x.AssetId }))
                                                    join aat in _context.AssetAttributes on arm.AssetId equals aat.AssetId
                                                    where arm.AssetAttributeTemplateId == attributeId && aat.AssetAttributeRuntime.Expression.Contains("{" + arm.Id.ToString() + "}")
                                                    select 1
                                                 ).AnyAsync();

            var assetAttributeAliasMappings = from aa in _context.Assets
                                              join sta in _context.AssetAttributeAliasMapping on aa.Id equals sta.AliasAssetId
                                              join at in _context.AssetAttributeTemplates on sta.AssetAttributeTemplateId equals at.Id
                                              where at.Id == attributeId
                                              select sta;

            var existReferenceAliasMapping = await (from am in assetAttributeAliasMappings
                                                    join aam in assetAttributeAliasMappings on
                                                      new { AssetId = am.AssetId, AttributeId = am.Id } equals
                                                      new { AssetId = aam.AliasAssetId.Value, AttributeId = aam.AliasAttributeId.Value }
                                                    select 1).AnyAsync();

            var existReferenceAliasAsset = await (from am in assetAttributeAliasMappings
                                                  join aa in _context.AssetAttributeAlias on
                                                      new { AssetId = am.AssetId, AttributeId = am.Id } equals
                                                      new { AssetId = aa.AliasAssetId ?? Guid.Empty, AttributeId = aa.AliasAttributeId ?? Guid.Empty }
                                                  select 1).AnyAsync();

            var existReferenceAlias = existReferenceAliasMapping || existReferenceAliasAsset;

            if (existReferenceStaticAlias || existReferenceDynamicAlias || existReferenceIntegrationAlias || existReferenceRuntimeAlias
                || existReferenceRuntimeExpression || existReferenceAssetRuntime || existReferenceAlias)
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_ATTRIBUTE_USING);
        }

    }
}