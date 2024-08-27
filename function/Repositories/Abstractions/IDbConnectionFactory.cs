using Npgsql;

namespace AHI.Infrastructure.Repository.Abstraction
{
    public interface IDbConnectionFactory
    {
        NpgsqlConnection CreateConnection(string projectId = null);
    }
}