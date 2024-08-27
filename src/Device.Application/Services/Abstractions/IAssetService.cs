using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Device.Command;
using Device.Application.Device.Command.Model;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetService : ISearchService<Domain.Entity.Asset, Guid, GetAssetByCriteria, GetAssetSimpleDto>
    {
        Task<BaseSearchResponse<GetAssetHierarchyDto>> HierarchySearchAsync(GetAssetHierarchy command, CancellationToken cancellationToken);

        Task<UpsertAssetDto> UpsertAssetAsync(UpsertAsset command, CancellationToken cancellationToken);
        Task<GetAssetDto> FindAssetByIdOptimizedAsync(GetAssetById command, CancellationToken cancellationToken);
        Task<GetAssetDto> FindAssetByIdAsync(GetAssetById command, CancellationToken cancellationToken);

        Task<GetAssetDto> FindAssetSnapshotByIdAsync(GetAssetById command, CancellationToken cancellationToken);

        Task<GetFullAssetDto> FindFullAssetByIdAsync(GetFullAssetById command, CancellationToken cancellationToken);

        Task<GetAssetDto> GetAssetCloneAsync(GetAssetClone command, CancellationToken cancellationToken);

        Task<SendConfigurationResultDto> SendConfigurationToDeviceIotAsync(SendConfigurationToDeviceIot request, CancellationToken cancellationToken, bool rowVersionCheck = true);

        Task<AttributeCommandDto> SendConfigurationToDeviceIotMutipleAsync(IEnumerable<IGrouping<Guid, SendConfigurationToDeviceIot>> assets, CancellationToken cancellationToken, bool rowVersionCheck = true);

        Task<IEnumerable<AssetPathDto>> GetPathsAsync(GetAssetPath request, CancellationToken cancellationToken);

        Task<IEnumerable<Guid>> CheckExistingAssetIdsAsync(CheckExistingAssetIds request, CancellationToken cancellationToken);

        Task<IEnumerable<GetAssetSimpleDto>> GetAssetsByTemplateIdAsync(GetAssetsByTemplateId command, CancellationToken cancellationToken);

        Task<IEnumerable<GetAssetSimpleDto>> GetAssetChildrenAsync(GetAssetChildren command, CancellationToken cancellationToken);

        Task<IEnumerable<ValidateDeviceBindingDto>> ValidateDeviceBindingAsync(ValidateDeviceBindings command, CancellationToken cancellationToken);

        Task<IEnumerable<ArchiveAssetDto>> ArchiveAsync(ArchiveAsset command, CancellationToken cancellationToken);

        Task<BaseResponse> VerifyArchiveAsync(VerifyArchivedAsset command, CancellationToken cancellationToken);

        Task<IDictionary<string, object>> RetrieveAsync(RetrieveAsset request, CancellationToken cancellationToken);

        Task<ValidateAssetResponse> ValidateDependencyAssetAsync(ValidateAsset command, CancellationToken cancellationToken);

        Task<IEnumerable<AttributeDependency>> GetDependencyOfAttributeAsync(IEnumerable<Guid> attributeIds);

        Task<IEnumerable<AttributeDependency>> GetDependencyOfAssetAsync(IEnumerable<Guid> assetIds);

        Task<ActivityResponse> ExportAttributesAsync(ExportAssetAttributes request, CancellationToken cancellationToken);

        Task<AssetAttributeParsedResponse> ParseAssetAttributesAsync(ParseAssetAttributes request, CancellationToken cancellationToken);
    }
}
