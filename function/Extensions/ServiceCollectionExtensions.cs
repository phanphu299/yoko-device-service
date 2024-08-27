using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Function.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddScopedWithActivator<TService, TImplementation>(this IServiceCollection serviceCollection, params Type[] inputTypes) where TService : class where TImplementation : class, TService
        {
            serviceCollection.AddScoped<TService, TImplementation>(pro => pro.CreateInstance<TImplementation>(inputTypes));
        }

        public static TImplementation CreateInstance<TImplementation>(this IServiceProvider provider, params Type[] inputTypes)
        {
            var inputs = inputTypes.Select(provider.GetRequiredService);
            return ActivatorUtilities.CreateInstance<TImplementation>(provider, inputs.ToArray());
        }
    }
}