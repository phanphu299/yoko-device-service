using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Device.Command
{
    public class CheckExistDevice : IRequest<BaseResponse>
    {
        public IEnumerable<string> Ids { get; set; }

        public CheckExistDevice(IEnumerable<string> ids)
        {
            Ids = ids;
        }
    }
}
