using Device.Application.Asset.Validation;
using Device.Application.AssetTemplate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Device.ApplicationExtension.Extension
{
    public static class ArchiveApplicationExtension
    {
        public static IServiceCollection AddArchiveValidations(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<StaticAttributeValidation>();
            serviceCollection.AddSingleton<DynamicAttributeValidation>();
            serviceCollection.AddSingleton<IntegrationAttributeValidation>();
            serviceCollection.AddSingleton<CommandAttributeValidation>();
            serviceCollection.AddSingleton<AliasAttributeValidation>();
            serviceCollection.AddSingleton<RuntimeAttributeValidation>();
            serviceCollection.AddSingleton<IAttributeValidation>(service => {
                var staticValidation = service.GetRequiredService<StaticAttributeValidation>();
                var dynamicValidation = service.GetRequiredService<DynamicAttributeValidation>();
                var integrationValidation = service.GetRequiredService<IntegrationAttributeValidation>();
                var commandValidation = service.GetRequiredService<CommandAttributeValidation>();
                var aliasValidation = service.GetRequiredService<AliasAttributeValidation>();
                var runtimeValidation = service.GetRequiredService<RuntimeAttributeValidation>();
                aliasValidation.SetNextValidation(runtimeValidation);
                commandValidation.SetNextValidation(aliasValidation);
                integrationValidation.SetNextValidation(commandValidation);
                dynamicValidation.SetNextValidation(integrationValidation);
                staticValidation.SetNextValidation(dynamicValidation);
                return staticValidation;
            });

            serviceCollection.AddSingleton<StaticAttributeTemplateValidation>();
            serviceCollection.AddSingleton<DynamicAttributeTemplateValidation>();
            serviceCollection.AddSingleton<IntegrationAttributeTemplateValidation>();
            serviceCollection.AddSingleton<CommandAttributeTemplateValidation>();
            serviceCollection.AddSingleton<AliasAttributeTemplateValidation>();
            serviceCollection.AddSingleton<RuntimeAttributeTemplateValidation>();
            serviceCollection.AddSingleton<IAttributeTemplateValidation>(service => {
                var staticValidation = service.GetRequiredService<StaticAttributeTemplateValidation>();
                var dynamicValidation = service.GetRequiredService<DynamicAttributeTemplateValidation>();
                var integrationValidation = service.GetRequiredService<IntegrationAttributeTemplateValidation>();
                var commandValidation = service.GetRequiredService<CommandAttributeTemplateValidation>();
                var aliasValidation = service.GetRequiredService<AliasAttributeTemplateValidation>();
                var runtimeValidation = service.GetRequiredService<RuntimeAttributeTemplateValidation>();
                aliasValidation.SetNextValidation(runtimeValidation);
                commandValidation.SetNextValidation(aliasValidation);
                integrationValidation.SetNextValidation(commandValidation);
                dynamicValidation.SetNextValidation(integrationValidation);
                staticValidation.SetNextValidation(dynamicValidation);
                return staticValidation;
            });

            return serviceCollection;
        }
    }
}