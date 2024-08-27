using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command.Model;
using Device.Application.AssetTemplate.Command;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Models;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetTemplateService : ISearchService<Domain.Entity.AssetTemplate, Guid, GetAssetTemplateByCriteria, GetAssetTemplateDto>, IFetchService<Domain.Entity.AssetTemplate, Guid, GetAssetTemplateDto>
    {
        Task<GetAssetTemplateDto> FindTemplateByIdAsync(GetAssetTemplateById command, CancellationToken cancellationToken);

        Task<AddAssetTemplateDto> AddAssetTemplateAsync(AddAssetTemplate command, CancellationToken cancellationToken);

        Task<AddAssetTemplateDto> CreateAssetTemplateFromAssetAsync(CreateAssetTemplateFromAsset command, CancellationToken cancellationToken);

        Task<UpdateAssetTemplateDto> UpdateAssetTemplateAsync(UpdateAssetTemplate command, CancellationToken cancellationToken);

        Task<bool> RemoveAssetTemplateAsync(DeleteAssetTemplate command, CancellationToken cancellationToken);

        Task<ActivityResponse> ExportAssetTemplateAsync(ExportAssetTemplate request, CancellationToken cancellationToken);

        Task<ActivityResponse> ExportAssetTemplateAttributeAsync(ExportAssetTemplateAttribute request, CancellationToken cancellationToken);

        // #region concurency
        // Task<LockAssetDto> LockAssetTemplateAsync(LockAssetTemplate command, CancellationToken token);
        // //Task<ForceLockAssetDto> ForceLockAssetTemplateAsync(ForceLockAssetTemplate command, CancellationToken token);
        // Task<TakeLockAssetDto> RequestUnlockAssetTemplateAsync(RequestUnlockAssetTemplate command, CancellationToken token);
        // Task<AcceptUnlockAssetRequestDto> AcceptUnlockAssetTemplateAsync(AcceptUnlockAssetTemplate command, CancellationToken token);
        // Task<BaseResponse> RejectUnlockAssetTemplateAsync(RejectUnlockAssetTemplate command, CancellationToken token);
        // Task<BaseResponse> UnlockAssetTemplateAsync(UnlockAssetTemplate command, CancellationToken token);
        // #endregion
        Task<BaseResponse> CheckExistingAssetTemplateAsync(CheckExistingAssetTemplate command, CancellationToken cancellationToken);

        Task<IEnumerable<ArchiveAssetTemplateDto>> ArchiveAsync(ArchiveAssetTemplate command, CancellationToken cancellationToken);

        Task<BaseResponse> VerifyArchiveAsync(VerifyAssetTemplate command, CancellationToken cancellationToken);

        Task<BaseResponse> RetrieveAsync(RetrieveAssetTemplate command, CancellationToken cancellationToken);

        Task<ValidateAssetResponse> ValidateDeleteTemplateAsync(Guid attributeTemplateId);

        Task<BaseResponse> CheckUsingAttributeAsync(Guid attributeTemplateId);

        Task<bool> IsDuplicationAttributeNameAsync(Guid assetTemplateId, IEnumerable<string> attributeNames);

        Task<AttributeTemplateParsed> ParseAttributeTemplateAsync(ParseAttributeTemplate request, CancellationToken cancellationToken);
    }
}
