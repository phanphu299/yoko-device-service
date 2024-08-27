using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Device.Persistence.Extensions
{
    public static class ConnectionExtension
    {
        public static async Task<IEnumerable<T>> QueryPagingDataAsync<T>(this IDbConnection dbConnection, string query, object param, int commandTimeout)
        {
            IEnumerable<T> output = default;
            using (var connection = dbConnection)
            {
                try
                {
                    output = await connection.QueryAsync<T>(query, param, commandTimeout: commandTimeout);
                }
                finally
                {
                    connection.Close();
                }
            }
            return output;
        }

        public static async Task<int> QueryTotalCountAsync(this IDbConnection dbConnection, string query, object param, int commandTimeout)
        {
            var totalCount = 0;
            using (var connection = dbConnection)
            {
                try
                {
                    totalCount = await connection.ExecuteScalarAsync<int>(query, param, commandTimeout: commandTimeout);
                }
                finally
                {
                    connection.Close();
                }
            }
            return totalCount;
        }
    }
}