using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using System;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.AssetTemplate.Command;
using Microsoft.EntityFrameworkCore;
using Device.Application.Constant;
using Device.Application.SharedKernel;
using System.Linq;

namespace Device.Application.Service
{
    public class AssetAttributeTemplateService : IAssetAttributeTemplateService
    {
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IAssetTemplateAttributeHandler _attributeHandler;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;

        public AssetAttributeTemplateService(
            IAssetTemplateAttributeHandler attributeHandler,
            IAssetTemplateUnitOfWork unitOfWork,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository)
        {
            _unitOfWork = unitOfWork;
            _attributeHandler = attributeHandler;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
        }

        public async Task<IList<BaseJsonPathDocument>> UpsertAssetAttributeTemplateAsync(JsonPatchDocument document, GetAssetTemplateDto assetTemplate, CancellationToken cancellationToken)
        {
            string path;
            List<Operation> operations = document.Operations;
            List<BaseJsonPathDocument> returnJsonPatch = new List<BaseJsonPathDocument>();
            Guid attributeId;
            //var attributes = new List<Domain.Entity.AssetAttributeTemplate>();
            int index = 0;
            var inputAttributes = operations
                                        .Where(x => x.op == "add" || x.op == "edit")
                                        .Select(x => JsonConvert.DeserializeObject<AssetTemplateAttribute>(JsonConvert.SerializeObject(x.value)));

            var deletedAttributeIds = new List<Guid>();
            foreach (Operation operation in operations)
            {
                index++;
                BaseJsonPathDocument resultModels = new BaseJsonPathDocument
                {
                    OP = operation.op,
                    Path = operation.path
                };
                switch (operation.op)
                {
                    case "add":
                        var addAttribute = JsonConvert.DeserializeObject<AssetTemplateAttribute>(JsonConvert.SerializeObject(operation.value));
                        addAttribute.AssetTemplate = assetTemplate;
                        addAttribute.SequentialNumber = index;
                        addAttribute.DecimalPlace = GetAttributeDecimalPlace(addAttribute);
                        var addAssetAttributeTemplate = await _attributeHandler.AddAsync(addAttribute, inputAttributes, cancellationToken);
                        //attributes.Add(addAssetAttributeTemplate);
                        resultModels.Values = addAssetAttributeTemplate.Id;
                        break;
                    case "edit":
                        path = operation.path.Replace("/", "");
                        if (Guid.TryParse(path, out attributeId))
                        {
                            var updateAttribute = JsonConvert.DeserializeObject<AssetTemplateAttribute>(JsonConvert.SerializeObject(operation.value));
                            updateAttribute.AssetTemplate = assetTemplate;
                            updateAttribute.Id = attributeId;
                            updateAttribute.DecimalPlace = GetAttributeDecimalPlace(updateAttribute);
                            var assetAttributeTemplate = await _attributeHandler.UpdateAsync(updateAttribute, inputAttributes, cancellationToken);
                            //attributes.Add(assetAttributeTemplate);
                            resultModels.Values = assetAttributeTemplate.Id;
                        }
                        break;
                    case "remove":
                        path = operation.path.Replace("/", "");
                        if (Guid.TryParse(path, out attributeId))
                        {
                            resultModels.Values = await _unitOfWork.Attributes.RemoveEntityAsync(attributeId, deletedAttributeIds);
                            deletedAttributeIds.Add(attributeId);
                        }
                        break;
                }
                returnJsonPatch.Add(resultModels);
            }
            return returnJsonPatch;
        }

        private int? GetAttributeDecimalPlace(AssetTemplateAttribute attribute)
        {
            return attribute.DataType == DataTypeConstants.TYPE_DOUBLE ? attribute.DecimalPlace : null;
        }

        public Task<bool> CheckExistAttributeAsync(Guid assetAttributeId)
        {
            return _readAssetAttributeTemplateRepository.AsQueryable().AnyAsync(x => x.Id == assetAttributeId);
        }
    }
}
