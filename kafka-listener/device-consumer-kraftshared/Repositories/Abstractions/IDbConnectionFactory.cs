using Npgsql;

namespace Device.Consumer.KraftShared.Repositories.Abstraction
{
    public interface IDbConnectionFactory
    {
        NpgsqlConnection CreateConnection(string projectId = null);
    }
}