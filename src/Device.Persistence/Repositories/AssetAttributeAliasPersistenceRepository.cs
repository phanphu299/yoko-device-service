using System.Linq;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using System.Data;
using Dapper;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.DbConnections;

namespace Device.Persistence.Repository
{
    public class AssetAttributeAliasPersistenceRepository : AHI.Infrastructure.Repository.Generic.GenericRepository<AssetAttributeAlias, Guid>, IAssetAttributeAliasRepository, IReadAssetAttributeAliasRepository
    {
        private readonly DeviceDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<AssetPersistenceRepository> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        public AssetAttributeAliasPersistenceRepository(DeviceDbContext context, IConfiguration configuration, ITenantContext tenantContext, ILoggerAdapter<AssetPersistenceRepository> logger, IDbConnectionFactory dbConnectionFactory) : base(context)
        {
            _dbContext = context;
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public Task<bool> ExistsAsync(Guid aliasAtributeId, Guid aliasAssetId)
        {
            //1 attribute just can mtach alias 1 other attribute
            return AsQueryable().AnyAsync(c => c.AliasAttributeId == aliasAtributeId && c.AliasAssetId == aliasAssetId);
        }

        public Task<bool> CheckExistAliasMappingAsync(Guid? aliasAtributeId, Guid aliasAssetId)
        {
            return AsQueryable().AnyAsync(c => c.AliasAttributeId == aliasAtributeId && c.AliasAssetId == aliasAssetId);
        }

        public Task<AssetAttributeAlias> GetParentIdByElementPropertyAliasId(Guid aliasAtributeId, Guid aliasAssetId)
        {
            return AsQueryable()
                 .Where(c => c.AliasAttributeId == aliasAtributeId && c.AliasAssetId == aliasAssetId).FirstOrDefaultAsync();
        }

        protected override void Update(AssetAttributeAlias requestObject, AssetAttributeAlias targetObject)
        {
            targetObject.AssetAttributeId = requestObject.AssetAttributeId;
            //targetObject.ParentId = requestObject.ParentId;
            targetObject.AliasAssetId = requestObject.AliasAssetId;
            targetObject.AliasAttributeId = requestObject.AliasAttributeId;
            targetObject.UpdatedUtc = System.DateTime.UtcNow;
        }

        public override Task<AssetAttributeAlias> FindAsync(Guid id)
        {
            return AsQueryable().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> ValidateCircleAliasAsync(Guid attributeId, Guid aliasAttributeId)
        {
            var isCircle = false;
            using (var connection = GetDbConnection())
            {
                try
                {
                    var snapshotValues = await connection.QueryAsync<Guid?>($@" select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc
                                                                                     ", new { AliasAttributeId = aliasAttributeId }, commandTimeout: 2);
                    isCircle = snapshotValues.Contains(attributeId);
                }
                catch (Npgsql.NpgsqlException exc)
                {
                    _logger.LogError(exc, exc.Message);
                    isCircle = true;
                }
                finally
                {
                    connection.Close();
                }
            }
            return isCircle;
        }

        protected IDbConnection GetDbConnection() => _dbConnectionFactory.CreateConnection();

        public Task<AssetAttributeAlias> UpdateEntityAsync(AssetAttributeAlias entity)
        {
            _dbContext.AssetAttributeAlias.Update(entity);
            return Task.FromResult(entity);
        }


        public async Task<Guid?> GetTargetAliasAttributeIdAsync(Guid aliasAttributeId)
        {
            using (var connection = GetDbConnection())
            {
                try
                {
                    var targetAliasId = await connection.QuerySingleAsync<Guid>($@"select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1", new { AliasAttributeId = aliasAttributeId }, commandTimeout: 2);
                    return targetAliasId;
                }
                catch (Npgsql.NpgsqlException exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
                finally
                {
                    connection.Close();
                }
            }
            return null;
        }
    }
}
