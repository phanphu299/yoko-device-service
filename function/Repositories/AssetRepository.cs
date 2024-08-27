using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using AHI.Device.Function.Model;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.Cache.Abstraction;
using Function.Extension;
using Dapper;
using Microsoft.Extensions.Logging;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using System.Data;
using Function.Helper;

namespace AHI.Infrastructure.Repository
{
    public class AssetRepository : IAssetRepository, IReadOnlyAssetRepository
    {
        private readonly ICache _cache;
        private readonly ILogger<AssetRepository> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AssetRepository(IConfiguration configuration,
            IDbConnectionFactory dbConnectionFactory,
            ICache cache,
            ILogger<AssetRepository> logger
        )
        {
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AssetInformation> GetAssetInformationsAsync(string projectId, Guid assetId)
        {
            var assetInformationHashField = CacheKey.ASSET_INFORMATION_HASH_FIELD.GetCacheKey(assetId);
            var assetInformationHashKey = CacheKey.PROCESSING_ASSET_HASH_KEY.GetCacheKey(projectId);

            var assetInfo = await _cache.GetHashByKeyAsync<AssetInformation>(assetInformationHashKey, assetInformationHashField);

            if (assetInfo == null)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    assetInfo = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryFirstOrDefaultAsync<AssetInformation>(@"select 
                                                                                            id as AssetId, 
                                                                                            retention_days as RetentionDays 
                                                                                        from assets where id = @AssetId",
                        new
                        {
                            AssetId = assetId
                        }, commandTimeout: 300)
                    );
                }

                assetInfo = assetInfo ?? new AssetInformation();
                await _cache.SetHashByKeyAsync(assetInformationHashKey, assetInformationHashField, assetInfo);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug("GetAssetInformationsAsync - Cache hit, no change");
            }

            return assetInfo;
        }

        private IDbConnection GetDbConnection(string projectId = null) => _dbConnectionFactory.CreateConnection(projectId);
    }
}