using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Extensions.Configuration;
using Npgsql;
namespace AHI.Infrastructure.Repository
{
    public class ReadOnlyDbConnectionFactory : IReadOnlyDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<ReadOnlyDbConnectionFactory> _logger;

        public ReadOnlyDbConnectionFactory(
            IConfiguration configuration,
            ITenantContext tenantContext,
            ILoggerAdapter<ReadOnlyDbConnectionFactory> logger
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
                var connectionStringCfg =  _configuration["ConnectionStrings:ReadOnly"];
                if (string.IsNullOrEmpty(connectionStringCfg))
                    connectionStringCfg = _configuration["ConnectionStrings:Default"];
                if (string.IsNullOrWhiteSpace(projectId))
                    projectId = _tenantContext.ProjectId;
                var connectionString = connectionStringCfg.BuildConnectionString(_configuration, projectId);
                return new NpgsqlConnection(connectionString);
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, "DbConnectionFactory create connection error: projectId={projectId}, readOnly=true", projectId);
                throw;
            }
        }
    }
}