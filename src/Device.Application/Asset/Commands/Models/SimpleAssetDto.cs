using System;

namespace Device.Application.Asset.Command.Model
{
    public class SimpleAssetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public SimpleAssetDto(Domain.Entity.Asset element)
        {
            Id = element.Id;
            Name = element.Name;
        }
    }
}
