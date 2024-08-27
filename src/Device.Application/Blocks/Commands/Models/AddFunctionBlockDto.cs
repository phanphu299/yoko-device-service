using System;
using System.Linq.Expressions;

namespace Device.Application.Block.Command.Model
{
    public class AddFunctionBlockDto
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.FunctionBlock, AddFunctionBlockDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlock, AddFunctionBlockDto>> Projection
        {
            get
            {
                return model => new AddFunctionBlockDto
                {
                    Id = model.Id
                };
            }
        }

        public static AddFunctionBlockDto Create(Domain.Entity.FunctionBlock model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
