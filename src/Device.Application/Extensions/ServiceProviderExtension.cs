using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.UserContext.Extension;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.ApplicationExtension.Extension;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Application.Extension
{
    public static class ServiceProviderExtension
    {
        public static IServiceScope CreateNewScope(this IServiceProvider serviceProvider)
        {
            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var tenantContext = serviceProvider.GetRequiredService<ITenantContext>();
            var userContext = serviceProvider.GetRequiredService<IUserContext>();
            var securityContext = serviceProvider.GetRequiredService<ISecurityContext>();

            // Create the scope
            var scope = serviceScopeFactory.CreateScope();

            // Copy to new scope
            var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.CopyTo(scopeTenantContext);

            var scopeUserContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            userContext.CopyTo(scopeUserContext);

            var scopeSecurityContext = scope.ServiceProvider.GetRequiredService<ISecurityContext>();
            securityContext.CopyTo(scopeSecurityContext);

            return scope;
        }

        public static IServiceScope CreateNewScope(this IServiceProvider serviceProvider, ITenantContext tenantContext)
        {
            if (tenantContext == null)
                throw new ArgumentNullException(nameof(tenantContext));

            var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var scope = serviceScopeFactory.CreateScope();

            var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            scopeTenantContext.CopyFrom(tenantContext);

            return scope;
        }
    }
}