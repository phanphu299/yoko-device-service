using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.UserContext.Extension;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using MediatR;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
namespace Device.Application.Asset.Command.Handler
{
    public class SendConfigurationToDeviceIotMutipleRequestHandler : IRequestHandler<SendConfigurationToDeviceIotMutiple, SendConfigurationResultMutipleDto>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserContext _userContext;
        public SendConfigurationToDeviceIotMutipleRequestHandler(IServiceProvider serviceProvider, IUserContext userContext)
        {
            _serviceProvider = serviceProvider;
            _userContext = userContext;
        }
        public async Task<SendConfigurationResultMutipleDto> Handle(SendConfigurationToDeviceIotMutiple request, CancellationToken cancellationToken)
        {

            var results = new List<SendConfigurationResultMutipleDetailDto<AttributeCommandDto>>();

            var sendconfigSync = request.Data.GroupBy(x => new { x.TenantId, x.SubscriptionId, x.ProjectId })
                          .Select(s => SendConfigurationToProjectMutipleAsync(results, s.Key.TenantId, s.Key.SubscriptionId, s.Key.ProjectId, s.GroupBy(a => a.AssetId), cancellationToken));
            await Task.WhenAll(sendconfigSync);

            var hasFailCommand = results.Any(x => !x.Status);
            var hasSuccessCommand = results.Any(x => x.Status);

            string status = Status.RESULT_SUCCESS;
            if (hasFailCommand && hasSuccessCommand)
            {
                status = Status.RESULT_PARTIAL;
            }
            else if (hasFailCommand && !hasSuccessCommand)
            {
                status = Status.RESULT_FAIL;
            }

            var output = new SendConfigurationResultMutipleDto(status, results);
            return output;
        }

        private async Task SendConfigurationToProjectMutipleAsync(
            List<SendConfigurationResultMutipleDetailDto<AttributeCommandDto>> results,
            string tenantId,
            string subscriptionId,
            string projectId,
            IEnumerable<IGrouping<Guid, SendConfigurationToDeviceIot>> assets,
            CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopeTenantContext = scope.ServiceProvider.GetService<ITenantContext>();
                scopeTenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);

                var scopeUserContext = scope.ServiceProvider.GetService<IUserContext>();
                _userContext.CopyTo(scopeUserContext);

                var assetService = scope.ServiceProvider.GetService(typeof(IAssetService)) as IAssetService;
                var securityService = scope.ServiceProvider.GetService(typeof(ISecurityService)) as ISecurityService;

                var input = new AttributeCommandDto(scopeTenantContext.TenantId, scopeTenantContext.SubscriptionId, scopeTenantContext.ProjectId, null);

                var authorizeAccessAsset = assets.GroupBy(x => x.Key).Select(x => AuthorizeAccessAssetsAsync(assetService, securityService, x.Key, input, cancellationToken).HandleSendConfigurationResult(input));
                var authorizeVerify = await Task.WhenAll(authorizeAccessAsset);

                if (authorizeVerify.Any(x => !x.Status))
                {
                    results.Add(authorizeVerify.First());
                }
                else
                {
                    var data = await assetService.SendConfigurationToDeviceIotMutipleAsync(assets, cancellationToken).HandleSendConfigurationResult(input);
                    results.Add(data);
                }
            }
        }
        private async Task<AttributeCommandDto> AuthorizeAccessAssetsAsync(IAssetService assetService, ISecurityService securityService, Guid assetId, AttributeCommandDto attributeCommandDto, CancellationToken cancellationToken)
        {
            try
            {
                var assetDto = await assetService.FindAssetByIdAsync(new GetAssetById(assetId), cancellationToken);
                securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.WRITE_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy);
                return attributeCommandDto;
            }
            catch (System.Exception ex)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(ex.Message));
            }
        }
    }
}