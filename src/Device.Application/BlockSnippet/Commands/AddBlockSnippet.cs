using System;
using System.Linq.Expressions;
using Device.Application.BlockSnippet.Model;
using MediatR;

namespace Device.Application.BlockSnippet.Command
{
    public class AddBlockSnippet : IRequest<BlockSnippetDto>
    {
        public string Name { get; set; }
        public string TemplateCode { get; set; }

        static Func<AddBlockSnippet, Domain.Entity.FunctionBlockSnippet> Converter = Projection.Compile();
        private static Expression<Func<AddBlockSnippet, Domain.Entity.FunctionBlockSnippet>> Projection
        {
            get
            {
                return model => new Domain.Entity.FunctionBlockSnippet
                {
                    Name = model.Name,
                    TemplateCode = model.TemplateCode,
                };
            }
        }


        public static Domain.Entity.FunctionBlockSnippet Create(AddBlockSnippet model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
