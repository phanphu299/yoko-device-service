using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class CleanTopicCache: IRequest<BaseResponse>
    {
        public IEnumerable<string> Topics { get; set; }

        public CleanTopicCache(params string[] topics)
        {
            Topics = topics;
        }
    }
}