using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using AHI.Infrastructure.Audit.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IFileExportService
    {
        Task<ImportExportBasePayload> ExportFileAsync(string upn, Guid activityId, ExecutionContext context, string objectType, IEnumerable<string> ids,
                                               string dateTimeFormat, string dateTimeOffset);
    }
}