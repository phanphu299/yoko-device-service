using System;
using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.AssetTemplate.Command
{
    public class CheckExistingAssetTemplate : IRequest<BaseResponse>
    {
        public IEnumerable<Guid> Ids { get; set; }

        public CheckExistingAssetTemplate(IEnumerable<Guid> ids)
        {
            Ids = ids;
        }
    }
}
