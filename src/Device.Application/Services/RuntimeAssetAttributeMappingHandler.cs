using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;

namespace Device.Application.Service
{
    public class RuntimeAssetAttributeMappingHandler : BaseAssetAttributeMappingHandler
    {
        private readonly IAssetUnitOfWork _unitOfWork;

        public RuntimeAssetAttributeMappingHandler(IAssetUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override bool CanApply(string type)
        {
            return type == AttributeTypeConstants.TYPE_RUNTIME;
        }

        /// <summary>
        /// Decorate asset with template attribute.
        /// </summary>
        /// <param name="asset"> processing asset.</param>
        /// <param name="templateAttribute"> processing attribute.</param>
        /// <param name="mappingAttributes"> mapping template attribute id & new asset attribute id.</param>
        /// <param name="mapping"> the mapping.</param>
        /// <param name="isKeepCreatedUtc"> the isKeepCreatedUtc.</param>
        /// <returns>asset Id.</returns>
        protected override async Task<Guid> DecorateAssetWithTemplateAttributeAsync(
            Domain.Entity.Asset asset,
            Domain.Entity.AssetAttributeTemplate templateAttribute,
            IDictionary<Guid, Guid> mappingAttributes,
            AttributeMapping mapping,
            bool? isKeepCreatedUtc = false)
        {
            var mappingEntity = new Domain.Entity.AssetAttributeRuntimeMapping()
            {
                Id = mappingAttributes.ContainsKey(templateAttribute.Id) ? mappingAttributes[templateAttribute.Id] : Guid.NewGuid(),
                AssetId = asset.Id,
                AssetAttributeTemplateId = templateAttribute.Id,
                EnabledExpression = templateAttribute.AssetAttributeRuntime.EnabledExpression,
                SequentialNumber = templateAttribute.SequentialNumber
            };

            if (isKeepCreatedUtc != null && isKeepCreatedUtc.Value)
            {
                mappingEntity.CreatedUtc = templateAttribute.CreatedUtc;
            }

            if (mappingEntity.EnabledExpression)
            {
                var (expression, expressionCompile, matchedAttributes) = await BuildExpressionAsync(asset, templateAttribute, mappingAttributes);
                mappingEntity.Expression = expression;
                mappingEntity.ExpressionCompile = expressionCompile;
                mappingEntity.IsTriggerVisibility = templateAttribute.AssetAttributeRuntime.TriggerAttributeId != null;
                // based on lastest business requirement, trigger attribute will be followed by the same asset
                // az: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/8545
                Guid? triggerAttributeId = null;

                if (mappingEntity.IsTriggerVisibility)
                {
                    var triggerAttributeMapping = asset.AssetAttributeDynamicMappings.Select(x => new { x.AssetAttributeTemplateId, x.Id })
                                                    .Union(asset.AssetAttributeRuntimeMappings.Select(x => new { x.AssetAttributeTemplateId, x.Id }))
                                                    .Union(asset.AssetAttributeIntegrationMappings.Select(x => new { x.AssetAttributeTemplateId, x.Id }))
                                                    .Union(mappingAttributes.Select(x => new { AssetAttributeTemplateId = x.Key, Id = x.Value }));

                    triggerAttributeId = triggerAttributeMapping.First(x => x.AssetAttributeTemplateId == templateAttribute.AssetAttributeRuntime.TriggerAttributeId).Id;

                    if (!matchedAttributes.Contains(triggerAttributeId.Value))
                    {
                        //Triggered by other attributes, not used in Expression.
                        var trigger = new AssetAttributeRuntimeTrigger
                        {
                            AssetId = asset.Id,
                            AttributeId = mappingEntity.Id,
                            TriggerAssetId = asset.Id,
                            TriggerAttributeId = triggerAttributeId.Value,
                            IsSelected = true
                        };
                        _unitOfWork.AssetAttributes.TrackMappingEntity(trigger);
                    }
                }
                foreach (var attributeId in matchedAttributes)
                {
                    var triggerSelected = true;
                    if (triggerAttributeId != null)
                    {
                        triggerSelected = attributeId == triggerAttributeId;
                    }
                    var trigger = new AssetAttributeRuntimeTrigger
                    {
                        AssetId = asset.Id,
                        AttributeId = mappingEntity.Id,
                        TriggerAssetId = asset.Id,
                        TriggerAttributeId = attributeId,
                        IsSelected = triggerSelected
                    };
                    _unitOfWork.AssetAttributes.TrackMappingEntity(trigger);
                }
            }
            asset.AssetAttributeRuntimeMappings.Add(mappingEntity);
            _unitOfWork.AssetAttributes.TrackMappingEntity(mappingEntity);
            return mappingEntity.Id;
        }

        private Task<(string, string, HashSet<Guid>)> BuildExpressionAsync(
            Domain.Entity.Asset asset,
            Domain.Entity.AssetAttributeTemplate attributeTemplate,
            IDictionary<Guid, Guid> mappingAttributes)
        {
            var expression = attributeTemplate.AssetAttributeRuntime.Expression;
            var expressionCompile = attributeTemplate.AssetAttributeRuntime.ExpressionCompile;
            var mappings = asset.AssetAttributeStaticMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id })
            .Union(asset.AssetAttributeDynamicMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeIntegrationMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeAliasMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .Union(asset.AssetAttributeRuntimeMappings.Select(x => new { Key = x.AssetAttributeTemplateId, Value = x.Id }))
            .ToDictionary(x => x.Key, y => y.Value);

            foreach (var item in mappingAttributes)
            {
                if (mappings.ContainsKey(item.Key))
                    mappings[item.Key] = item.Value;
                else
                    mappings.TryAdd(item.Key, item.Value);
            }

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
    }

    internal class AssetAttributeRuntimeMapping
    {
        public Guid? TriggerAssetId { get; set; }
    }
}
