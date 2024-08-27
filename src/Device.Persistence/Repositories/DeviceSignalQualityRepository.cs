using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Repository;
using Device.Domain.Entity;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using AHI.Infrastructure.SharedKernel.Extension;

namespace Device.Persistence.Repository
{
    public class DeviceSignalQualityRepository : IDeviceSignalQualityRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        public DeviceSignalQualityRepository(IConfiguration configuration, ITenantContext tenantContext)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
        }
        protected virtual IDbConnection GetDbConnection()
        {
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            var connection = new NpgsqlConnection(connectionString);
            return connection;
        }
        public async Task<IEnumerable<DeviceSignalQuality>> GetAllSignalQualityAsync()
        {
            using (var dbConnection = GetDbConnection())
            {
                var query = await dbConnection.QueryAsync<DeviceSignalQuality>("select id as Id, name as Name from device_signal_quality_codes");
                dbConnection.Close();
                return query;
            }
        }
    }
}