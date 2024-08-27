using System;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Device.Consumer.KraftShared.Repositories;
using System.Linq;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
namespace Device.Consumer.KraftShared.Extensions
{
    public static class PersistenceExtensions
    {
        public static void AddRepositories(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IDeviceRepository, DeviceRepository>();
            serviceCollection.AddScopedWithActivator<IReadOnlyAssetRepository, AssetRepository>(typeof(IReadOnlyDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadOnlyDeviceRepository, DeviceRepository>(typeof(IReadOnlyDbConnectionFactory));
        }
    }
}
