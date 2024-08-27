using System.Collections.Generic;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;


namespace Device.Application.Uom.Command
{
    public class CheckExistUom : IRequest<BaseResponse>
    {
        public IEnumerable<int> Ids { get; set; }

        public CheckExistUom(IEnumerable<int> ids)
        {
            Ids = ids;
        }
    }
}