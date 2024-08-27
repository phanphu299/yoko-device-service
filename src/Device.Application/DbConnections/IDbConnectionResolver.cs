using System.Data;

namespace Device.Application.DbConnections
{
    public interface IDbConnectionResolver
    {
        IDbConnection CreateConnection(bool isReadOnly = false);
    }
}
