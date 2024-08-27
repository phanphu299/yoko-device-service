using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.FileRequest.Command
{
    public class ImportFile : IRequest<BaseResponse>
    {
        public string ObjectType { get; set; }
        public IEnumerable<string> FileNames { get; set; }
    }
}