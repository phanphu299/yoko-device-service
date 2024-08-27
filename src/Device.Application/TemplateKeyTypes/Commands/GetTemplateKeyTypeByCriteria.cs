using Device.Application.TemplateKeyType.Command.Model;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.TemplateKeyType.Command
{
    public class GetTemplateKeyTypeByCriteria : BaseCriteria, IRequest<BaseSearchResponse<GetTemplateKeyTypeDto>>
    {
        //public bool ClientOverride { get; set; } = false;
    }
}
