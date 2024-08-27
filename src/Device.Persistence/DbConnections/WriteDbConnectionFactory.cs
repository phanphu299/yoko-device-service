using System;
using System.Data;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.DbConnections;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Device.Persistence.DbConnections
{
    public class WriteDbConnectionFactory : IWriteConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<WriteDbConnectionFactory> _logger;

        public WriteDbConnectionFactory(IConfiguration configuration, ITenantContext tenantContext, ILoggerAdapter<WriteDbConnectionFactory> logger)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public IDbConnection CreateConnection()
        {
            var connectionStringCfg = _configuration["ConnectionStrings:Default"];
            var connectionString = connectionStringCfg.BuildConnectionString(_configuration, _tenantContext.ProjectId);
            var con = new NpgsqlConnection(connectionString);
            try
            {
                con.Open();
                return con;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "WriteDbConnectionFactory OpenFail. ProjectId={projectId}", _tenantContext.ProjectId);
                throw;
            }
        }
    }
}