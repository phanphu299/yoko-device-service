using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AHI.Device.Function.Service.Abstraction;

namespace AHI.Device.Function.Service
{
    /// <summary>
    /// Write into local folder -> It will sync into the cloud using k8s sci
    /// </summary>
    public class FileSystemLogService : ILogService
    {
        private readonly ILogger<FileSystemLogService> _log;
        private readonly string _workingDirectory;
        private readonly int _retensionDays;

        public FileSystemLogService(ILogger<FileSystemLogService> log, IConfiguration configuration)
        {
            _log = log;
            _workingDirectory = Path.Combine(configuration["DataFolder"] ?? "/var/data", "ingestion-log");
            _retensionDays = Convert.ToInt32(configuration["DataRetentionDays"] ?? "15");
        }

        /// <summary>
        /// === Log structure ===
        /// /var/data/ingestion-log
        ///     project_a_id
        ///         device_a_id
        ///             2023_10_01.txt (many lines)
        ///             2023_10_02.txt
        ///             2023_10_03.txt
        ///         device_b_id
        ///             2023_10_01.txt
        ///             2023_10_02.txt
        ///             2023_10_03.txt
        ///     project_b_id
        ///         device_c_id
        ///             2023_10_01.txt
        ///             2023_10_02.txt
        ///             2023_10_03.txt
        /// </summary>
        public async Task LogMessageAsync(string projectId, string deviceId, string message)
        {
            var currentDate = DateTime.UtcNow;
            var fileDirectory = Path.Combine(_workingDirectory, projectId, deviceId);

            if (!Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
            }

            var fileName = $"{currentDate.ToString("yyyy_MM_dd")}.txt";
            var filePath = Path.Combine(fileDirectory, fileName);

            // A device's logs will be stored in a file per day (the file will be expired after by default 15 days, and will be deleted by a timer LogCleaner.cs)
            await File.AppendAllLinesAsync(filePath, new List<string>() { message });
        }

        public void DeleteExpiredFiles()
        {
            var currentDate = DateTime.UtcNow;
            var files = GetFiles(_workingDirectory);
            var expiredFiles = files.Where(f => f.CreationTimeUtc < currentDate.AddDays(_retensionDays * -1));
            foreach (var expiredFile in expiredFiles)
            {
                File.Delete(expiredFile.FullName);
            }
        }

        /// <summary>
        /// Get all files in a directory (including sub directories)
        /// </summary>
        private IEnumerable<FileInfo> GetFiles(string directoryPath)
        {
            var queue = new Queue<string>();

            queue.Enqueue(directoryPath);

            while (queue.Count > 0)
            {
                directoryPath = queue.Dequeue();

                try
                {
                    foreach (string subDirectory in Directory.GetDirectories(directoryPath))
                    {
                        queue.Enqueue(subDirectory);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    _log.LogError("Directory access denied", ex);
                }

                FileInfo[] files = null;

                try
                {
                    var directoryInfo = new DirectoryInfo(directoryPath);
                    files = directoryInfo.GetFiles();
                }
                catch (UnauthorizedAccessException ex)
                {
                    _log.LogError("Directory access denied", ex);
                }

                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }
    }
}