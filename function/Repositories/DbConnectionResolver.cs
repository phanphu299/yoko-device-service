using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using Npgsql;

namespace Function.Repositories
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