using System;
using System.Collections.Generic;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Model.ImportModel.Validation;
using AHI.Device.Function.FileParser.Template;
using AHI.Device.Function.FileParser.Template.Abstraction;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.ErrorTracking;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Import.Abstraction;
using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel.Attribute;
using Function.Models.ImportModel.Validation;

namespace AHI.Device.Function.FileParser.Extension
{
    public static class DataParserExtensions
    {
        public static void AddDataParserServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ExcelDataParser>();
            serviceCollection.AddScoped<ExcelTemplate, SimpleExcelTemplate>();
            serviceCollection.AddScoped<ExcelTemplate, ComplexExcelTemplate>();
            serviceCollection.AddScoped(typeof(SimpleParser<>));
            serviceCollection.AddScoped(typeof(ComplexParser<>));
            serviceCollection.AddScoped<Abstraction.IParserContext, ParserContext>();

            serviceCollection.AddScoped<IFileHandler<DeviceModel>, DeviceExcelParser>();
            serviceCollection.AddScoped<IFileHandler<DeviceTemplate>, DeviceTemplateJsonHandler>();
            serviceCollection.AddScoped<IFileHandler<AssetTemplate>, AssetTemplateExcelParser>();
            serviceCollection.AddScoped<IFileHandler<AssetAttribute>, AssetAttributeExcelParser>();
            serviceCollection.AddScoped<IFileHandler<Uom>, UomExcelParser>();
            serviceCollection.AddScoped<IFileHandler<AttributeTemplate>, AssetTemplateAttributeExcelParser>();

            serviceCollection.AddScoped<ExcelTrackingService>();
            serviceCollection.AddScoped<JsonTrackingService>();
            serviceCollection.AddScoped<IExportTrackingService, ExportTrackingService>();
            serviceCollection.AddScoped<Abstraction.IFileIngestionTrackingService, FileIngestionTrackingService>();
            serviceCollection.AddScoped<IExcelTrackingService>(service => service.GetRequiredService<ExcelTrackingService>());
            serviceCollection.AddScoped<IJsonTrackingService>(service => service.GetRequiredService<JsonTrackingService>());
            serviceCollection.AddScoped<IDictionary<string, IImportTrackingService>>(service =>
            {
                return new Dictionary<string, IImportTrackingService> {
                    {MimeType.EXCEL, service.GetRequiredService<ExcelTrackingService>()},
                    {MimeType.JSON, service.GetRequiredService<JsonTrackingService>()}
                };
            });

            serviceCollection.AddScoped<IValidator<DeviceModel>, DeviceValidation>();
            serviceCollection.AddScoped<IValidator<DeviceTemplate>, DeviceTemplateValidation>();
            serviceCollection.AddScoped<IValidator<TemplatePayload>, TemplatePayloadValidation>();
            serviceCollection.AddScoped<IValidator<TemplateDetail>, TemplateDetailValidation>();
            serviceCollection.AddScoped<IValidator<TemplateBinding>, TemplateBindingValidation>();
            serviceCollection.AddScoped<IValidator<AssetTemplate>, AssetTemplateValidation>();
            serviceCollection.AddScoped<IValidator<AssetTemplateAttribute>, AssetTemplateAttributeValidation>();
            serviceCollection.AddScoped<IValidator<AssetAttribute>, AssetAttributeValidation>();
            serviceCollection.AddScoped<IValidator<Uom>, UomValidation>();
            serviceCollection.AddScoped<IValidator<AttributeTemplate>, AttributeTemplateParserValidation>();
            serviceCollection.AddScoped<IDictionary<Type, IValidator>>(service =>
            {
                return new Dictionary<Type, IValidator> {
                    {typeof(DeviceModel), service.GetRequiredService<IValidator<DeviceModel>>()},
                    {typeof(DeviceTemplate), service.GetRequiredService<IValidator<DeviceTemplate>>()},
                    {typeof(TemplatePayload), service.GetRequiredService<IValidator<TemplatePayload>>()},
                    {typeof(TemplateDetail), service.GetRequiredService<IValidator<TemplateDetail>>()},
                    {typeof(TemplateBinding), service.GetRequiredService<IValidator<TemplateBinding>>()},
                    {typeof(AssetTemplate), service.GetRequiredService<IValidator<AssetTemplate>>()},
                    {typeof(AssetTemplateAttribute), service.GetRequiredService<IValidator<AssetTemplateAttribute>>()},
                    {typeof(Uom), service.GetRequiredService<IValidator<Uom>>()},
                    {typeof(AttributeTemplate), service.GetRequiredService<IValidator<AttributeTemplate>>()},
                    {typeof(AssetAttribute), service.GetRequiredService<IValidator<AssetAttribute>>()}
                };
            });
        }
    }
}
