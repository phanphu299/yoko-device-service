using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Block.Command.Model;
using Device.Application.BlockBinding.Command;
using Device.Application.Constant;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.Block.Command
{
    public class AddFunctionBlock : IRequest<AddFunctionBlockDto>, INotification
    {
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public string BlockContent { get; set; }
        public Guid CategoryId { get; set; }
        public string Type { get; set; } = BlockTypeConstants.TYPE_BLOCK;
        public IEnumerable<AddFunctionBlockBinding> Bindings { get; set; } = new List<AddFunctionBlockBinding>();
        static Func<AddFunctionBlock, Domain.Entity.FunctionBlock> Converter = Projection.Compile();
        private static Expression<Func<AddFunctionBlock, Domain.Entity.FunctionBlock>> Projection
        {
            get
            {
                return command => new Domain.Entity.FunctionBlock
                {
                    Name = command.Name,
                    BlockContent = command.BlockContent,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    Deleted = false,
                    CategoryId = command.CategoryId,
                    Type = command.Type,
                    Bindings = command.Bindings.Select(AddFunctionBlockBinding.Create).ToList()
                };
            }
        }

        public static Domain.Entity.FunctionBlock Create(AddFunctionBlock command)
        {
            if (command != null)
            {
                return Converter(command);
            }
            return null;
        }
    }
}
