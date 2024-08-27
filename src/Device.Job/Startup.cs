using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Device.Job.Model;
using Device.Job.Service;
using Device.Job.Validation;
using Device.Job.Service.Abstraction;
using Device.Persistence.Extensions;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.UserContext;
using AHI.Infrastructure.Exception.Filter;
using AHI.Infrastructure.Validation.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.MultiTenancy.Middleware;
using Prometheus.SystemMetrics;
using Prometheus;
using Newtonsoft.Json;
using AHI.Infrastructure.Audit.Extension;

namespace Device.Job
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddPersistenceService();
            services.AddMultiTenantService();
            services.AddAuditLogService();
            services.AddDynamicValidation();
            services.AddApplicationServices();
            services.AddControllers(option =>
            {
                option.ExceptionHandling();
            }).AddNewtonsoftJson(option =>
            {
                option.SerializerSettings.NullValueHandling = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting.NullValueHandling;
                option.SerializerSettings.DateFormatString = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting.DateFormatString;
                option.SerializerSettings.ReferenceLoopHandling = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting.ReferenceLoopHandling;
                option.SerializerSettings.DateParseHandling = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting.DateParseHandling;
                option.SerializerSettings.ContractResolver = AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting.ContractResolver;
            });
            JsonConvert.DefaultSettings = () => AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting; // Newtonsoft.Json not receiveing fully config from above step with caused issue when Deserilize an object content value like 2023-05-11T14:05:58:0000 - the milisecond part will make the error
            services.AddAuthentication()
                .AddIdentityServerAuthentication("oidc",
                jwtTokenOption =>
                {
                    jwtTokenOption.Authority = Configuration["Authentication:Authority"];
                    jwtTokenOption.RequireHttpsMetadata = Configuration["Authentication:Authority"].StartsWith("https");
                    jwtTokenOption.TokenValidationParameters.ValidateAudience = false;
                    jwtTokenOption.ClaimsIssuer = Configuration["Authentication:Issuer"];
                }, referenceTokenOption =>
                {
                    referenceTokenOption.IntrospectionEndpoint = Configuration["Authentication:IntrospectionEndpoint"];
                    referenceTokenOption.ClientId = Configuration["Authentication:ApiScopeName"];
                    referenceTokenOption.ClientSecret = Configuration["Authentication:ApiScopeSecret"];
                    referenceTokenOption.ClaimsIssuer = Configuration["Authentication:Issuer"];
                    referenceTokenOption.SaveToken = true;
                });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", Configuration["Authentication:ApiScopeName"]);
                });
            });
            services.AddSystemMetrics();

            services.AddSingleton<JobBackgroundService>();
            services.AddHostedService(sp => sp.GetRequiredService<JobBackgroundService>());
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<IExportNotificationService, ExportNotificationService>();
            services.AddScoped<IStorageService, StorageService>();
            services.AddScoped<IDataSourceService, TimeseriesService>();
            services.AddScoped<IOutputFileService, CsvOutputFileService>();

            services.AddSingleton<FluentValidation.IValidator<AddJob>, AddJobValidation>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseHttpMetrics();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "tmp")),
                RequestPath = new PathString("/dev/jobs")
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWhen(
                context => !(context.Request.Path.HasValue
                            && context.Request.Path.Value.StartsWith("/metrics")),
                builder =>
                {
                    builder.UseMiddleware<MultiTenancyMiddleware>();
                    builder.UseMiddleware<UserContextMiddleware>();
                });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers()
                         .RequireAuthorization("ApiScope");
            });
        }
    }
}