using System;
using System.Linq.Expressions;
using Device.Application.BlockFunctionCategory.Model;
using Device.Domain.Entity;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class UpdateBlockCategory : IRequest<BlockCategoryDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        static Func<UpdateBlockCategory, FunctionBlockCategory> Converter = Projection.Compile();
        private static Expression<Func<UpdateBlockCategory, FunctionBlockCategory>> Projection
        {
            get
            {
                return model => new FunctionBlockCategory
                {
                    Id = model.Id,
                    Name = model.Name,
                    ParentId = model.ParentId,
                };
            }
        }
        public static FunctionBlockCategory Create(UpdateBlockCategory model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
