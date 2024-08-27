using System;
using System.Linq.Expressions;

namespace Device.Application.BlockTemplate.Command.Model
{
    public class FunctionBlockTemplateDto
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.FunctionBlockTemplate, FunctionBlockTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplate, FunctionBlockTemplateDto>> Projection
        {
            get
            {
                return model => new FunctionBlockTemplateDto
                {
                    Id = model.Id,
                };
            }
        }

        public static FunctionBlockTemplateDto Create(Domain.Entity.FunctionBlockTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
