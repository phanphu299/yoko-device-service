using System;
using System.Collections.Generic;
using System.Linq;
using Device.Application.Analytic.Query.Model;
using Device.Application.Service.Abstraction;
using MathNet.Numerics;

namespace Device.Application.Service
{
    public class RegressionAnalysis : IRegressionAnalysis
    {
        public RegressionDataDto FitAnalysis(string fitmethod, IEnumerable<double> x, IEnumerable<double> y, int? order)
        {
            double[] coeff;
            double a, b, r, rSquared, se, pearson;
            var ret = new RegressionDataDto();

            switch (fitmethod)
            {
                case "linear":
                    (a, b) = Fit.Line(x.ToArray(), y.ToArray());
                    if (double.IsNaN(a) || double.IsNaN(b))
                    {
                        throw new System.Exception("Invalid data");
                    }
                    rSquared = GoodnessOfFit.RSquared(x.Select(x => a + b * x), y); // == 1.0
                    pearson = GoodnessOfFit.R(x.Select(x => a + b * x), y);
                    se = GoodnessOfFit.StandardError(x.Select(x => a + b * x), y, 0);

                    ret.Equation = $"{a} + {b}*x";
                    ret.Coefficients = new List<ValuesDics>
                                {
                                    new ValuesDics("a",a),
                                    new ValuesDics("b",b)
                                };
                    ret.GoodnessMeansures = new List<ValuesDics>
                                {
                                    new ValuesDics("rSquared",rSquared),
                                    new ValuesDics("pearson",pearson),
                                    new ValuesDics("se",se)
                                };

                    break;
                case "logarithm": //logarithm y : x -> a + b*ln(x), returning
                    (a, b) = Fit.Logarithm(x.ToArray(), y.ToArray());
                    if (double.IsNaN(a) || double.IsNaN(b))
                    {
                        throw new System.Exception("Invalid data");
                    }

                    rSquared = GoodnessOfFit.RSquared(x.Select(x => a + b * Math.Log(x)), y); // == 1.0
                    pearson = GoodnessOfFit.R(x.Select(x => a + b * Math.Log(x)), y);
                    se = GoodnessOfFit.StandardError(x.Select(x => a + b * Math.Log(x)), y, 0);

                    ret.Equation = $"{a} + {b}*ln(x)";
                    ret.Coefficients = new List<ValuesDics>
                                {
                                    new ValuesDics("a",a),
                                    new ValuesDics("b",b)
                                };
                    ret.GoodnessMeansures = new List<ValuesDics>
                                {
                                    new ValuesDics("rSquared",rSquared),
                                    new ValuesDics("pearson",pearson),
                                    new ValuesDics("se",se)
                                };

                    break;
                case "exponential":
                    (a, r) = Fit.Exponential(x.ToArray(), y.ToArray());
                    if (double.IsNaN(a) || double.IsNaN(r))
                    {
                        throw new System.Exception("Invalid data");
                    }
                    rSquared = GoodnessOfFit.RSquared(x.Select(x => a * Math.Exp(r * x)), y); // == 1.0
                    pearson = GoodnessOfFit.R(x.Select(x => a * Math.Exp(r * x)), y);
                    se = GoodnessOfFit.StandardError(x.Select(x => a * Math.Exp(r * x)), y, 0);

                    ret.Equation = $"{a}*exp({r}*x)";
                    ret.Coefficients = new List<ValuesDics>
                                {
                                    new ValuesDics("a",a),
                                    new ValuesDics("r",r)
                                };
                    ret.GoodnessMeansures = new List<ValuesDics>
                                {
                                    new ValuesDics("rSquared",rSquared),
                                    new ValuesDics("pearson",pearson),
                                    new ValuesDics("se",se)
                                };

                    break;
                case "polynomial":
                    var polynomialOrder = (order != null) ? (int)order : 2;
                   
                    coeff = Fit.Polynomial(x.ToArray(), y.ToArray(), polynomialOrder);
                    if (coeff.Any(x => double.IsNaN(x)))
                    {
                        throw new System.Exception("Invalid data");
                    }
                    var poly = new MathNet.Numerics.Polynomial(coeff);

                    rSquared = GoodnessOfFit.RSquared(x.Select(x => poly.Evaluate(x)), y); // == 1.0
                    pearson = GoodnessOfFit.R(x.Select(x => poly.Evaluate(x)), y);
                    se = GoodnessOfFit.StandardError(x.Select(x => poly.Evaluate(x)), y, 0);

                    ret.Equation = poly.ToString();
                    ret.Coefficients = coeff.Select((x, index) => new ValuesDics($"p{index}", x));

                    ret.GoodnessMeansures = new List<ValuesDics>
                                    {
                                        new ValuesDics("rSquared",rSquared),
                                        new ValuesDics("pearson",pearson),
                                        new ValuesDics("se",se)
                                    };

                    break;
                case "power":
                    (a, b) = Fit.Power(x.ToArray(), y.ToArray());
                    if (double.IsNaN(a) || double.IsNaN(b))
                    {
                        throw new System.Exception("Invalid data");
                    }
                    rSquared = GoodnessOfFit.RSquared(x.Select(x => a * Math.Pow(x, b)), y); // == 1.0
                    pearson = GoodnessOfFit.R(x.Select(x => a * Math.Pow(x, b)), y);
                    se = GoodnessOfFit.StandardError(x.Select(x => a * Math.Pow(x, b)), y, 0);

                    ret.Equation = $"{a}*x^{b}";
                    ret.Coefficients = new List<ValuesDics>
                                {
                                    new ValuesDics("a",a),
                                    new ValuesDics("b",b)
                                };
                    ret.GoodnessMeansures = new List<ValuesDics>
                                {
                                    new ValuesDics("rSquared",rSquared),
                                    new ValuesDics("pearson",pearson),
                                    new ValuesDics("se",se)
                                };
                    break;
                default:
                    break;
            }
            return ret;
        }
    }
}
