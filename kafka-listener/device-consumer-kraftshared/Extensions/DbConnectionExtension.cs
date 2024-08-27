using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Helpers;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog.Core;


namespace Device.Consumer.KraftShared.Extensions
{
    public static class DbConnectionExtension
    {

        public static async Task<(int rowAffects, string errMessage)> BulkUpsertCSVAsync<T>(
            this NpgsqlConnection dbConnection,
            string targetTableAndProperties,
            string onConflictProperties,
            string onConflictAction,
            IEnumerable<T> entities,
            IEnumerable<string> customHeaders = null,
            CancellationToken ct = default)
        {
            try
            {
                if (dbConnection == null)
                    throw new ArgumentNullException("DbConnection must not be null");
                if (!entities.Any())
                    throw new ArgumentNullException("entities must not be null or empty");

                var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");
                await WriteDataToCsvFileAsync(filePath, entities, customHeaders);
                var sql = string.Format("COPY {0} FROM '{1}' ON CONFLICT {2} DO UPDATE {3}", targetTableAndProperties, filePath, onConflictProperties, onConflictAction);
                dbConnection.Open();
                var command = dbConnection.CreateCommand();
                command.CommandText = sql;
                var affectedAmount = await command.ExecuteNonQueryAsync();
                dbConnection.Close();
                DeleteFile(filePath);
                return (affectedAmount, string.Empty);
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

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
            ILogger logger,
            CancellationToken ct = default)
        {
            try
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () => {
                    var cmd = string.Format("COPY {0} from STDIN (FORMAT BINARY)", targetTableAndProperties);
                    using (var writer = dbConnection.BeginBinaryImport(cmd))
                    {
                        await WriteDataToConsoleAsync(writer, entities, propertyOrders);
                        var result = writer.Complete();
                        return ((int)result, string.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

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
        public static async Task<(int rowAffects, string errMessage)> BulkInsertWithWriterAsync<T>(this NpgsqlConnection dbConnection,
            string targetTableAndProperties,
            Func<NpgsqlBinaryImporter, Task> writeDataToSTDINAsync,
            ILogger logger,
            CancellationToken ct = default)
        {
            try
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () =>
                {
                    var cmd = string.Format("COPY {0} from STDIN (FORMAT BINARY)", targetTableAndProperties);
                    using (var writer = dbConnection.BeginBinaryImport(cmd))
                    {
                        await writeDataToSTDINAsync(writer);
                        var result = await writer.CompleteAsync();
                        return ((int)result, string.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
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
            ILogger logger)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(logger);
                return await retryStrategy.ExecuteAsync(async () => {
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
                    Console.WriteLine($"==== BulkUpsertAsync successed, took: {watch.ElapsedMilliseconds} ms");
                    return (rowAffects, string.Empty);
                });
            }
            catch (Exception ex)
            {
                watch.Stop();
                Console.WriteLine($"==== BulkUpsertAsync failed, took: {watch.ElapsedMilliseconds} ms");
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

        /// <summary>
        /// Write data to csv file then return file path
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        private static async Task<(bool isSuccess, string errMessage)> WriteDataToCsvFileAsync<T>(string filePath, IEnumerable<T> data, IEnumerable<string> customHeaders = null, string delimiter = ",")
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.csv");

                var stringBuilder = new StringBuilder();
                string[] propertyNames = data.First().GetType().GetProperties().Select(p => p.Name).ToArray();

                //extract header from record
                if (customHeaders == null || !customHeaders.Any())
                    customHeaders = propertyNames;

                var headerRow = string.Join(delimiter, customHeaders);
                stringBuilder.AppendLine(headerRow);

                //Prepare records
                foreach (var record in data)
                    stringBuilder.AppendLine(ToCsvRecordString(delimiter, record, customHeaders));

                //Write data to file
                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8, 65536))
                    await sw.WriteAsync(stringBuilder.ToString());

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.ToString());
            }
        }

        private static string ToCsvRecordString<T>(string delimiter, T data, IEnumerable<string> propertyNames)
        {
            var values = propertyNames.Select(propertyName => data.GetType().GetProperty(propertyName).GetValue(data, null)).ToArray();
            return string.Join(delimiter, values);
        }

        private static void DeleteFile(string filePath)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
