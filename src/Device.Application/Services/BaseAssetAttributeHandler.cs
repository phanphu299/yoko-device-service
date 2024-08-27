using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Audit.Service.Abstraction;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public abstract class BaseAssetAttributeHandler : IAssetAttributeHandler
    {
        private IAssetAttributeHandler _next;
        protected readonly IAssetUnitOfWork _unitOfWork;
        protected readonly IAuditLogService _auditLogService;
        protected readonly IDomainEventDispatcher _domainEventDispatcher;
        protected readonly ITenantContext _tenantContext;
        protected readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        protected readonly IReadAssetRepository _readAssetRepository;
        protected readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;

        public BaseAssetAttributeHandler(IAssetUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository)
        {
            _unitOfWork = unitOfWork;
            _auditLogService = auditLogService;
            _domainEventDispatcher = domainEventDispatcher;
            _tenantContext = tenantContext;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetRepository = readAssetRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
        }

        public void SetNextHandler(IAssetAttributeHandler next)
        {
            _next = next;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        public async Task<Domain.Entity.AssetAttribute> AddAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            if (CanApply(attribute.AttributeType))
            {
                if (await IsDuplicatedAssetAttributeNameAsync(attribute.Name, attribute.AssetId) && !attribute.IsStandalone)
                {
                    throw ValidationExceptionHelper.GenerateDuplicateValidation(nameof(Domain.Entity.AssetAttribute.Name));
                }
                var entity = await AddAttributeAsync(attribute, inputAttributes, cancellationToken, ignoreValidation);
                return entity;
            }
            else if (_next != null)
            {
                return await _next.AddAsync(attribute, inputAttributes, cancellationToken, ignoreValidation);
            }
            throw new SystemNotSupportedException(detailCode: MessageConstants.ASSET_ATTRIBUTE_TYPE_INVALID);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        public async Task<Domain.Entity.AssetAttribute> UpdateAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            if (CanApply(attribute.AttributeType))
            {
                var entity = await UpdateAttributeAsync(attribute, inputAttributes, cancellationToken);

                return entity;
            }
            else if (_next != null)
            {
                return await _next.UpdateAsync(attribute, inputAttributes, cancellationToken);
            }
            throw new SystemNotSupportedException(detailCode: MessageConstants.ASSET_ATTRIBUTE_TYPE_INVALID);
        }

        private async Task<bool> IsDuplicatedAssetAttributeNameAsync(string name, Guid assetId)
        {
            var isDbDuplicateAssetAttributeName = await _readAssetAttributeRepository
                                                                    .AsQueryable()
                                                                    .AsNoTracking()
                                                                    .AnyAsync(x => x.Name.ToLower() == name.ToLower() && x.AssetId == assetId);
            var isLocalDuplicateAssetAttributeName = _unitOfWork.AssetAttributes
                                                                    .UnSaveAttributes
                                                                    .Any(x => x.Name.ToLower() == name.ToLower() && x.AssetId == assetId);
            var isDuplicateAssetAttributeName = isDbDuplicateAssetAttributeName && isLocalDuplicateAssetAttributeName;
            if (!isDuplicateAssetAttributeName)
            {
                var assetEntity = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().Where(x => x.Id == assetId).FirstOrDefaultAsync();
                if (assetEntity != null && assetEntity.AssetTemplateId != null)
                {
                    // need to fallback to asset template attribute in case the asset using template
                    // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/4021
                    isDuplicateAssetAttributeName = await _readAssetAttributeTemplateRepository
                                                                    .AsQueryable()
                                                                    .AsNoTracking()
                                                                    .Where(x => x.Name.ToLower() == name.ToLower() && x.AssetTemplateId == assetEntity.AssetTemplateId)
                                                                    .AnyAsync();
                }
            }
            return isDuplicateAssetAttributeName;
        }

        protected abstract Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false);

        public abstract bool CanApply(string attributeType);

        protected abstract Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken);
    }
}