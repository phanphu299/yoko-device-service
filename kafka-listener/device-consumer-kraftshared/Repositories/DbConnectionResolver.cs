using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Npgsql;

namespace Device.Consumer.KraftShared.Repositories
{
    public class DbConnectionResolver : IDbConnectionResolver
    {
        private readonly IWriteDbConnectionFactory _writeDbConnectionFactory;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;

        public DbConnectionResolver(IWriteDbConnectionFactory writeDbConnectionFactory, IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory)
        {
            _writeDbConnectionFactory = writeDbConnectionFactory;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
        }


        public NpgsqlConnection CreateConnection(string projectId = null, bool isReadOnly = false)
        {
            return isReadOnly ? _readOnlyDbConnectionFactory.CreateConnection(projectId) : _writeDbConnectionFactory.CreateConnection(projectId);
        }
    }
}