using System;
using System.Linq.Expressions;

namespace Device.Application.BlockTemplate.Command.Model
{
    public class SimpleTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    public class SimpleFunctionBlockTemplateDto : SimpleTemplateDto
    {
        private static Func<Domain.Entity.FunctionBlockTemplate, SimpleFunctionBlockTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplate, SimpleFunctionBlockTemplateDto>> Projection
        {
            get
            {
                return entity => new SimpleFunctionBlockTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedUtc = entity.CreatedUtc
                };
            }
        }

        public static SimpleTemplateDto Create(Domain.Entity.FunctionBlockTemplate entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }

    public class SimpleFunctionBlockDto : SimpleTemplateDto
    {
        static Func<Domain.Entity.FunctionBlock, SimpleFunctionBlockDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlock, SimpleFunctionBlockDto>> Projection
        {
            get
            {
                return entity => new SimpleFunctionBlockDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedUtc = entity.CreatedUtc
                };
            }
        }

        public static SimpleTemplateDto Create(Domain.Entity.FunctionBlock entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
