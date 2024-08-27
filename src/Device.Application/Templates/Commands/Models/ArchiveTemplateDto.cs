using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Template.Command.Model
{
    public class ArchiveTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int? TotalMetric { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public IEnumerable<ArchiveTemplatePayloadDto> Payloads { get; set; } = new List<ArchiveTemplatePayloadDto>();
        public IEnumerable<ArchiveTemplateBindingDto> Bindings { get; set; } = new List<ArchiveTemplateBindingDto>();
        public IEnumerable<EntityTag> EntityTags { get; set; } = new List<EntityTag>();
        static Func<Domain.Entity.DeviceTemplate, ArchiveTemplateDto> DtoConverter = DtoProjection.Compile();
        static Func<ArchiveTemplateDto, string, Domain.Entity.DeviceTemplate> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.DeviceTemplate, ArchiveTemplateDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    TotalMetric = entity.TotalMetric,
                    Payloads = entity.Payloads.Select(ArchiveTemplatePayloadDto.Create),
                    Bindings = entity.Bindings.Select(ArchiveTemplateBindingDto.Create),
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    EntityTags = entity.EntityTags.OrderBy(x => x.Id).Select(x => new Domain.Entity.EntityTagDb
                    {
                        Id = x.Id,
                        TagId = x.TagId,
                        EntityType = x.EntityType,
                        EntityIdString = x.EntityIdString,
                        EntityIdLong = x.EntityIdLong,
                        EntityIdGuid = x.EntityIdGuid,
                        EntityIdInt = x.EntityIdInt
                    }).ToList(),
                };
            }
        }

        private static Expression<Func<ArchiveTemplateDto, string, Domain.Entity.DeviceTemplate>> EntityProjection
        {
            get
            {
                return (templateDto, upn) => new Domain.Entity.DeviceTemplate
                {
                    Id = templateDto.Id,
                    Name = templateDto.Name,
                    TotalMetric = templateDto.TotalMetric ?? 0,
                    CreatedBy = upn,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    EntityTags = templateDto.EntityTags.OrderBy(x => x.Id).Select(x => new Domain.Entity.EntityTagDb
                    {
                        Id = x.Id,
                        TagId = x.TagId,
                        EntityType = x.EntityType,
                        EntityIdString = x.EntityIdString,
                        EntityIdLong = x.EntityIdLong,
                        EntityIdGuid = x.EntityIdGuid,
                        EntityIdInt = x.EntityIdInt
                    }).ToList(),
                    Bindings = templateDto.Bindings.Select(x => new Domain.Entity.TemplateBinding
                    {
                        Id = x.Id,
                        TemplateId = x.TemplateId,
                        Key = x.Key,
                        DataType = x.DataType,
                        DefaultValue = x.DefaultValue
                    }).ToList(),
                    Payloads = templateDto.Payloads.Select(x => new Domain.Entity.TemplatePayload
                    {
                        Id = x.Id,
                        JsonPayload = x.JsonPayload,
                        Details = x.Details.Select(d => new Domain.Entity.TemplateDetail
                        {
                            Id = d.Id,
                            Key = d.Key,
                            Name = d.Name,
                            KeyTypeId = d.KeyTypeId.Value,
                            Expression = d.Expression,
                            ExpressionCompile = d.ExpressionCompile,
                            Enabled = d.Enabled,
                            DataType = d.DataType,
                            DetailId = d.DetailId.Value
                        })
                        .ToList()
                    })
                    .ToList()
                };
            }
        }

        public static ArchiveTemplateDto Create(Domain.Entity.DeviceTemplate model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        public static Domain.Entity.DeviceTemplate Create(ArchiveTemplateDto model, string upn)
        {
            if (model != null)
            {
                return EntityConverter(model, upn);
            }
            return null;
        }
    }
}
