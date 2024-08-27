using System.Data;

namespace Device.Application.DbConnections
{
    public class DbConnectionResolver : IDbConnectionResolver
    {
        private readonly IReadDbConnectionFactory _readDbConnectionFactory;
        private readonly IWriteConnectionFactory _writeConnectionFactory;

        public DbConnectionResolver(IWriteConnectionFactory writeConnectionFactory, IReadDbConnectionFactory readDbConnectionFactory)
        {
            _writeConnectionFactory = writeConnectionFactory;
            _readDbConnectionFactory = readDbConnectionFactory;
        }

        public IDbConnection CreateConnection(bool isReadOnly = false)
        {
            return isReadOnly ? _readDbConnectionFactory.CreateConnection() : _writeConnectionFactory.CreateConnection();
        }
    }
}
