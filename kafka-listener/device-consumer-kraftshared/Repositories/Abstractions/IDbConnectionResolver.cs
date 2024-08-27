using Npgsql;

namespace Device.Consumer.KraftShared.Repositories.Abstraction
{
    public interface IDbConnectionResolver
    {
        NpgsqlConnection CreateConnection(string projectId = null, bool isReadOnly = false);
    }
}