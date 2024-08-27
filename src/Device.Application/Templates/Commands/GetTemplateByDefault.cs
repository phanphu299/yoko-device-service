using Device.Application.Template.Command.Model;
using MediatR;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command
{
    public class GetTemplateByDefault : BaseCriteria, IRequest<IEnumerable<GetValidTemplateDto>>
    {
        public GetTemplateByDefault()
        {
        }
    }
}
