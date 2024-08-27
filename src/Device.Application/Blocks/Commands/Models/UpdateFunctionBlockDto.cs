using System;
using System.Linq.Expressions;

namespace Device.Application.Block.Command.Model
{
    public class UpdateFunctionBlockDto
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.FunctionBlock, UpdateFunctionBlockDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlock, UpdateFunctionBlockDto>> Projection
        {
            get
            {
                return model => new UpdateFunctionBlockDto
                {
                    Id = model.Id
                };
            }
        }

        public static UpdateFunctionBlockDto Create(Domain.Entity.FunctionBlock model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
