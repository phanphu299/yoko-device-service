using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;
using MediatR;

namespace Device.Application.Analytic.Query
{
    public class ManualRegressionData : IRequest<RegressionDataDto>
    {
        public string TimezoneOffset { get; set; } = "+00:00";
        public int TimeoutInSecond { get; set; }
        public IEnumerable<FitingPoint> DataPlots { get; set; }
        public string FitMethod { get; set; }
        public int Order { get; set; }

    }
}
