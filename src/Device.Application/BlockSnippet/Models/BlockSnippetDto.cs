using System;
using System.Linq.Expressions;

namespace Device.Application.BlockSnippet.Model
{
    public class BlockSnippetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TemplateCode { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        static Func<Domain.Entity.FunctionBlockSnippet, BlockSnippetDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockSnippet, BlockSnippetDto>> Projection
        {
            get
            {
                return model => new BlockSnippetDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    TemplateCode = model.TemplateCode,
                    CreatedUtc = model.CreatedUtc,
                    UpdatedUtc = model.UpdatedUtc,
                };
            }
        }

        public static BlockSnippetDto Create(Domain.Entity.FunctionBlockSnippet entity)
        {
            return Converter(entity);
        }
    }
}
