using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Block.Command.Model;

namespace Device.Application.BlockTemplate.Command.Model
{
    public class ArchiveBlockTemplateDto : GetFunctionBlockTemplateDto
    {
        public string ResourcePath { get; set; }
        public new IEnumerable<ArchiveBlockTemplateNodeDto> Nodes { get; set; } = new List<ArchiveBlockTemplateNodeDto>();
        private static Func<Domain.Entity.FunctionBlockTemplate, ArchiveBlockTemplateDto> DtoConverter = DtoProjection.Compile();
        private static Func<ArchiveBlockTemplateDto, string, Domain.Entity.FunctionBlockTemplate> EntityConverter = EntityProjection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplate, ArchiveBlockTemplateDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveBlockTemplateDto()
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    DesignContent = entity.DesignContent,
                    Content = entity.Content,
                    ResourcePath = entity.ResourcePath,
                    TriggerType = entity.TriggerType,
                    TriggerContent = entity.TriggerContent,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Version = entity.Version,
                    Nodes = entity.Nodes.Select(ArchiveBlockTemplateNodeDto.Create).ToList()
                };
            }
        }

        private static Expression<Func<ArchiveBlockTemplateDto, string, Domain.Entity.FunctionBlockTemplate>> EntityProjection
        {
            get
            {
                return (dto, upn) => new Domain.Entity.FunctionBlockTemplate()
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    DesignContent = dto.DesignContent,
                    Content = dto.Content,
                    ResourcePath = dto.ResourcePath,
                    TriggerType = dto.TriggerType,
                    TriggerContent = dto.TriggerContent,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    CreatedBy = upn,
                    Version = dto.Version,
                    // ResourcePath = $"objects/{dto.Id}",
                    Nodes = dto.Nodes.Select(ArchiveBlockTemplateNodeDto.CreateEntity).ToList()
                };
            }
        }

        public static new ArchiveBlockTemplateDto Create(Domain.Entity.FunctionBlockTemplate entity)
        {
            if (entity != null)
            {
                return DtoConverter(entity);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockTemplate Create(ArchiveBlockTemplateDto dto, string upn)
        {
            if (dto != null)
            {
                return EntityConverter(dto, upn);
            }
            return null;
        }
    }

    public class ArchiveBlockTemplateNodeDto : FunctionBlockTemplateNodeDto
    {
        public Guid BlockTemplateId { get; set; }
        public Guid FunctionBlockId { get; set; }
        private static Func<Domain.Entity.FunctionBlockTemplateNode, ArchiveBlockTemplateNodeDto> DtoConverter = DtoProjection.Compile();
        private static Func<ArchiveBlockTemplateNodeDto, Domain.Entity.FunctionBlockTemplateNode> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockTemplateNode, ArchiveBlockTemplateNodeDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveBlockTemplateNodeDto
                {
                    Id = entity.Id,
                    BlockTemplateId = entity.BlockTemplateId,
                    FunctionBlockId = entity.FunctionBlockId,
                    AssetMarkupName = entity.AssetMarkupName,
                    BlockType = entity.BlockType,
                    Name = entity.Name,
                    SequentialNumber = entity.SequentialNumber,
                    TargetName = entity.TargetName,
                    PortId = entity.PortId
                };
            }
        }

        private static Expression<Func<ArchiveBlockTemplateNodeDto, Domain.Entity.FunctionBlockTemplateNode>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.FunctionBlockTemplateNode
                {
                    Id = dto.Id,
                    BlockTemplateId = dto.BlockTemplateId,
                    FunctionBlockId = dto.FunctionBlockId,
                    AssetMarkupName = dto.AssetMarkupName,
                    BlockType = dto.BlockType,
                    Name = dto.Name,
                    SequentialNumber = dto.SequentialNumber,
                    TargetName = dto.TargetName,
                    PortId = dto.PortId
                };
            }
        }

        public new static ArchiveBlockTemplateNodeDto Create(Domain.Entity.FunctionBlockTemplateNode entity)
        {
            if (entity != null)
            {
                return DtoConverter(entity);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockTemplateNode CreateEntity(ArchiveBlockTemplateNodeDto dto)
        {
            if (dto != null)
            {
                return EntityConverter(dto);
            }
            return null;
        }
    }
}
