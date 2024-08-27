using System;
using System.Linq.Expressions;
using MessagePack;

namespace Device.Application.Historical.Query.Model
{
    [MessagePackObject]
    public class PaginationAttributeDto : AttributeDto
    {
        [Key("pageIndex")]
        public int PageIndex { get; set; }
        
        [Key("pageSize")]
        public int PageSize { get; set; }

        [Key("totalCount")]
        public int TotalCount { get; set; }

        static Func<AttributeDto, PaginationAttributeDto> Converter = Projection.Compile();
        private static Expression<Func<AttributeDto, PaginationAttributeDto>> Projection
        {
            get
            {
                return model => new PaginationAttributeDto
                {
                    AliasAttributeType = model.AliasAttributeType,
                    AttributeId = model.AttributeId,
                    AttributeName = model.AttributeName,
                    AttributeNameNormalize = model.AttributeNameNormalize,
                    AttributeType = model.AttributeType,
                    DataType = model.DataType,
                    DecimalPlace = model.DecimalPlace,
                    GapfillFunction = model.GapfillFunction,
                    Quality = model.Quality,
                    QualityCode = model.QualityCode,
                    Series = model.Series,
                    ThousandSeparator = model.ThousandSeparator,
                    Uom = model.Uom
                };
            }
        }
        public static PaginationAttributeDto Create(AttributeDto model)
        {
            return Converter(model);
        }
    }
}