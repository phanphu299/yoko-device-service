using System;
using System.Linq.Expressions;
using Device.Application.BlockFunctionCategory.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class AddBlockCategory : IRequest<BlockCategoryDto>
    {
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        static Func<AddBlockCategory, Domain.Entity.FunctionBlockCategory> Converter = Projection.Compile();
        private static Expression<Func<AddBlockCategory, Domain.Entity.FunctionBlockCategory>> Projection
        {
            get
            {
                return model => new Domain.Entity.FunctionBlockCategory
                {
                    Name = model.Name,
                    ParentId = model.ParentId,
                };
            }
        }
        public static Domain.Entity.FunctionBlockCategory Create(AddBlockCategory model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
