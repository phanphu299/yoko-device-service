using System;
using Device.Application.BlockFunctionCategory.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class GetBlockCategoryById : IRequest<GetBlockCategoryDto>
    {
        public Guid Id { get; set; }
    }
}
