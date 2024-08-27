using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Device.Consumer.KraftShared.Repositories
{
    public class WriteDbConnectionFactory : IWriteDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;

        public WriteDbConnectionFactory(IConfiguration configuration, ITenantContext tenantContext)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
        }

        public NpgsqlConnection CreateConnection(string projectId = null)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                projectId = _tenantContext.ProjectId;
            var connectionStringCfg =  _configuration["ConnectionStrings:Default"];
            var connectionString = connectionStringCfg.BuildConnectionString(_configuration, projectId);
            return new NpgsqlConnection(connectionString);
        }
    }
}