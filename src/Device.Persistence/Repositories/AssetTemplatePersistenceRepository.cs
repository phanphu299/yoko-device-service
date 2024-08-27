using System;
using System.Linq;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Exception;
using System.Collections.Generic;
using System.Data;
using Device.Application.DbConnections;
using Dapper;
using Device.Persistence.Constant;
using Device.Application.AssetTemplate.Command.Model;

namespace Device.Persistence.Repository
{
    public class AssetTemplatePersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<AssetTemplate, Guid>, IAssetTemplateRepository, IReadAssetTemplateRepository
    {
        private readonly DeviceDbContext _context;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public AssetTemplatePersistenceRepository(DeviceDbContext context, IDbConnectionFactory dbConnectionFactory) : base(context)
        {
            _context = context;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public override IQueryable<AssetTemplate> AsQueryable()
        {
            return base.AsQueryable()
                        .Include(x => x.EntityTags)
                        .Where(x => !x.EntityTags.Any() || x.EntityTags.Any(a => a.EntityType == EntityTypeConstants.ASSET_TEMPLATE));
        }

        public override IQueryable<AssetTemplate> AsFetchable()
        {
            return _context.AssetTemplates.AsNoTracking().Select(x => new AssetTemplate { Id = x.Id, Name = x.Name });
        }

        protected override void Update(AssetTemplate requestObject, AssetTemplate targetObject)
        {
            targetObject.Name = requestObject.Name;
            targetObject.UpdatedUtc = DateTime.UtcNow;
        }

        // public async Task ReloadAsync(AssetTemplate entity)
        // {
        //     await _context.Entry(entity).ReloadAsync();
        // }
        public override Task<AssetTemplate> FindAsync(Guid id)
        {
            return AsQueryable().AsNoTracking()
                .Include(x => x.Attributes)
                .Include(x => x.Attributes).ThenInclude(x => x.Uom)
                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeDynamic).ThenInclude(x => x.AssetAttributeDynamicMappings)
                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeRuntime).ThenInclude(x => x.AssetAttributeRuntimeMappings)
                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeIntegration).ThenInclude(x => x.AssetAttributeIntegrationMappings)
                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeCommand).ThenInclude(x => x.AssetAttributeCommandMappings)
                .Include(x => x.Attributes).ThenInclude(x => x.AssetAttributeAliasMappings)
                .Include(x => x.Assets)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<GetAssetTemplateDto> GetAssetTemplateAsync(Guid assetTemplateId)
        {
            var assetTemplateDictionary = new Dictionary<Guid, AssetTemplate>();
            List<AssetAttributeTemplate> _attribute = new List<AssetAttributeTemplate>();
            List<Asset> _asset = new List<Asset>();
            List<EntityTagDb> _entityTags = new List<EntityTagDb>();
            Type[] types = { typeof(AssetTemplate),
                typeof(AssetAttributeTemplate) ,
                typeof(Asset) ,
                typeof(AssetAttributeTemplateIntegration),
                typeof(AssetAttributeDynamicTemplate),
                typeof(AssetAttributeRuntimeTemplate),
                typeof(AssetAttributeCommandTemplate),
                typeof(EntityTagDb)
            };

            Func<object[], AssetTemplate> map = objects =>
            {
                var assetTemplate = objects[0] as AssetTemplate;
                var attribute = objects[1] as AssetAttributeTemplate;
                var asset = objects[2] as Asset;
                var attributeIntegration = objects[3] as AssetAttributeTemplateIntegration;
                var attributeDynamic = objects[4] as AssetAttributeDynamicTemplate;
                var attributeRuntime = objects[5] as AssetAttributeRuntimeTemplate;
                var attributeCommand = objects[6] as AssetAttributeCommandTemplate;
                var entityTag = objects[7] as EntityTagDb;

                AssetTemplate _assetTemplate;

                if (!assetTemplateDictionary.TryGetValue(assetTemplate.Id, out _assetTemplate))
                {
                    _assetTemplate = assetTemplate;
                    assetTemplateDictionary.Add(assetTemplate.Id, _assetTemplate);
                    _attribute = new List<AssetAttributeTemplate>();
                    _asset = new List<Asset>();
                    _entityTags = new List<EntityTagDb>();
                }

                if (attribute != null && !_assetTemplate.Attributes.Select(x => x.Id).Contains(attribute.Id))
                {
                    attribute.AssetAttributeIntegration = attributeIntegration;
                    attribute.AssetAttributeDynamic = attributeDynamic;
                    attribute.AssetAttributeRuntime = attributeRuntime;
                    attribute.AssetAttributeCommand = attributeCommand;
                    _attribute.Add(attribute);
                    _assetTemplate.Attributes = _attribute;
                }

                if (asset != null && !_assetTemplate.Assets.Select(x => x.Id).Contains(asset.Id))
                {
                    _asset.Add(asset);
                    _assetTemplate.Assets = _asset;
                }

                if (entityTag != null)
                {
                    _entityTags.Add(entityTag);
                    _assetTemplate.EntityTags = _entityTags;
                }

                return _assetTemplate;
            };

            var assetTemplates = await GetDbConnection().QueryAsync(
                SQLConstants.GET_ASSET_TEMPLATE_SCRIPT,
                types,
                map,
                new { assetTemplateId }, splitOn: "id, id, id, id, id, id , entityIdGuid");

            return GetAssetTemplateDto.Create(assetTemplates.FirstOrDefault());
        }

        public async Task<bool> RemoveEntityAsync(AssetTemplate entity)
        {
            var trackingEntity = await AsQueryable().Where(a => a.Id == entity.Id).FirstOrDefaultAsync();

            if (trackingEntity == null)
                throw new EntityNotFoundException(detailCode: ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);

            if (await _context.AssetAttributeAlias.Where(x => x.AliasAssetId == entity.Id).AnyAsync())
                throw new EntityInvalidException(detailCode: MessageConstants.ASSET_TEMPLATE_USING);

            _context.AssetTemplates.Remove(trackingEntity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AssetTemplate> UpdateEntityAsync(AssetTemplate entity)
        {
            try
            {
                var trackingEntity = await AsQueryable().Where(a => a.Id == entity.Id).FirstOrDefaultAsync();
                if (trackingEntity == null)
                    throw new EntityNotFoundException();

                Update(entity, trackingEntity);
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public async Task<AssetTemplate> AddEntityAsync(AssetTemplate entity)
        {
            try
            {
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.DATABASE_QUERY_FAILED, innerException: ex);
            }
        }

        public Task RetrieveAsync(IEnumerable<AssetTemplate> templates)
        {
            _context.Database.SetCommandTimeout(RetrieveConstants.TIME_OUT);
            return _context.AssetTemplates.AddRangeAsync(templates);
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();
    }
}
