using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Extensions.Configuration;
using Npgsql;
using AHI.Infrastructure.Repository.Abstraction;
namespace AHI.Infrastructure.Repository
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<DbConnectionFactory> _logger;

        public DbConnectionFactory(
            IConfiguration configuration,
            ITenantContext tenantContext,
            ILoggerAdapter<DbConnectionFactory> logger
            )
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public NpgsqlConnection CreateConnection(string projectId = null)
        {
            try
            {
                var connectionStringCfg = _configuration["ConnectionStrings:Default"];
                if (string.IsNullOrWhiteSpace(projectId))
                    projectId = _tenantContext.ProjectId;
                var connectionString = connectionStringCfg.BuildConnectionString(_configuration, projectId);
                return new NpgsqlConnection(connectionString);
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, $"DbConnectionFactory create connection error: projectId={projectId}, readOnly=false", projectId);
                throw;
            }
        }
    }
}