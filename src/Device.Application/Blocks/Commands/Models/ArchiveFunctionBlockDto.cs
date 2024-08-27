using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.BlockBinding.Command.Model;

namespace Device.Application.Block.Command.Model
{
    public class ArchiveFunctionBlockDto : GetFunctionBlockDto
    {
        public new Guid CategoryId { get; set; }
        public string ResourcePath { get; set; }
        public new ICollection<ArchiveFunctionBlockBindingDto> Bindings { get; set; }

        static Func<Domain.Entity.FunctionBlock, ArchiveFunctionBlockDto> DtoConverter = DtoProjection.Compile();

        static Func<ArchiveFunctionBlockDto, string, Domain.Entity.FunctionBlock> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlock, ArchiveFunctionBlockDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveFunctionBlockDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    CategoryId = entity.CategoryId,
                    BlockContent = entity.BlockContent,
                    Type = entity.Type,
                    Deleted = entity.Deleted,
                    ResourcePath = entity.ResourcePath,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Bindings = entity.Bindings.Select(ArchiveFunctionBlockBindingDto.CreateDto).ToList()
                };
            }
        }

        private static Expression<Func<ArchiveFunctionBlockDto, string, Domain.Entity.FunctionBlock>> EntityProjection
        {
            get
            {
                return (dto, upn) => new Domain.Entity.FunctionBlock
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    CategoryId = dto.CategoryId,
                    BlockContent = dto.BlockContent,
                    Type = dto.Type,
                    Deleted = dto.Deleted,
                    ResourcePath = dto.ResourcePath,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    Bindings = dto.Bindings.Select(ArchiveFunctionBlockBindingDto.CreateEntity).ToList(),
                    CreatedBy = upn
                };
            }
        }

        public static ArchiveFunctionBlockDto CreateDto(Domain.Entity.FunctionBlock model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlock CreateEntity(ArchiveFunctionBlockDto model, string upn)
        {
            if (model != null)
            {
                return EntityConverter(model, upn);
            }
            return null;
        }
    }

    public class ArchiveFunctionBlockBindingDto : GetFunctionBlockBindingDto
    {
        public int SequentialNumber { get; set; } = 1;
        public DateTime CreatedUtc { get; set; }

        static Func<Domain.Entity.FunctionBlockBinding, ArchiveFunctionBlockBindingDto> DtoConverter = DtoProjection.Compile();

        static Func<ArchiveFunctionBlockBindingDto, Domain.Entity.FunctionBlockBinding> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockBinding, ArchiveFunctionBlockBindingDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveFunctionBlockBindingDto
                {
                    Id = entity.Id,
                    Key = entity.Key,
                    FunctionBlockId = entity.FunctionBlockId,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue,
                    BindingType = entity.BindingType,
                    Description = entity.Description,
                    Deleted = entity.Deleted,
                    SequentialNumber = entity.SequentialNumber,
                    CreatedUtc = entity.CreatedUtc
                };
            }
        }

        private static Expression<Func<ArchiveFunctionBlockBindingDto, Domain.Entity.FunctionBlockBinding>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.FunctionBlockBinding
                {
                    Id = dto.Id,
                    Key = dto.Key,
                    FunctionBlockId = dto.FunctionBlockId,
                    DataType = dto.DataType,
                    DefaultValue = dto.DefaultValue,
                    BindingType = dto.BindingType,
                    Description = dto.Description,
                    Deleted = dto.Deleted,
                    SequentialNumber = dto.SequentialNumber,
                    CreatedUtc = dto.CreatedUtc
                };
            }
        }

        public static ArchiveFunctionBlockBindingDto CreateDto(Domain.Entity.FunctionBlockBinding model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockBinding CreateEntity(ArchiveFunctionBlockBindingDto model)
        {
            if (model != null)
            {
                return EntityConverter(model);
            }
            return null;
        }
    }
}
