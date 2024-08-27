using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using AHI.Infrastructure.Audit.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IFileImportService
    {
        Task<ImportExportBasePayload> ImportFileAsync(string upn, Guid activityId, ExecutionContext context, string objectType, IEnumerable<string> fileNames, string dateTimeFormat, string dateTimeOffset, Guid correlationId);
    }
}