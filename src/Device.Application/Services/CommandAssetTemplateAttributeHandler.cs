using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Repository;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class CommandAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadAssetTemplateRepository _readAssetTemplateRepository;
        private readonly IReadDeviceTemplateRepository _readDeviceTemplateRepository;

        public CommandAssetTemplateAttributeHandler(IDynamicValidator dynamicValidator
            , IAssetTemplateUnitOfWork unitOfWork
            , IReadAssetAttributeRepository readAssetAttributeRepository
            , IReadAssetRepository readAssetRepository
            , IReadAssetTemplateRepository readAssetTemplateRepository
            , IReadDeviceTemplateRepository readDeviceTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _unitOfWork = unitOfWork;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetRepository = readAssetRepository;
            _readAssetTemplateRepository = readAssetTemplateRepository;
            _readDeviceTemplateRepository = readDeviceTemplateRepository;
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_COMMAND;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeCommandTemplate>();
            await _dynamicValidator.ValidateAsync(commandPayload, cancellationToken);
            var entity = AssetTemplateAttribute.Create(attribute);
            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeCommand = AssetAttributeCommandTemplate.Create(commandPayload);
            await ValidateCommandAttributeAsync(entity);

            // with mapping asset
            var assets = await _readAssetRepository
                            .OnlyAssetAsQueryable()
                            .Include(x => x.AssetAttributeCommandMappings).ThenInclude(x => x.AssetAttributeCommandTemplate)
                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                            .ToListAsync();
            var assetAttributeCommandMappings = new List<Domain.Entity.AssetAttributeCommandMapping>();
            var assetTemplateCommandAttribute = await _readAssetTemplateRepository.AsQueryable().Where(x => x.Id == entity.AssetTemplateId && x.Attributes.Any(att => att.AssetAttributeCommand.DeviceTemplateId == entity.AssetAttributeCommand.DeviceTemplateId)).SelectMany(x => x.Attributes.Select(a => a.AssetAttributeCommand)).FirstOrDefaultAsync();

            foreach (var asset in assets)
            {
                var assetMapping = asset.AssetAttributeCommandMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == assetTemplateCommandAttribute?.AssetAttributeTemplateId && x.AssetAttributeCommandTemplate.MarkupName == commandPayload.MarkupName);
                // find the proper deviceId
                string deviceId = assetMapping?.DeviceId;
                var commandMapping = new Domain.Entity.AssetAttributeCommandMapping
                {
                    AssetId = asset.Id,
                    AssetAttributeTemplateId = entity.Id,
                    DeviceId = deviceId,
                    MetricKey = entity.AssetAttributeCommand.MetricKey
                };
                assetAttributeCommandMappings.Add(commandMapping);
            }
            entity.AssetAttributeCommandMappings = assetAttributeCommandMappings;

            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }

        private async Task ValidateCommandAttributeAsync(Domain.Entity.AssetAttributeTemplate attribute)
        {
            var deviceTemplate = await _readDeviceTemplateRepository.AsQueryable().AsNoTracking().Include(x => x.Bindings).FirstOrDefaultAsync(x => x.Id == attribute.AssetAttributeCommand.DeviceTemplateId);
            if (deviceTemplate == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeCommandTemplate.DeviceTemplateId));

            var metricFieldName = nameof(Domain.Entity.AssetAttributeCommandTemplate.MetricKey);
            if (deviceTemplate.Bindings == null || !deviceTemplate.Bindings.Any())
                throw ValidationExceptionHelper.GenerateNotFoundValidation(metricFieldName);

            var binding = deviceTemplate.Bindings.FirstOrDefault(x => x.Key == attribute.AssetAttributeCommand.MetricKey);
            if (binding == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(metricFieldName);

            attribute.DataType = binding.DataType;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeCommandTemplate>();
            await _dynamicValidator.ValidateAsync(commandPayload, cancellationToken);

            var entity = await _unitOfWork.Attributes.AsQueryable().Include(x => x.AssetAttributeCommand).FirstAsync(x => x.Id == attribute.Id);
            entity.Name = attribute.Name;
            entity.ThousandSeparator = attribute.ThousandSeparator;
            entity.DecimalPlace = attribute.DecimalPlace;

            var currentDeviceTemplateId = entity.AssetAttributeCommand.DeviceTemplateId;
            entity.AssetAttributeCommand.DeviceTemplateId = commandPayload.DeviceTemplateId;
            entity.AssetAttributeCommand.MetricKey = commandPayload.MetricKey;
            entity.AssetAttributeCommand.MarkupName = commandPayload.MarkupName;
            await ValidateCommandAttributeAsync(entity);
            var attributeMappings = await _readAssetRepository.OnlyAssetAsQueryable()
                                                            .Include(x => x.AssetAttributeCommandMappings).ThenInclude(x => x.AssetAttributeCommandTemplate)
                                                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                                                            .SelectMany(x => x.AssetAttributeCommandMappings)
                                                            .ToListAsync();
            foreach (var mapping in attributeMappings.Where(x => x.AssetAttributeTemplateId == attribute.Id))
            {
                mapping.MetricKey = commandPayload.MetricKey;
                if (commandPayload.DeviceTemplateId != currentDeviceTemplateId)
                {
                    // if the markup already exist (and has valid device Id) in any other attribute mapping, update this mapping with the corresponding device Id
                    var currentDeviceIdWithSameMarkup = attributeMappings.Where(x => x.AssetAttributeTemplateId != attribute.Id
                                                                                  && x.AssetAttributeCommandTemplate.MarkupName == commandPayload.MarkupName
                                                                                  && x.DeviceId != null)
                                                                         .FirstOrDefault()?.DeviceId;
                    mapping.DeviceId = currentDeviceIdWithSameMarkup;
                    if (currentDeviceIdWithSameMarkup is null)
                    {
                        continue;
                    }
                }

                var exist = (await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().Include(x => x.AssetAttributeCommandMappings).SelectMany(x => x.AssetAttributeCommandMappings)
                        .AnyAsync(x => x.DeviceId == mapping.DeviceId && x.MetricKey == mapping.MetricKey && x.Id != mapping.Id))
                        || (await _readAssetAttributeRepository.AsQueryable().AsNoTracking().Include(x => x.AssetAttributeCommand)
                        .AnyAsync(x => x.AssetAttributeCommand.DeviceId == mapping.DeviceId && x.AssetAttributeCommand.MetricKey == mapping.MetricKey));
                if (exist)
                    mapping.MetricKey = string.Empty;
            }
            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }
    }

    internal class AssetAttributeCommandTemplate
    {
        public Guid DeviceTemplateId { get; set; }
        public string MarkupName { get; set; }
        public string MetricKey { get; set; }

        internal static Domain.Entity.AssetAttributeCommandTemplate Create(AssetAttributeCommandTemplate commandPayload)
        {
            return new Domain.Entity.AssetAttributeCommandTemplate()
            {
                DeviceTemplateId = commandPayload.DeviceTemplateId,
                MarkupName = commandPayload.MarkupName,
                MetricKey = commandPayload.MetricKey
            };
        }
    }
}
