using AHI.Infrastructure.MultiTenancy.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Persistence.Extensions;
using AHI.Infrastructure.UserContext;
using AHI.Infrastructure.UserContext.Extension;
using AHI.Infrastructure.Validation.Extension;
using AHI.Infrastructure.Exception.Filter;
using Prometheus;
using Prometheus.SystemMetrics;
using Newtonsoft.Json;

namespace Device.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            // Add application service from Application layer
            // Add persistence service
            services.AddPersistenceService();
            services.AddMultiTenantService();
            services.AddUserContextService();
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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseHttpMetrics();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWhen(
               context => !(context.Request.Path.HasValue && (context.Request.Path.Value.Contains("attributes/generate/assembly") || context.Request.Path.Value.StartsWith("/metrics"))),
               builder => builder.UseMiddleware<MultiTenancyMiddleware>());
            app.UseWhen(
                //context => !(context.Request.Path.HasValue && (context.Request.Path.Value.StartsWith("/dev/assets/series")
                //|| context.Request.Path.Value.Contains("attributes/generate/assembly")) || context.Request.Path.Value.StartsWith("/metrics")),
                context => !(context.Request.Path.HasValue && (context.Request.Path.Value.StartsWith("/dev/assets/series") || context.Request.Path.Value.Contains("attributes/generate/assembly")) || context.Request.Path.Value.StartsWith("/metrics")),
               builder => builder.UseMiddleware<UserContextMiddleware>());
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers()
                         .RequireAuthorization("ApiScope");
            });
        }
    }
}
