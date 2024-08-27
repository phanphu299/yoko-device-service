using MediatR;
using System;
using System.Collections.Generic;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Asset.Command
{
    public class ValidateAssetAttributesSeries : IRequest<IEnumerable<ValidateAssetAttributesDto>>
    {
        public IEnumerable<ValidateAssetAttributeSeries> ValidateAssets { get; set; }
    }

    public class ValidateAssetAttributeSeries
    {
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid AssetId { get; set; }
        public IEnumerable<Guid> AttributeIds { get; set; }
        public IDictionary<string, string> Statics { get; set; }

        public ValidateAssetAttributeSeries()
        {
            AttributeIds = new List<Guid>();
        }
    }
}