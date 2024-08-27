using System;
using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Model.ImportModel.Attribute;
using Microsoft.Azure.WebJobs;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface ITemplateAttributeParserService
    {
        Task<ImportAttributeResponse> ParseAsync(AssetTemplateAttributeMessage message, Guid activityId, ExecutionContext context);
    }
}
