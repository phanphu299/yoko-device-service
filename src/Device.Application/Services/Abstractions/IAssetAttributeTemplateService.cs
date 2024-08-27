using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch;
using System.Threading.Tasks;
using Device.Application.SharedKernel;
using System.Threading;
using Device.Application.AssetTemplate.Command.Model;
using System;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetAttributeTemplateService
    {
        Task<IList<BaseJsonPathDocument>> UpsertAssetAttributeTemplateAsync(JsonPatchDocument document, GetAssetTemplateDto assetTemplate, CancellationToken cancellationToken);
        Task<bool> CheckExistAttributeAsync(Guid assetAttributeId);
    }
}
