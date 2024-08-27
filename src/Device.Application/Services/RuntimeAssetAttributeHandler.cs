using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.ApplicationExtension.Extension;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using AHI.Infrastructure.Exception;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Exception.Helper;
using Device.Domain.Entity;
using Device.Application.Asset.Command.Model;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;

namespace Device.Application.Service
{
    public class RuntimeAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly ILoggerAdapter<RuntimeAssetAttributeHandler> _logger;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly IReadUomRepository _readUomRepository;

        public RuntimeAssetAttributeHandler(
            ILoggerAdapter<RuntimeAssetAttributeHandler> logger,
            IDynamicValidator dynamicValidator,
            IAssetUnitOfWork repository,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            IDynamicResolver dynamicResolver,
            ITenantContext tenantContext,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadUomRepository readUomRepository,
            IReadAssetRepository readAssetRepository) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _logger = logger;
            _dynamicResolver = dynamicResolver;
            _dynamicValidator = dynamicValidator;
            _readUomRepository = readUomRepository;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeRuntime>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeRuntime = new Domain.Entity.AssetAttributeRuntime()
            {
                AssetAttributeId = attribute.Id,
                IsTriggerVisibility = dynamicPayload.TriggerAttributeId != null,
                EnabledExpression = dynamicPayload.EnabledExpression,
                Expression = dynamicPayload.Expression
            };

            if (!ignoreValidation)
            {
                if (entity.AssetAttributeRuntime.EnabledExpression)
                {
                    await ValidateRuntimeAttribute(entity, inputAttributes, dynamicPayload);
                }
                await ValidateExistUomByIdAsync(entity);
            }

            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeRuntime>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.AssetAttributeRuntime = new Domain.Entity.AssetAttributeRuntime()
            {
                AssetAttributeId = attribute.Id,
                IsTriggerVisibility = dynamicPayload.TriggerAttributeId != null,
                EnabledExpression = dynamicPayload.EnabledExpression,
                Expression = dynamicPayload.Expression
            };
            if (entity.AssetAttributeRuntime.EnabledExpression)
            {
                await ValidateRuntimeAttribute(entity, inputAttributes, dynamicPayload);
            }

            await ValidateExistUomByIdAsync(entity);
            var asset = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.AssetId);
            if (asset == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttribute.AssetId));
            }

            if (asset.AssetTemplateId.HasValue)
            {
                var runtimeMapping = _unitOfWork.Assets.AsQueryable().Where(x => x.Id == asset.Id).SelectMany(x => x.AssetAttributeRuntimeMappings).FirstOrDefault(x => x.Id == entity.Id);

                if (runtimeMapping != null)
                {
                    runtimeMapping.Expression = entity.AssetAttributeRuntime.Expression;
                    runtimeMapping.ExpressionCompile = entity.AssetAttributeRuntime.ExpressionCompile;
                    _unitOfWork.AssetAttributes.TrackMappingEntity(runtimeMapping, EntityState.Modified);
                    return entity;
                }
            }

            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().Include(x => x.AssetAttributeRuntime).FirstOrDefaultAsync(x => x.Id == entity.Id);

            if (trackingEntity == null)
                throw new EntityNotFoundException();

            _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
            trackingEntity.AssetAttributeRuntime.Expression = entity.AssetAttributeRuntime.Expression;
            trackingEntity.AssetAttributeRuntime.ExpressionCompile = entity.AssetAttributeRuntime.ExpressionCompile;
            trackingEntity.AssetAttributeRuntime.EnabledExpression = entity.AssetAttributeRuntime.EnabledExpression;
            trackingEntity.AssetAttributeRuntime.IsTriggerVisibility = entity.AssetAttributeRuntime.IsTriggerVisibility;
            trackingEntity.AssetAttributeRuntime.UpdatedUtc = DateTime.UtcNow;

            return await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
        }

        private async Task ValidateRuntimeAttribute(Domain.Entity.AssetAttribute entity, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, AssetAttributeRuntime dynamicPayload)
        {
            var assetId = entity.AssetId;

            var targetValidateAttributes = new List<AssetTemplateAttributeValidationRequest>();
            var unsaveAttributes = _unitOfWork.AssetAttributes.UnSaveAttributes;
            if (unsaveAttributes.Any())
            {
                targetValidateAttributes.AddRange(unsaveAttributes.Select(item => new AssetTemplateAttributeValidationRequest()
                {
                    Id = item.Id,
                    DataType = item.DataType
                }));
            }

            var aliasAndTargetAliasPairs = await GetAliasTargetMappings(assetId);
            var relatedTargets = aliasAndTargetAliasPairs.Select(ar => ar.TargetAliasAttributeId);

            var targetAttributes = await _readAssetAttributeRepository.AsQueryable().AsNoTracking()
                                    .Where(x => relatedTargets.Contains(x.Id)).ToListAsync();

            var remapAliasAttribute = (from tg in aliasAndTargetAliasPairs
                                       join at in targetAttributes on tg.TargetAliasAttributeId equals at.Id
                                       select new { Id = tg.AliasAttributeId, DataType = at.DataType, at.Value });

            var asset = await _unitOfWork.Assets.AsQueryable()
            .Include(x => x.Attributes)
            .Include(x => x.AssetAttributeStaticMappings).ThenInclude(x => x.AssetAttributeTemplate)
            .Include(x => x.AssetAttributeRuntimeMappings).ThenInclude(x => x.AssetAttributeTemplate)
            .Include(x => x.AssetAttributeDynamicMappings).ThenInclude(x => x.AssetAttributeTemplate)
            .Where(x => x.Id == assetId).FirstOrDefaultAsync();

            var localAsset = _unitOfWork.Assets.UnSaveAssets.Where(x => x.Id == assetId).First();
            if (asset == null)
            {
                // asset can be null due to clone operation, need to fallback into Memory of EF core
                asset = localAsset;
            }

            var attributes = asset.Attributes.Select(x => new { x.Id, DataType = x.DataType, x.Value })
            .Union(asset.AssetAttributeStaticMappings.Select(x => new { x.Id, DataType = x.AssetAttributeTemplate.DataType, x.AssetAttributeTemplate.Value }))
            .Union(asset.AssetAttributeRuntimeMappings.Select(x => new { x.Id, DataType = x.AssetAttributeTemplate.DataType, x.AssetAttributeTemplate.Value }))
            .Union(asset.AssetAttributeDynamicMappings.Select(x => new { x.Id, DataType = x.AssetAttributeTemplate.DataType, x.AssetAttributeTemplate.Value }));

            attributes = attributes.Select(x =>
            {
                var newAttribute = remapAliasAttribute.FirstOrDefault(r => r.Id == x.Id);
                if (newAttribute != null)
                {
                    return newAttribute;
                }
                var localAttribute = localAsset.Attributes.FirstOrDefault(r => r.Id == x.Id);
                if (localAttribute != null)
                {
                    // need to update the dataType
                    // https://dev.azure.com/ThanhTrungBui/yokogawa-ppm/_workitems/edit/16502
                    return new { x.Id, DataType = localAttribute.DataType, localAttribute.Value };
                }
                return x;
            });
            targetValidateAttributes.AddRange(attributes.Select(att => new AssetTemplateAttributeValidationRequest()
            {
                Id = att.Id,
                DataType = att.DataType
            }));

            targetValidateAttributes.AddRange(inputAttributes.Select(att => new AssetTemplateAttributeValidationRequest()
            {
                Id = att.Id,
                DataType = att.DataType
            }));
            var request = new AssetTemplateAttributeValidationRequest()
            {
                Id = entity.Id,
                DataType = entity.DataType,
                Expression = entity.AssetAttributeRuntime.Expression,
                Attributes = targetValidateAttributes
            };
            var (validateResult, expression, matchedAttributes) = ValidateExpression(request);
            if (!validateResult)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttributeRuntime.Expression));
            }
            entity.AssetAttributeRuntime.ExpressionCompile = expression;
            entity.AssetAttributeRuntime.Triggers.Clear();

            await _unitOfWork.AssetAttributes.RemoveAssetRuntimeAttributeTriggersAsync(entity.Id);

            var attrs = unsaveAttributes.Where(x => x.AssetId == assetId).Select(x => new { x.Id, DataType = x.DataType, x.Value }).Union(attributes).ToList();
            attrs.AddRange(inputAttributes.Select(x => new { x.Id, x.DataType, x.Value }));
            var triggers = CreateRuntimeTriggers(entity, dynamicPayload, matchedAttributes, attrs.Select(x => x.Id).Distinct());

            await _unitOfWork.AssetAttributes.AddAssetRuntimeAttributeTriggersAsync(triggers);
        }

        private async Task<IEnumerable<(Guid AliasAttributeId, Guid TargetAliasAttributeId)>> GetAliasTargetMappings(Guid assetId)
        {
            var validAliasAssetAttributes = await _readAssetAttributeRepository.AsQueryable().AsNoTracking()
                .Where(x => x.AssetId == assetId && x.AttributeType == AttributeTypeConstants.TYPE_ALIAS).ToListAsync();

            var aliasAndTargetAliasPairs = new List<(Guid AliasAttributeId, Guid TargetAliasAttributeId)>();

            foreach (var assetAttributeId in validAliasAssetAttributes.Select(x => x.Id))
            {
                var targetAliasId = await _unitOfWork.Alias.GetTargetAliasAttributeIdAsync(assetAttributeId);
                if (targetAliasId == null)
                {
                    continue;
                }
                (Guid AliasAttributeId, Guid TargetAliasAttributeId) pair = (assetAttributeId, targetAliasId.Value);
                aliasAndTargetAliasPairs.Add(pair);
            }
            return aliasAndTargetAliasPairs;
        }

        private IEnumerable<AssetAttributeRuntimeTrigger> CreateRuntimeTriggers(
            Domain.Entity.AssetAttribute entity,
            AssetAttributeRuntime dynamicPayload,
            IEnumerable<Guid> matchedAttributes,
            IEnumerable<Guid> triggerAttributeIds
        )
        {
            var triggers = new List<AssetAttributeRuntimeTrigger>();
            Guid? triggerAttributeId = null;
            if (entity.AssetAttributeRuntime.IsTriggerVisibility)
            {
                var exist = triggerAttributeIds.Contains(dynamicPayload.TriggerAttributeId.Value);

                if (!exist)
                {
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(dynamicPayload.TriggerAttributeId));
                }
                triggerAttributeId = dynamicPayload.TriggerAttributeId.Value;

                if (!matchedAttributes.Contains(triggerAttributeId.Value))
                {
                    //Triggered by other attributes, not used in Expression.
                    triggers.Add(new AssetAttributeRuntimeTrigger()
                    {
                        AssetId = entity.AssetId,
                        AttributeId = entity.Id,
                        TriggerAssetId = entity.AssetId,
                        TriggerAttributeId = triggerAttributeId.Value,
                        IsSelected = true
                    });
                }
            }

            foreach (var attributeId in matchedAttributes)
            {
                var triggerSelected = true;
                if (triggerAttributeId != null)
                {
                    triggerSelected = attributeId == triggerAttributeId;
                }
                triggers.Add(new AssetAttributeRuntimeTrigger()
                {
                    AssetId = entity.AssetId,
                    AttributeId = entity.Id,
                    TriggerAssetId = entity.AssetId,
                    TriggerAttributeId = attributeId,
                    IsSelected = triggerSelected
                });
            }
            return triggers;
        }

        public (bool, string, HashSet<Guid>) ValidateExpression(AssetTemplateAttributeValidationRequest request)
        {
            var expressionValidate = request.Expression;

            // *** TODO: NOW VALUE WILL NOT IN VALUE COLUMN ==> now alway true
            if (string.IsNullOrWhiteSpace(expressionValidate))
                return (false, null, null);

            HashSet<Guid> matchedAttributes = new HashSet<Guid>();
            TryParseIdProperty(expressionValidate, matchedAttributes);
            if (matchedAttributes.Contains(request.Id))
            {
                // cannot self reference
                return (false, null, null);
            }

            //must not include command attribute in expression
            if (request.Attributes.Any(x => matchedAttributes.Contains(x.Id) && x.AttributeType == AttributeTypeConstants.TYPE_COMMAND))
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(AssetAttributeRuntime.Expression));
            }

            if (matchedAttributes.Any(id => !request.Attributes.Select(t => t.Id).Contains(id)))
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(ErrorPropertyConstants.AssetAttribute.ASSET_ATTRIBUTE_ID);
            }

            var dataType = request.DataType;
            var dictionary = new Dictionary<string, object>();
            expressionValidate = BuildExpression(expressionValidate, request, dictionary);

            if (dataType == DataTypeConstants.TYPE_TEXT
                && !request.Attributes.Any(x => expressionValidate.Contains($"request[\"{x.Id}\"]")))
            {
                //if expression contain special character, we need to escape it one more time
                expressionValidate = expressionValidate.ToJson();
            }

            if (!expressionValidate.Contains("return "))
            {
                expressionValidate = $"return {expressionValidate}";
            }

            try
            {
                _logger.LogTrace(expressionValidate);
                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value.ToString()))
                {
                    var result = value.ParseResultWithDataType(dataType);
                    return (result, expressionValidate, matchedAttributes);
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
            return (false, null, null);
        }

        private bool TryParseIdProperty(string expressionValidate, HashSet<Guid> matchedAttributes)
        {
            Match m = Regex.Match(expressionValidate, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            while (m.Success)
            {
                if (!Guid.TryParse(m.Groups[1].Value, out var idProperty))
                    return false;
                if (!matchedAttributes.Contains(idProperty))
                    matchedAttributes.Add(idProperty);
                m = m.NextMatch();
            }
            return true;
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_RUNTIME;
        }

        private async Task ValidateExistUomByIdAsync(Domain.Entity.AssetAttribute attribute)
        {
            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }
        }

        private string BuildExpression(string expressionValidate, AssetTemplateAttributeValidationRequest request, Dictionary<string, object> dictionary)
        {
            foreach (var element in request.Attributes)
            {
                object value = null;
                switch (element.DataType?.ToLower())
                {
                    case DataTypeConstants.TYPE_DOUBLE:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDouble(request[\"{element.Id}\"])");
                        value = 1.0;
                        break;
                    case DataTypeConstants.TYPE_INTEGER:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToInt32(request[\"{element.Id}\"])");
                        value = 1;
                        break;
                    case DataTypeConstants.TYPE_BOOLEAN:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToBoolean(request[\"{element.Id}\"])");
                        value = true;
                        break;
                    case DataTypeConstants.TYPE_TIMESTAMP:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDouble(request[\"{element.Id}\"])");
                        value = (double)1;
                        break;
                    case DataTypeConstants.TYPE_DATETIME:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDateTime(request[\"{element.Id}\"])");
                        value = new DateTime(1970, 1, 1);
                        break;
                    case DataTypeConstants.TYPE_TEXT:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"request[\"{element.Id}\"].ToString()");
                        value = "default";
                        break;
                }
                dictionary[element.Id.ToString()] = value;
            }
            return expressionValidate;
        }
    }

    internal class AssetAttributeRuntime
    {
        public Guid? TriggerAttributeId { get; set; }
        public string Expression { get; set; }
        public bool EnabledExpression { get; set; }
        public string ExpressionCompile { get; set; }
    }
}