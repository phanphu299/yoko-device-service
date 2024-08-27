using System;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class UnlockTable : IRequest<BaseResponse>
    {
        public Guid? AssetId { get; set; }
        public Guid TableId { get; set; }
        public string CurrentUserUpn { get; set; }
        public DateTime? CurrentTimestamp { get; set; }
    }
}
