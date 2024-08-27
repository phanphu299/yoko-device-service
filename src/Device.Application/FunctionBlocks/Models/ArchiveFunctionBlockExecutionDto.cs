using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Constant;

namespace Device.Application.BlockFunction.Model
{
    public class ArchiveFunctionBlockExecutionDto : FunctionBlockExecutionDto
    {
        public string ExecutionContent { get; set; }
        public string ResourcePath { get; set; }
        public DateTime CreatedUtc { get; set; }
        public new IEnumerable<ArchiveFunctionBlockNodeMappingDto> Mappings { get; set; }

        static Func<Domain.Entity.FunctionBlockExecution, ArchiveFunctionBlockExecutionDto> DtoConverter = DtoProjection.Compile();

        static Func<ArchiveFunctionBlockExecutionDto, string, Domain.Entity.FunctionBlockExecution> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockExecution, ArchiveFunctionBlockExecutionDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveFunctionBlockExecutionDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TemplateId = entity.TemplateId,
                    FunctionBlockId = entity.FunctionBlockId,
                    DiagramContent = entity.DiagramContent,
                    TriggerType = entity.TriggerType,
                    TriggerContent = entity.TriggerContent,
                    Status = entity.Status == BlockExecutionStatusConstants.STOPPED_ERROR ? entity.Status : BlockExecutionStatusConstants.STOPPED,
                    ExecutionContent = entity.ExecutionContent,
                    RunImmediately = entity.RunImmediately,
                    ResourcePath = entity.ResourcePath,
                    TriggerAssetMarkup = entity.TriggerAssetMarkup,
                    TriggerAssetId = entity.TriggerAssetId,
                    TriggerAttributeId = entity.TriggerAttributeId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Version = entity.Version,
                    Mappings = entity.Mappings.Select(ArchiveFunctionBlockNodeMappingDto.CreateDto).ToList()
                };
            }
        }

        private static Expression<Func<ArchiveFunctionBlockExecutionDto, string, Domain.Entity.FunctionBlockExecution>> EntityProjection
        {
            get
            {
                return (dto, upn) => new Domain.Entity.FunctionBlockExecution
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    TemplateId = dto.TemplateId,
                    FunctionBlockId = dto.FunctionBlockId,
                    DiagramContent = dto.DiagramContent,
                    TriggerType = dto.TriggerType,
                    TriggerContent = dto.TriggerContent,
                    Status = dto.Status,
                    ExecutionContent = dto.ExecutionContent,
                    RunImmediately = dto.RunImmediately,
                    ResourcePath = dto.ResourcePath,
                    TriggerAssetMarkup = dto.TriggerAssetMarkup,
                    TriggerAssetId = dto.TriggerAssetId,
                    TriggerAttributeId = dto.TriggerAttributeId,
                    Mappings = dto.Mappings.Select(ArchiveFunctionBlockNodeMappingDto.CreateEntity).ToList(),
                    CreatedBy = upn,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    Version = dto.Version
                };
            }
        }

        public static ArchiveFunctionBlockExecutionDto CreateDto(Domain.Entity.FunctionBlockExecution model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockExecution CreateEntity(ArchiveFunctionBlockExecutionDto model, string upn)
        {
            if (model != null)
            {
                return EntityConverter(model, upn);
            }
            return null;
        }
    }

    public class ArchiveFunctionBlockNodeMappingDto
    {
        public Guid Id { get; set; }
        public Guid BlockExecutionId { get; set; }
        public Guid? BlockTemplateNodeId { get; set; }
        public string AssetMarkupName { get; set; }
        public Guid? AssetId { get; set; }
        public string AssetName { get; set; }
        public string TargetName { get; set; }
        public string Value { get; set; }

        static Func<Domain.Entity.FunctionBlockNodeMapping, ArchiveFunctionBlockNodeMappingDto> DtoConverter = DtoProjection.Compile();

        static Func<ArchiveFunctionBlockNodeMappingDto, Domain.Entity.FunctionBlockNodeMapping> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockNodeMapping, ArchiveFunctionBlockNodeMappingDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveFunctionBlockNodeMappingDto
                {
                    Id = entity.Id,
                    BlockExecutionId = entity.BlockExecutionId,
                    BlockTemplateNodeId = entity.BlockTemplateNodeId,
                    AssetMarkupName = entity.AssetMarkupName,
                    AssetId = entity.AssetId,
                    AssetName = entity.AssetName,
                    TargetName = entity.TargetName,
                    Value = entity.Value
                };
            }
        }

        private static Expression<Func<ArchiveFunctionBlockNodeMappingDto, Domain.Entity.FunctionBlockNodeMapping>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.FunctionBlockNodeMapping
                {
                    Id = dto.Id,
                    BlockExecutionId = dto.BlockExecutionId,
                    BlockTemplateNodeId = dto.BlockTemplateNodeId,
                    AssetMarkupName = dto.AssetMarkupName,
                    AssetId = dto.AssetId,
                    AssetName = dto.AssetName,
                    TargetName = dto.TargetName,
                    Value = dto.Value
                };
            }
        }

        public static ArchiveFunctionBlockNodeMappingDto CreateDto(Domain.Entity.FunctionBlockNodeMapping model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockNodeMapping CreateEntity(ArchiveFunctionBlockNodeMappingDto model)
        {
            if (model != null)
            {
                return EntityConverter(model);
            }
            return null;
        }
    }
}
