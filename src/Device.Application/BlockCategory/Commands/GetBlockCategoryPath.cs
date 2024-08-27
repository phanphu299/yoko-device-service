using System;
using System.Collections.Generic;
using Device.Application.BlockCategory.Model;
using MediatR;

namespace Device.Application.BlockCategory.Command
{
    public class GetBlockCategoryPath : IRequest<IEnumerable<GetBlockCategoryPathDto>>
    {
        public IEnumerable<Guid> Ids { get; set; }
        public string Type { get; set; }

        public GetBlockCategoryPath(IEnumerable<Guid> ids, string type)
        {
            Ids = ids;
            Type = type;
        }
    }
}
