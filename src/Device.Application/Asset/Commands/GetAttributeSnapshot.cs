using System;
using Device.Application.Historical.Query.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetAttributeSnapshot : IRequest<HistoricalDataDto>
    {
        public Guid Id { get; set; }
        public bool UseCache { get; set; } = true;
        public GetAttributeSnapshot(Guid id)
        {
            Id = id;
        }
    }
}