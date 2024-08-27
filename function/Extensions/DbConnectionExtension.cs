using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Function.Helper;
using Npgsql;

namespace Function.Extension
{
    public static class DbConnectionExtension
    {
        /// <summary>
        /// BulkUpsertAsync use COPY binary to insert, must provide property list as same as table insert list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbConnection"></param>
        /// <param name="targetTableAndProperties"></param>
        /// <param name="onConflictProperties"></param>
        /// <param name="onConflictAction"></param>
        /// <param name="entities"></param>
        /// <param name="propertyOrders"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(int rowAffects, string errMessage)> BulkInsertAsync<T>(this NpgsqlConnection dbConnection,
            string targetTableAndProperties,
            IEnumerable<T> entities,
            IEnumerable<string> propertyOrders,
            IBaseLoggerAdapter logger,
            CancellationToken ct = default)
        {
            try
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () =>
                {
                    var count = 1;
                    var cmd = string.Format("COPY {0} from STDIN (FORMAT BINARY)", targetTableAndProperties);
                    try
                    {
                        using (var writer = dbConnection.BeginBinaryImport(cmd))
                        {
                            await WriteDataToConsoleAsync(writer, entities, propertyOrders);
                            var result = writer.Complete();
                            return ((int)result, string.Empty);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogDebug(ex, $"[BulkInsertAsync] Failed {count} time(s): {ex.Message} - Connection State: {dbConnection.State}");
                        logger.LogTrace($"[BulkInsertAsync] Failed {count} time(s) - Details: Inner Exception: {ex.InnerException?.ToJson()}");
                        count++;
                        throw;
                    }
                });
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"[BulkInsertAsync] failed");
                return (0, ex.Message);
            }
        }

        /// <summary>
        /// BulkUpsertAsync use COPY binary to insert, must provide property list as same as table insert list
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="targetTableAndProperties"></param>
        /// <param name="onConflictProperties"></param>
        /// <param name="onConflictAction"></param>
        /// <param name="entities"></param>
        /// <param name="propertyOrders"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(int rowAffects, string errMessage)> BulkInsertWithWriterAsync(this NpgsqlConnection dbConnection,
            string targetTableAndProperties,
            Func<NpgsqlBinaryImporter, Task> writeDataToSTDINAsync,
            IBaseLoggerAdapter logger,
            CancellationToken ct = default)
        {
            try
            {
                var count = 1;

                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () =>
                {
                    var cmd = string.Format("COPY {0} from STDIN (FORMAT BINARY)", targetTableAndProperties);
                    try
                    {
                        using (var writer = dbConnection.BeginBinaryImport(cmd))
                        {
                            await writeDataToSTDINAsync(writer);
                            var result = await writer.CompleteAsync();
                            return ((int)result, string.Empty);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogDebug(ex, $"[BulkInsertWithWriterAsync] Failed {count} time(s): {ex.Message} - Connection State: {dbConnection.State}");
                        logger.LogTrace($"[BulkInsertWithWriterAsync] Failed {count} time(s) - Details: Inner Exception: {ex.InnerException?.ToJson()}");
                        count++;
                        throw;
                    }
                });
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, $"[BulkInsertWithWriterAsync] Failed: {ex.Message}!");
                return (0, ex.Message);
            }
        }

        /// <summary>
        /// BulkUpsertAsync use COPY binary to insert, must provide property list as same as table insert list
        /// </summary>
        /// <param name="dbConnection"></param>
        /// <param name="targetTableAndProperties"></param>
        /// <param name="onConflictProperties"></param>
        /// <param name="onConflictAction"></param>
        /// <param name="entities"></param>
        /// <param name="propertyOrders"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<(int rowAffects, string errMessage)> BulkUpsertAsync(this NpgsqlConnection dbConnection,
            string targetTableName,
            string targetTableFields,
            string onConflictFields,
            string onConflictAction,
            Func<NpgsqlBinaryImporter, Task> writeDataToSTDINAsync,
            IBaseLoggerAdapter logger)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () =>
                {
                    var count = 1;
                    try
                    {
                        var fields = targetTableFields.Replace("(", "").Replace(")", "");
                        var temptableName = $"tmp_table_{Guid.NewGuid().ToString().Replace("-", "")}";
                        var createTempTableSql = $"CREATE TEMP TABLE {temptableName} AS SELECT {fields} FROM {targetTableName} WITH NO DATA;";
                        await using var cmd1 = new NpgsqlCommand(createTempTableSql, dbConnection);
                        _ = await cmd1.ExecuteNonQueryAsync();
                        var copyBinaryCmd = $"COPY {temptableName}{targetTableFields} FROM STDIN (FORMAT BINARY)";
                        using (var writer = dbConnection.BeginBinaryImport(copyBinaryCmd))
                        {
                            await writeDataToSTDINAsync(writer);
                            await writer.CompleteAsync();
                        }
                        var insertSql = $"INSERT INTO {targetTableName}{targetTableFields} SELECT {fields} FROM {temptableName} ON CONFLICT {onConflictFields} DO {onConflictAction};";
                        await using var cmd2 = new NpgsqlCommand(insertSql, dbConnection);
                        var rowAffects = await cmd2.ExecuteNonQueryAsync();
                        watch.Stop();
                        logger.LogTrace($"[BulkUpsertAsync] succeeded, took: {watch.ElapsedMilliseconds} ms");
                        return (rowAffects, string.Empty);
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogDebug(ex, $"[BulkUpsertAsync] Failed {count} time(s): {ex.Message} - Connection State: {dbConnection.State}");
                        logger.LogTrace($"[BulkUpsertAsync] Failed {count} time(s) - Details: Inner Exception: {ex.InnerException?.ToJson()}");
                        count++;
                        throw;
                    }

                });
            }
            catch (System.Exception ex)
            {
                watch.Stop();
                logger.LogError(ex, $"[BulkUpsertAsync] Failed, took: {watch.ElapsedMilliseconds} ms");
                return (0, ex.ToString());
            }
        }

        private static async Task WriteDataToConsoleAsync<T>(NpgsqlBinaryImporter writer, IEnumerable<T> data, IEnumerable<string> propertyOrders)
        {
            foreach (var record in data)
            {
                var values = propertyOrders.Select(propertyName => record.GetType().GetProperty(propertyName).GetValue(record, null)).ToArray();
                writer.StartRow();
                foreach (var val in values)
                    writer.Write(val);
            }
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }
}
