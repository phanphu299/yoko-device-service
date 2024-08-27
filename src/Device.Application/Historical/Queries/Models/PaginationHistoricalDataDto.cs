using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MessagePack;

namespace Device.Application.Historical.Query.Model
{
    [MessagePackObject]
    public class PaginationHistoricalDataDto : HistoricalGeneralDto
    {
        [Key("attributes")]
        public List<PaginationAttributeDto> Attributes { get; set; }

        static Func<HistoricalDataDto, PaginationHistoricalDataDto> Converter = Projection.Compile();
        private static Expression<Func<HistoricalDataDto, PaginationHistoricalDataDto>> Projection
        {
            get
            {
                return model => new PaginationHistoricalDataDto
                {
                    Aggregate = model.Aggregate,
                    AssetId = model.AssetId,
                    AssetName = model.AssetName,
                    AssetNormalizeName = model.AssetNormalizeName,
                    Attributes = model.Attributes.Select(x => PaginationAttributeDto.Create(x)).ToList(),
                    End = model.End,
                    QueryType = model.QueryType,
                    RequestType = model.RequestType,
                    Start = model.Start,
                    Statics = model.Statics,
                    TimeGrain = model.TimeGrain,
                    TimezoneOffset = model.TimezoneOffset
                };
            }
        }

        public static PaginationHistoricalDataDto Create(HistoricalDataDto model)
        {
            return Converter(model);
        }
    }
}