using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Audit.Service.Abstraction;
using Device.Application.Constant;
using Device.Application.Events;
using AHI.Infrastructure.Audit.Constant;

namespace Device.Application.Service
{
    public class AssetAssemblyService : IAssetAssemblyService
    {
        private readonly IAssetService _assetService;
        protected readonly IAuditLogService _auditLogService;
        public AssetAssemblyService(IAssetService assetService, IAuditLogService auditLogService)
        {
            _assetService = assetService;
            _auditLogService = auditLogService;
        }
        public virtual async Task<AttributeAssemblyDto> GenerateAssemblyAsync(Guid id, CancellationToken token)
        {
            try
            {
                var assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(id), token);
                if (assetDto == null)
                {
                    throw new EntityNotFoundException();
                }
                var builder = new StringBuilder();
                foreach (var attribute in assetDto.Attributes)
                {
                    builder.AppendLine($"public {GetPrimitiveType(attribute.DataType)} {attribute.NormalizeName} {{ get; set; }}");
                }

                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, ActivitiesLogEventAction.Download_Attribute_Assembly, ActionStatus.Success, assetDto.Id, assetDto.Name, assetDto);

                return BuildAssembly(assetDto.NormalizeName, builder.ToString());
            }
            catch
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET, ActivitiesLogEventAction.Download_Attribute_Assembly, ActionStatus.Fail, id, payload: id);
                throw;

            }
        }
        protected AttributeAssemblyDto BuildAssembly(string name, string content)
        {
            var classContent = @$"
                                using System;
    
                                namespace AHI.Model
                                {{
                                    public class {name}_timeseries
                                    {{
                                        {"public DateTime AHI_Timestamp { get; set; }"}
                                        {content}
                                    }}
                                    public class {name}_snapshot
                                    {{
                                        public DateTime AHI_Timestamp {{ get; set; }}
                                        public string AHI_AssetName {{ get; set; }}
                                        public string AHI_AttributeName {{ get; set; }}
                                        public string AHI_Value {{ get; set; }}
                                    }}
                                }}";
            return new AttributeAssemblyDto($"{name}.cs", Encoding.UTF8.GetBytes(classContent));
        }
        protected string GetPrimitiveType(string dataType)
        {
            switch (dataType)
            {
                case "text":
                    return "string";
                case "datetime":
                    return "DateTime";
                default:
                    return dataType;
            }
        }
    }
}
