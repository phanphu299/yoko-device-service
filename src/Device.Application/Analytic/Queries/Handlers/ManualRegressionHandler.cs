using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Analytic.Query.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Device.Application.Service.Abstraction;

namespace Device.Application.Analytic.Query.Handler
{
    public class ManualRegressionHandler : IRequestHandler<ManualRegressionData, RegressionDataDto>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IRegressionAnalysis _analysis;

        private const int MINIMUM_INPUT = 3;
        public ManualRegressionHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext, IConfiguration configuration,  IRegressionAnalysis analysis)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
            _configuration = configuration;
            _analysis = analysis;
        }
        public async Task<RegressionDataDto> Handle(ManualRegressionData request, CancellationToken cancellationToken)
        {
            var x = request.DataPlots.Select(k => k.x);
            var y = request.DataPlots.Select(k => k.y);
            var ret = new RegressionDataDto();

            if (x.Count() > MINIMUM_INPUT && y.Count() > MINIMUM_INPUT) // at least 3 items
            {
                var analysisResult = await Task.Run(() => _analysis.FitAnalysis(request.FitMethod, x, y, request.Order));
                ret.Equation = analysisResult.Equation;
                ret.GoodnessMeansures = analysisResult.GoodnessMeansures;
                ret.Coefficients = analysisResult.Coefficients;
            }
            return ret;
        }
    }
}
