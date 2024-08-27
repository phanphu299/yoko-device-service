using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IRegressionAnalysis
    {
         RegressionDataDto FitAnalysis(string fitmethod, IEnumerable<double> x, IEnumerable<double> y, int? order);

    }
}