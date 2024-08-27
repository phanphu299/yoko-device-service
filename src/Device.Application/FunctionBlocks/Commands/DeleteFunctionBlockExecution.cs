using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.FunctionBlock.Command
{
    public class DeleteFunctionBlockExecution : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public bool IsListDelete { get; set; } = false;
        public IEnumerable<DeleteFunctionBlockExecution> BlockFunctions { get; set; } = new List<DeleteFunctionBlockExecution>();
        public DeleteFunctionBlockExecution()
        {
        }
        public DeleteFunctionBlockExecution(Guid id)
        {
            Id = id;
        }
        static Func<DeleteFunctionBlockExecution, Domain.Entity.FunctionBlockExecution> Converter = Projection.Compile();
        private static Expression<Func<DeleteFunctionBlockExecution, Domain.Entity.FunctionBlockExecution>> Projection
        {
            get
            {
                return entity => new Domain.Entity.FunctionBlockExecution
                {
                    Id = entity.Id
                };
            }
        }

        public static Domain.Entity.FunctionBlockExecution Create(DeleteFunctionBlockExecution model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
