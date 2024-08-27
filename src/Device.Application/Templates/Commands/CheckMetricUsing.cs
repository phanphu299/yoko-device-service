using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Template.Command
{
    public class CheckMetricUsing : IRequest<BaseResponse>
    {
        public System.Guid Id { get; set; }
        public System.Guid DetailId { get; set; }

        public CheckMetricUsing(System.Guid id, System.Guid detailId)
        {
            Id = id;
            DetailId = detailId;
        }
    }
}
