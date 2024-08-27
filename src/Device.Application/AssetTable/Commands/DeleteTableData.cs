using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class DeleteTableData : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }

        public IEnumerable<object> Ids { get; set; }

        public DeleteTableData(Guid id, IEnumerable<object> ids)
        {
            Id = id;
            Ids = ids;
        }
    }
}