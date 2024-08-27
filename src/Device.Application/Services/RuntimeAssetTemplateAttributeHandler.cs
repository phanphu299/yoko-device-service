using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.ApplicationExtension.Extension;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Exception.Helper;
using Device.Domain.Entity;
using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using AHI.Infrastructure.SharedKernel.Extension;
namespace Device.Application.Service
{
    public class RuntimeAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly IReadUomRepository _readUomRepository;
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly ILoggerAdapter<RuntimeAssetTemplateAttributeHandler> _logger;
        private readonly IDynamicResolver _dynamicResolver;
        public RuntimeAssetTemplateAttributeHandler(
            IDynamicValidator dynamicValidator,
            IReadAssetRepository  readAssetRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            IReadUomRepository readUomRepository,
            IAssetTemplateUnitOfWork unitOfWork,
            IDynamicResolver dynamicResolver,
            ILoggerAdapter<RuntimeAssetTemplateAttributeHandler> logger)
        {
            _dynamicValidator = dynamicValidator;
            _readAssetRepository = readAssetRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _readUomRepository = readUomRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dynamicResolver = dynamicResolver;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeRuntimeTemplate>();
            var entity = AssetTemplateAttribute.Create(attribute);
            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeRuntime = AssetAttributeRuntimeTemplate.Create(dynamicPayload, attribute.AssetTemplate.Id);
            if (!dynamicPayload.EnabledExpression)
            {
                entity.AssetAttributeRuntime.TriggerAttributeId = null;
                entity.AssetAttributeRuntime.Expression = null;
                entity.AssetAttributeRuntime.ExpressionCompile = null;
            }
            else
            {
                await ValidateRuntimeAttribute(entity, attribute, inputAttributes);
            }
            // with mapping asset
            var assets = await _unitOfWork.Assets.AsQueryable()
            .Include(x => x.AssetAttributeStaticMappings)
            .Include(x => x.AssetAttributeDynamicMappings)
            .Include(x => x.AssetAttributeRuntimeMappings).ThenInclude(x => x.AssetAttributeRuntimeTemplate)
            .Include(x => x.AssetAttributeIntegrationMappings)
            .Include(x => x.AssetAttributeAliasMappings)
            .Where(x => x.AssetTemplateId == entity.AssetTemplateId).ToListAsync();
            var assetAttributeRuntimeMappings = new List<Domain.Entity.AssetAttributeRuntimeMapping>();
            foreach (var asset in assets)
            {
                var runtimeMapping = new Domain.Entity.AssetAttributeRuntimeMapping()
                {
                    AssetId = asset.Id,
                    AssetAttributeTemplateId = entity.Id,
                    EnabledExpression = dynamicPayload.EnabledExpression
                };
                if (dynamicPayload.EnabledExpression)
                {
                    var (expression, expressionCompile, matchedAttributes) = await BuildExpressionAsync(asset, entity);
                    // var assetMapping = asset.AssetAttributeRuntimeMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == assetTemplateRuntimeAttribute?.AssetAttributeTemplateId && dynamicPayload.MarkupName == x.AssetAttributeRuntimeTemplate.MarkupName);

                    runtimeMapping.Expression = expression;
                    runtimeMapping.ExpressionCompile = expressionCompile;
                    runtimeMapping.IsTriggerVisibility = entity.AssetAttributeRuntime.TriggerAttributeId != null;

                    ProcessRuntimeTrigger(runtimeMapping, runtimeMapping.Triggers, asset, entity, matchedAttributes);
                }

                assetAttributeRuntimeMappings.Add(runtimeMapping);
            }
            entity.AssetAttributeRuntimeMappings = assetAttributeRuntimeMappings;
            await ValidateExistUomByIdAsync(entity);
            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }
        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeRuntimeTemplate>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = await _unitOfWork.Attributes.AsQueryable().Include(x => x.AssetAttributeRuntime).FirstAsync(x => x.Id == attribute.Id);
            entity.Name = attribute.Name;
            entity.UomId = attribute.UomId;
            entity.DataType = attribute.DataType;
            entity.ThousandSeparator = attribute.ThousandSeparator;
            entity.DecimalPlace = attribute.DecimalPlace;
            entity.AssetAttributeRuntime.Expression = dynamicPayload.Expression;
            entity.AssetAttributeRuntime.TriggerAttributeId = dynamicPayload.TriggerAttributeId;
            entity.AssetAttributeRuntime.EnabledExpression = dynamicPayload.EnabledExpression;
            await ValidateExistUomByIdAsync(entity);

            if (dynamicPayload.EnabledExpression)
            {
                await ValidateRuntimeAttribute(entity, attribute, inputAttributes);

                var assets = await _unitOfWork.Assets.AsQueryable()
                .Include(x => x.AssetAttributeStaticMappings)
                .Include(x => x.AssetAttributeDynamicMappings)
                .Include(x => x.AssetAttributeRuntimeMappings).ThenInclude(x => x.Triggers)
                .Include(x => x.AssetAttributeIntegrationMappings)
                .Include(x => x.AssetAttributeAliasMappings)
                .Where(x => x.AssetTemplateId == entity.AssetTemplateId).ToListAsync();

                var triggers = new List<AssetAttributeRuntimeTrigger>();

                foreach (var asset in assets)
                {
                    var (expression, expressionCompile, matchedAttributes) = await BuildExpressionAsync(asset, entity);
                    foreach (var runtimeMapping in asset.AssetAttributeRuntimeMappings.Where(x => x.AssetAttributeTemplateId == entity.Id))
                    {
                        runtimeMapping.EnabledExpression = true;
                        runtimeMapping.Expression = expression;
                        runtimeMapping.ExpressionCompile = expressionCompile;
                        runtimeMapping.IsTriggerVisibility = dynamicPayload.TriggerAttributeId != null;
                        runtimeMapping.Triggers.Clear();
                        ProcessRuntimeTrigger(runtimeMapping, triggers, asset, entity, matchedAttributes);
                    }
                }

                await _unitOfWork.AssetAttributes.AddAssetRuntimeAttributeTriggersAsync(triggers);
            }
            else
            {
                var assets = await _unitOfWork.Assets.AsQueryable().Include(x => x.AssetAttributeRuntimeMappings).Where(x => x.AssetTemplateId == entity.AssetTemplateId).ToListAsync();
                foreach (var asset in assets)
                {
                    foreach (var runtimeMapping in asset.AssetAttributeRuntimeMappings.Where(x => x.AssetAttributeTemplateId == entity.Id))
                    {
                        runtimeMapping.EnabledExpression = false;
                        runtimeMapping.Expression = null;
                        runtimeMapping.ExpressionCompile = null;
                        runtimeMapping.Triggers.Clear();
                    }
                }
            }
            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }

        private void ProcessRuntimeTrigger(
            Domain.Entity.AssetAttributeRuntimeMapping runtimeMapping,
            ICollection<AssetAttributeRuntimeTrigger> triggers,
            Domain.Entity.Asset asset,
            Domain.Entity.AssetAttributeTemplate attribute,
            IEnumerable<Guid> matchedAttributes)
        {
            Guid? triggerAttributeId = null;
            if (runtimeMapping.IsTriggerVisibility)
            {
                var attributeIds = asset.AssetAttributeDynamicMappings.Select(x => (x.AssetAttributeTemplateId, x.Id))
                                            .Union(asset.AssetAttributeRuntimeMappings.Select(x => (x.AssetAttributeTemplateId, x.Id)))
                                            .Union(asset.AssetAttributeIntegrationMappings.Select(x => (x.AssetAttributeTemplateId, x.Id)))
                                            .Union(asset.AssetAttributeStaticMappings.Select(x => (x.AssetAttributeTemplateId, x.Id)));
                triggerAttributeId = attributeIds.First(x => x.AssetAttributeTemplateId == attribute.AssetAttributeRuntime.TriggerAttributeId).Id;

                if (!matchedAttributes.Contains(triggerAttributeId.Value))
                {
                    //Triggered by other attributes, not used in Expression.
                    triggers.Add(new AssetAttributeRuntimeTrigger()
                    {
                        AssetId = asset.Id,
                        AttributeId = runtimeMapping.Id,
                        TriggerAssetId = asset.Id,
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
                    AssetId = asset.Id,
                    AttributeId = runtimeMapping.Id,
                    TriggerAssetId = asset.Id,
                    TriggerAttributeId = attributeId,
                    IsSelected = triggerSelected
                });
            }
            // else
            // {
            //     foreach (var triggerAttributeId in matchedAttributes)
            //     {
            //         triggers.Add(new AssetAttributeRuntimeTrigger()
            //         {
            //             AssetId = asset.Id,
            //             AttributeId = runtimeMapping.Id,
            //             TriggerAssetId = asset.Id,
            //             TriggerAttributeId = triggerAttributeId
            //         });
            //     }
            // }
        }

        private async Task ValidateRuntimeAttribute(Domain.Entity.AssetAttributeTemplate entity, AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes)
        {
            var assetTemplateAttributes = await _readAssetAttributeTemplateRepository.AsQueryable().AsNoTracking().Where(x => x.AssetTemplateId == entity.AssetTemplateId).ToListAsync();
            var expressionRelationAttributes = assetTemplateAttributes.Select(x => new { Id = x.Id, DataType = x.DataType, AttributeType = x.AttributeType });
            // asset can be null due to clone operation, need to fallback into Memory of EF core
            var unsaveAttributes = _unitOfWork.Attributes.UnSaveAttributes;
            var localAttributes = unsaveAttributes.Select(x => new { Id = x.Id, DataType = x.DataType, AttributeType = x.AttributeType }).ToList();
            var attributes = inputAttributes.Select(x => new AssetTemplateAttributeValidationRequest()
            {
                Id = x.Id,
                DataType = x.DataType,
                AttributeType = x.AttributeType
            })
            .ToList();
            foreach (var localAttribute in localAttributes)
            {
                attributes.Add(new AssetTemplateAttributeValidationRequest()
                {
                    Id = localAttribute.Id,
                    DataType = localAttribute.DataType,
                    AttributeType = localAttribute.AttributeType
                });
            }
            foreach (var attr in expressionRelationAttributes)
            {
                if (!attributes.Any(x => x.Id == attr.Id))
                {
                    attributes.Add(new AssetTemplateAttributeValidationRequest()
                    {
                        Id = attr.Id,
                        DataType = attr.DataType,
                        AttributeType = attr.AttributeType
                    });
                }
            }
            //when create template from asset, there is no attribute in db
            foreach (var attr in attribute.AssetTemplate.Attributes)
            {
                if (!attributes.Any(x => x.Id == attr.Id))
                {
                    attributes.Add(new AssetTemplateAttributeValidationRequest()
                    {
                        Id = attr.Id,
                        DataType = attr.DataType
                    });
                }
            }

            var request = new AssetTemplateAttributeValidationRequest()
            {
                Id = entity.Id,
                Expression = entity.AssetAttributeRuntime.Expression,
                DataType = entity.DataType,
                Attributes = attributes.Distinct()
            };
            var (validateResult, expression) = ValidateExpression(request);
            if (!validateResult)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttributeRuntimeTemplate.Expression));
            }

            entity.AssetAttributeRuntime.ExpressionCompile = expression;

            // if assetTemplateAttributes is null or empty that means asset template is created from asset
            if (entity.AssetAttributeRuntime.TriggerAttributeId.HasValue && assetTemplateAttributes != null && assetTemplateAttributes.Any())
            {
                var existInInputAttrs = inputAttributes.Any(attr => attr.Id == entity.AssetAttributeRuntime.TriggerAttributeId);
                var exist = assetTemplateAttributes.Union(unsaveAttributes).Any(x => x.AssetTemplateId == entity.AssetTemplateId && x.Id == entity.AssetAttributeRuntime.TriggerAttributeId);
                if (!exist && !existInInputAttrs)
                {
                    throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(entity.AssetAttributeRuntime.TriggerAttributeId));
                }
            }
        }
        public (bool, string) ValidateExpression(AssetTemplateAttributeValidationRequest request)
        {
            var expressionValidate = request.Expression;
            // need to make sure the expression is valid and compile
            // *** TODO: NOW VALUE WILL NOT IN VALUE COLUMN ==> now alway true
            if (string.IsNullOrWhiteSpace(expressionValidate))
                return (false, null);

            //get metric in expresstion : {a}*2+{b} => {}
            ICollection<Guid> assetIds = new List<Guid>();
            Match m = Regex.Match(expressionValidate, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            //get metric name in expression
            while (m.Success)
            {
                Guid idProperty;
                if (!Guid.TryParse(m.Value.Replace("${", "").Replace("}$", "").Trim(), out idProperty))
                {
                    return (false, null);
                }
                if (!assetIds.Contains(idProperty))
                    assetIds.Add(idProperty);
                m = m.NextMatch();
            }
            if (assetIds.Contains(request.Id))
            {
                // cannot self-reference in the expression.
                return (false, null);
            }
            var dictionary = new Dictionary<string, object>();
            expressionValidate = BuildExpression(expressionValidate, request, dictionary);
            if (request.DataType == DataTypeConstants.TYPE_TEXT)
            {
                //if expression contain special character, we need to escape it one more time
                if (!request.Attributes.Any(x => expressionValidate.Contains($"request[\"{x.Id.ToString()}\"]")))
                {
                    expressionValidate = expressionValidate.ToJson();
                }
            }
            if (!expressionValidate.Contains("return "))
            {
                expressionValidate = $"return {expressionValidate}";
            }
            try
            {
                _logger.LogTrace(expressionValidate);
                var value = _dynamicResolver.ResolveInstance("return true;", expressionValidate).OnApply(dictionary);
                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    var result = value.ParseResultWithDataType(request.DataType);
                    return (result, expressionValidate);
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
            return (false, null);
        }

        private string BuildExpression(string expressionValidate, AssetTemplateAttributeValidationRequest request, IDictionary<string, object> dictionary)
        {
            foreach (var element in request.Attributes)
            {
                //Remap data type if target attribute is alias. 
                var dataTypeName = (element.AttributeType == AttributeTypeConstants.TYPE_ALIAS && element.DataType == null && request.DataType != null) ? request.DataType : element.DataType;
                object value = null;
                switch (dataTypeName.ToLower())
                {
                    case DataTypeConstants.TYPE_DOUBLE:
                        expressionValidate = expressionValidate.Replace($"${{{element.Id}}}$", $"Convert.ToDouble(request[\"{element.Id}\"])");
                        value = (double)1.0;
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


        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_RUNTIME;
        }
        private Task<(string, string, HashSet<Guid>)> BuildExpressionAsync(Domain.Entity.Asset asset, Domain.Entity.AssetAttributeTemplate attributeTemplate)
        {
            var expression = attributeTemplate.AssetAttributeRuntime.Expression;
            var expressionCompile = attributeTemplate.AssetAttributeRuntime.ExpressionCompile;
            var mappings = asset.AssetAttributeStaticMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id })
            .Union(asset.AssetAttributeDynamicMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeIntegrationMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeAliasMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeRuntimeMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .ToDictionary(x => x.Key, y => y.Value);
            foreach (var element in mappings)
            {
                expression = expression.Replace(element.Key.ToString(), mappings[element.Key].ToString());
                expressionCompile = expressionCompile.Replace(element.Key.ToString(), mappings[element.Key].ToString());
            }
            HashSet<Guid> matchedAttributes = new HashSet<Guid>();
            Match m = Regex.Match(expression, RegexConstants.PATTERN_EXPRESSION_KEY, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));
            while (m.Success)
            {
                if (Guid.TryParse(m.Groups[1].Value, out var idProperty))
                {
                    if (!matchedAttributes.Contains(idProperty))
                        matchedAttributes.Add(idProperty);
                }
                m = m.NextMatch();
            }
            return Task.FromResult((expression, expressionCompile, matchedAttributes));
        }

        private async Task ValidateExistUomByIdAsync(Domain.Entity.AssetAttributeTemplate attribute)
        {
            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }
        }
    }
    internal class AssetAttributeRuntimeTemplate
    {
        public Guid? TriggerAttributeId { get; set; }
        public string Expression { get; set; }
        public bool EnabledExpression { get; set; }

        internal static Domain.Entity.AssetAttributeRuntimeTemplate Create(AssetAttributeRuntimeTemplate dynamicPayload, Guid assetTemplateId)
        {
            return new Domain.Entity.AssetAttributeRuntimeTemplate()
            {
                TriggerAttributeId = dynamicPayload.TriggerAttributeId,
                Expression = dynamicPayload.Expression,
                EnabledExpression = dynamicPayload.EnabledExpression
            };
        }
    }
}