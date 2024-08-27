using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetAttributeService
    {
        Task<UpsertAssetAttributeDto> UpsertAssetAttributeAsync(UpsertAssetAttribute command, CancellationToken token);

        // Task CheckUserRightPermissionAsync(UpsertAssetAttribute command, CancellationToken token);
        Task<IEnumerable<ValidateAssetAttributesDto>> ValidateAssetAttributesSeriesAsync(ValidateAssetAttributeSeries request, CancellationToken token);

        Task<BaseResponse> ValidateDeleteAssetTemplateAttributeAsync(Guid attributeId, CancellationToken token);

        //Task<BaseResponse> ValidateDeleteAssetTemplateAttributeAsync(ValidateRemoveAssetTemplateAttribute command, CancellationToken token);
    }
}
