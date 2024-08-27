using System.Data;

namespace Device.Application.DbConnections
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
