using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Uom.Command.Model;
using MessagePack;

namespace Device.Application.Historical.Query.Model
{
    [MessagePackObject]
    public class AttributeDto
    {
        static Func<Guid, string, IEnumerable<Domain.Entity.TimeSeries>, bool, string, GetSimpleUomDto, AttributeDto> Converter = Projection.Compile();
        [Key("attributeId")]
        public Guid AttributeId { get; set; }
        [Key("series")]
        public List<TimeSeriesDto> Series { get; set; }
        [Key("attributeName")]
        public string AttributeName { get; set; }
        [Key("attributeNameNormalize")]
        public string AttributeNameNormalize { get; set; }
        [Key("uom")]
        public GetSimpleUomDto Uom { get; set; }
        [Key("decimalPlace")]
        public int? DecimalPlace { get; set; }
        [Key("thousandSeparator")]
        public bool? ThousandSeparator { get; set; }
        [Key("gapfillFunction")]
        public string GapfillFunction { get; set; }
        [Key("attributeType")]
        public string AttributeType { get; set; }
        [Key("dataType")]
        public string DataType { get; set; }
        [Key("qualityCode")]
        public int? QualityCode { get; set; }
        [Key("quality")]
        public string Quality { get; set; }
        [Key("aliasAttributeType")]
        public string AliasAttributeType { get; set; }
        private static Expression<Func<Guid, string, IEnumerable<Domain.Entity.TimeSeries>, bool, string, GetSimpleUomDto, AttributeDto>> Projection
        {
            get
            {
                return (attributeId, dataType, entities, isRawData, attributeName, uom) => new AttributeDto
                {
                    AttributeId = attributeId,
                    Series = entities.Select(x => TimeSeriesDto.Create(dataType, x, isRawData)).ToList(),
                    AttributeName = attributeName,
                    Uom = uom
                };
            }
        }

        public static AttributeDto Create(Guid attributeId, string dataType, IEnumerable<Domain.Entity.TimeSeries> entity, bool isRawData, string attributeName = null, GetSimpleUomDto umo = null)
        {
            if (entity == null)
                return null;
            return Converter(attributeId, dataType, entity, isRawData, attributeName, umo);
        }
    }
}