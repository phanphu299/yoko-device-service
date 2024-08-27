using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Model;

namespace Function.Services.Abstractions
{
    internal interface IFileImportAttributeTemplateService
    {
        Task<ImportExportBasePayload> ImportFileAsync(string upn, Guid activityId, ExecutionContext context, string objectType, IEnumerable<string> fileNames, string dateTimeFormat, string dateTimeOffset);
    }
}
