using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Events;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class AssetTemplateAssemblyService : AssetAssemblyService, IAssetTemplateAssemblyService
    {
        private readonly IAssetTemplateService _assetTemplateService;
        public AssetTemplateAssemblyService(IAssetTemplateService assetTemplateService, IAssetService assetService, IAuditLogService auditLogService) : base(assetService, auditLogService)
        {
            _assetTemplateService = assetTemplateService;
        }
        public override async Task<AttributeAssemblyDto> GenerateAssemblyAsync(Guid id, CancellationToken token)
        {
            try
            {
                var dto = await _assetTemplateService.FindTemplateByIdAsync(new AssetTemplate.Command.GetAssetTemplateById(id), token);
                var builder = new StringBuilder();
                foreach (var attribute in dto.Attributes)
                {
                    builder.AppendLine($"public {GetPrimitiveType(attribute.DataType)} {attribute.NormalizeName} {{ get; set; }}");
                }

                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActivitiesLogEventAction.Download_Attribute_Assembly, ActionStatus.Success, dto.Id, dto.Name, dto);

                return BuildAssembly(dto.NormalizeName, builder.ToString());
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_TEMPLATE, ActivitiesLogEventAction.Download_Attribute_Assembly, ActionStatus.Fail, id, payload: id);
                throw;
            }
        }
    }
}