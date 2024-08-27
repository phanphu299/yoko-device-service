using System;
using System.Linq.Expressions;
using Device.Application.BlockSnippet.Model;
using Device.Domain.Entity;
using MediatR;

namespace Device.Application.BlockSnippet.Command
{
    public class UpdateBlockSnippet : IRequest<BlockSnippetDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TemplateCode { get; set; }

        static Func<UpdateBlockSnippet, FunctionBlockSnippet> Converter = Projection.Compile();

        private static Expression<Func<UpdateBlockSnippet, FunctionBlockSnippet>> Projection
        {
            get
            {
                return model => new FunctionBlockSnippet
                {
                    Name = model.Name,
                    TemplateCode = model.TemplateCode
                };
            }
        }
        public static FunctionBlockSnippet Create(UpdateBlockSnippet model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
