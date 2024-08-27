using System;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetAssetsByTemplateId : BaseCriteria, IRequest<BaseSearchResponse<GetAssetSimpleDto>>
    {
        public Guid Id { get; set; }

        public GetAssetsByTemplateId(Guid id)
        {
            Id = id;
        }
    }
}