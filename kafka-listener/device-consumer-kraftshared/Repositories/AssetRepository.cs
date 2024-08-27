using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Device.Consumer.KraftShared.Model;
using Dapper;
using Microsoft.Extensions.Logging;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using System.Data;
using Device.Consumer.KraftShared.Helpers;
using Device.Consumer.KraftShared.Constants;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Consumer.KraftShared.Repositories
{
    public class AssetRepository : IAssetRepository, IReadOnlyAssetRepository
    {
        private readonly IRedisDatabase _cache;
        private readonly ILogger<AssetRepository> _logger;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public AssetRepository(
            IDbConnectionFactory dbConnectionFactory,
            IRedisDatabase cache,
            ILogger<AssetRepository> logger
        )
        {
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
            _logger = logger;
        }
        public async Task<AssetInformation> GetAssetInformationsAsync(string projectId, Guid assetId)
        {
            var assetInformationKey = string.Format(IngestionRedisCacheKeys.AssetInfosPattern, projectId);//hash field = AssetId, hash value = asset info

            var assetInfo = await _cache.HashGetAsync<AssetInformation>(assetInformationKey, assetId.ToString());
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
                await _cache.HashSetAsync(assetInformationKey, assetId.ToString(), assetInfo);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug($"{nameof(GetAssetInformationsAsync)} - Cache hit, no change");
            }

            return assetInfo;
        }
        private IDbConnection GetDbConnection(string projectId) => _dbConnectionFactory.CreateConnection(projectId);
    }
}