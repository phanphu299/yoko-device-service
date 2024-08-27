using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Analytic.Query.Model;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;
using Device.ApplicationExtension.Extension;
using MediatR;
using Newtonsoft.Json;

namespace Device.Application.Analytic.Query.Handler
{
    public class GetAssetCorrelationHandler : IRequestHandler<GetAssetAttributeCorrelationData, IEnumerable<AssetCorrelationDataDto>>
    {
        private readonly IMediator _mediator;
        public GetAssetCorrelationHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IEnumerable<AssetCorrelationDataDto>> Handle(GetAssetAttributeCorrelationData request, CancellationToken cancellationToken)
        {
            var timeserieRequest = new GetAssetAttributeSeries()
            {
                Assets = request.Assets.Where(x => x.RequestType == HistoricalDataType.SERIES),
                TimeoutInSecond = request.TimeoutInSecond,
                TimezoneOffset = request.TimezoneOffset
            };
            var timeseriesResponse = await _mediator.Send(timeserieRequest);
            // add the correlation overlay on top of the result;
            var correlationResult = new List<AssetCorrelationDataDto>();
            foreach (var response in timeseriesResponse)
            {
                var result = BuildCorrelationResult(response, timeserieRequest);
                if (result == null)
                    break;
                correlationResult.Add(result);
            }
            return correlationResult;
        }
        
        private AssetCorrelationDataDto BuildCorrelationResult(HistoricalDataDto response, GetAssetAttributeSeries timeserieRequest)
        {
            var result = new AssetCorrelationDataDto()
            {
                Aggregate = response.Aggregate,
                AssetId = response.AssetId,
                AssetName = response.AssetName,
                AssetNormalizeName = response.AssetNormalizeName,
                End = response.End,
                Statics = response.Statics,
                TimeGrain = response.TimeGrain,
                Start = response.Start,
                QueryType = response.QueryType,
                RequestType = response.RequestType,
                TimezoneOffset = response.TimezoneOffset,
                GapfillFunction = response.Attributes.FirstOrDefault()?.GapfillFunction
            };

            if (response.Attributes.Any())
            {
                var canDoCorrelation = BuildCorrelations(response, result);
                if (!canDoCorrelation)
                    return null;
            }

            result.NoDataAttributeIds = response.Attributes.Where(x => x.Series == null || !x.Series.Any()).Select(x => x.AttributeId);

            var requestAsset = timeserieRequest.Assets.First(x => JsonConvert.SerializeObject(x.Statics) == JsonConvert.SerializeObject(response.Statics));
            var deletedAttributeIds = (from reqAtt in requestAsset.AttributeIds
                                       join resAtt in response.Attributes.Select(x => x.AttributeId) on reqAtt equals resAtt into gj
                                       where !gj.Any()
                                       select reqAtt);
            if (deletedAttributeIds.Any())
            {
                result.NoDataAttributeIds = result.NoDataAttributeIds.Union(deletedAttributeIds);
            }

            return result;
        }
        
        private bool BuildCorrelations(HistoricalDataDto response, AssetCorrelationDataDto result)
        {
            var firstAttribute = response.Attributes.First();
            var correlationArray = new List<double[]>();
            var attributeSeries = response.Attributes.Where(x => x.AttributeId != firstAttribute.AttributeId).SelectMany(x => x.Series.Select(series => (x.AttributeId, series.ts, Convert.ToDouble(series.v)))).ToList();
            var seriesJoinResult = (from source in firstAttribute.Series
                                    join target in attributeSeries on source.ts equals target.ts into gj
                                    select (source, gj.DefaultIfEmpty())
                                );

            // In case the first attribute just has one data point, then cann't do correlation
            if (seriesJoinResult.Count() == 1)
                return false;

            var attributes = response.Attributes.Where(x => x.AttributeId != firstAttribute.AttributeId).Select(x => x.AttributeId).ToList();
            foreach (var series in seriesJoinResult)
            {
                var (source, target) = series;
                var array = new List<double>();
                array.Add(Convert.ToDouble(source.v));
                var found = target.Where(x => x != default).ToList();
                array.AddRange(found.Select(t => t.Item3));
                var notFoundTimeSeries = attributes.Where(attributeId => !found.Any(foundTimeSeries => attributeId == foundTimeSeries.AttributeId)).Distinct();
                array.AddRange(notFoundTimeSeries.Select(attributeId =>
                                                       {
                                                           var previousValue = attributeSeries.FirstOrDefault(attribute => attribute.AttributeId == attributeId && source.ts > attribute.ts);
                                                           return previousValue != default ? previousValue.Item3 : Convert.ToDouble(source.v);
                                                       }));
                correlationArray.Add(array.ToArray());
            }
            // https://numerics.mathdotnet.com/api/MathNet.Numerics.Statistics/Correlation.htm
            // var correlation = Correlation.PearsonMatrix(correlationArray.ToArray());

            // http://accord-framework.net/docs/html/M_Accord_Statistics_Measures_Correlation_2.htm
            //result.Correlations = Measures.Correlation(correlationArray.ToArray());
            result.Correlations = correlationArray.ToArray().Correlation();
            return true;
        }
    }
}
