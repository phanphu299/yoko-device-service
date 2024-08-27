using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class UpsertTableData : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public string DefaultColumnAction { get; set; }

        public IEnumerable<IDictionary<string, object>> Data { get; set; }

        public UpsertTableData(Guid id, IEnumerable<IDictionary<string, object>> data, string defaultColumnAction = null)
        {
            Id = id;
            Data = data;
            DefaultColumnAction = defaultColumnAction;
        }
    }
}