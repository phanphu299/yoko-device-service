﻿using Npgsql;

namespace AHI.Infrastructure.Repository.Abstraction
{
    public interface IDbConnectionResolver
    {
        NpgsqlConnection CreateConnection(string projectId = null, bool isReadOnly = false);
    }
}