using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Block.Command.Model;
using Device.Application.BlockBinding;
using Device.Application.Constant;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.Block.Command
{
    public class UpdateFunctionBlock : IRequest<UpdateFunctionBlockDto>
    {
        public Guid Id { get; set; }
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public string BlockContent { get; set; }
        public Guid CategoryId { get; set; }
        public string Type { get; set; } = BlockTypeConstants.TYPE_BLOCK;
        public IEnumerable<UpdateFunctionBlockBinding> Bindings { get; set; } = new List<UpdateFunctionBlockBinding>();
        static Func<UpdateFunctionBlock, Domain.Entity.FunctionBlock> Converter = Projection.Compile();
        private static Expression<Func<UpdateFunctionBlock, Domain.Entity.FunctionBlock>> Projection
        {
            get
            {
                return command => new Domain.Entity.FunctionBlock
                {
                    Id = command.Id,
                    Name = command.Name,
                    BlockContent = command.BlockContent,
                    CategoryId = command.CategoryId,
                    Type = command.Type,
                    Bindings = command.Bindings.Select(UpdateFunctionBlockBinding.Create).ToList()
                };
            }
        }

        public static Domain.Entity.FunctionBlock Create(UpdateFunctionBlock command)
        {
            if (command != null)
            {
                return Converter(command);
            }
            return null;
        }
    }
}
