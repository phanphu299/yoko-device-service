using System;
using System.Data;
using System.Text.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.DbConnections;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Device.Persistence.DbConnections
{
    public class ReadDbConnectionFactory : IReadDbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<ReadDbConnectionFactory> _logger;

        public ReadDbConnectionFactory(IConfiguration configuration, ITenantContext tenantContext, ILoggerAdapter<ReadDbConnectionFactory> logger)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public IDbConnection CreateConnection()
        {
            var connectionStringCfg = _configuration["ConnectionStrings:ReadOnly"];
            if (string.IsNullOrEmpty(connectionStringCfg))
                connectionStringCfg = _configuration["ConnectionStrings:Default"];

            var connectionString = connectionStringCfg.BuildConnectionString(_configuration, _tenantContext.ProjectId);
            var con = new NpgsqlConnection(connectionString);
            try
            {
                con.Open();
                return con;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ReadDbConnectionFac OpenFail. ProjectId={projectId}", _tenantContext.ProjectId);
                throw;
            }
        }
    }
}