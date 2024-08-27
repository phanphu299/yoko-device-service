using System.Data.Common;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Device.Consumer.KraftShared.Helpers
{
    public static class DbHelper
    {
        public static DbConnection GetDbConnection(IConfiguration configuration, ITenantContext tenantContext)
        {
            var connectionString = configuration["ConnectionStrings:Default"].BuildConnectionString(configuration, tenantContext.ProjectId);
            return new NpgsqlConnection(connectionString);
        }
    }
}