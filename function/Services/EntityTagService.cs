using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.SharedKernel.Extension;
using Dapper;
using Function.Extension;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AHI.Device.Function.Service
{
    public class EntityTagService : IEntityTagService
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ICache _cache;

        public EntityTagService(
            IConfiguration configuration,
            ITenantContext tenantContext,
            ICache cache)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _cache = cache;
        }

        public async Task<IEnumerable<EntityTagDto>> GetEntityIdsByTagIdsAsync(long[] tagIds)
        {
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = @$"SELECT et.entity_id_uuid as {nameof(EntityTag.EntityIdGuid)},
                                      et.entity_id_int as {nameof(EntityTag.EntityIdInt)},
                                      et.entity_id_long as {nameof(EntityTag.EntityIdLong)},
                                      et.entity_id_varchar as {nameof(EntityTag.EntityIdString)},
                                      et.entity_type as {nameof(EntityTag.EntityType)}
                            FROM entity_tags et
                            WHERE et.tag_id = ANY(@TagIds)";
                var result = await connection.QueryAsync<EntityTag>(query, new { TagIds = tagIds });
                await connection.CloseAsync();
                return result.Select(x => new EntityTagDto
                {
                    EntityId = x.EntityIdGuid?.ToString() ?? x.EntityIdInt?.ToString() ?? x.EntityIdLong?.ToString() ?? x.EntityIdString,
                    EntityType = x.EntityType
                });
            }
        }

        public async Task RemoveCachesAsync(IEnumerable<EntityTagDto> entityTags)
        {
            var tasks = entityTags.Select(
                x => x.EntityType switch
                {
                    EntityTypeConstants.ASSET => ClearAssetCacheAsync(_tenantContext),
                    _ => Task.CompletedTask
                }
            );
            await Task.WhenAll(tasks);
        }

        private Task ClearAssetCacheAsync(ITenantContext tenantContext)
        {
            string assetHashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
            return _cache.ClearHashAsync(assetHashKey);
        }
    }
}