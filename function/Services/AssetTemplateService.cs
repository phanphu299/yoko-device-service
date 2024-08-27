using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using Dapper;
using Function.Event;
using System.Linq;
using AHI.Infrastructure.Bus.ServiceBus.Enum;
using System;
using Function.Helper.DbHelper;

namespace AHI.Device.Function.Service
{
    public class AssetTemplateService : IAssetTemplateService
    {
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<AssetTemplateService> _logger;
        private readonly IDomainEventDispatcher _dispatcher;
        private readonly IConfiguration _configuration;

        public AssetTemplateService(
            ITenantContext tenantContext,
            ILoggerAdapter<AssetTemplateService> logger,
            IConfiguration configuration,
            IDomainEventDispatcher dispatcher
        )
        {
            _tenantContext = tenantContext;
            _logger = logger;
            _configuration = configuration;
            _dispatcher = dispatcher;
        }

        public async Task ProcessChangeAsync(AssetTemplateMessage message)
        {

            using (var connection = DbHelper.GetDbConnection(_configuration, _tenantContext))
            {
                await connection.OpenAsync();

                try
                {
                    var query = @$" SELECT a.id FROM assets a WHERE a.asset_template_id = @AssetTemplateId";
                    var assets = await connection.QueryAsync<Guid>(query, new { AssetTemplateId = message.Id });
                    if (assets.Any())
                    {
                        var assetEvents = assets.Select(x => new AssetAttributeChangedEvent(x, 0, _tenantContext, ActionTypeEnum.Updated, forceReload: true)).ToList();
                        await _dispatcher.SendAsync(assetEvents);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }
    }
}