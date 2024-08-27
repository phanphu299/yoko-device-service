using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.TemplateBinding.Command.Model;
using Device.Application.TemplatePayload.Command.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.Template.Command.Model
{
    public class GetTemplateDto : TagDtos
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { set; get; }
        public int TotalMetric { set; get; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string LockedByUpn { get; set; }
        public string CreatedBy { get; set; }
        public IEnumerable<GetTemplatePayloadDto> Payloads { get; set; } = new List<GetTemplatePayloadDto>();
        public IEnumerable<GetTemplateBindingDto> Bindings { get; set; } = new List<GetTemplateBindingDto>();

        public int CountPayload { set; get; }
        public GetTemplateDto()
        {
            CountPayload = Payloads.Count();
        }
        static Func<Domain.Entity.DeviceTemplate, GetTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.DeviceTemplate, GetTemplateDto>> Projection
        {
            get
            {
                return entity => new GetTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Deleted = entity.Deleted,
                    TotalMetric = entity.TotalMetric,
                    Payloads = entity.Payloads.OrderBy(payload => payload.Id).Select(GetTemplatePayloadDto.Create),
                    CountPayload = entity.Payloads.Count,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedBy = entity.CreatedBy,
                    Bindings = entity.Bindings.OrderBy(binding => binding.Id).Select(GetTemplateBindingDto.Create),
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetTemplateDto Create(Domain.Entity.DeviceTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
