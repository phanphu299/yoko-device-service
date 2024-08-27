using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Helper;
using Microsoft.Extensions.Logging;
using Polly;

namespace Function.Helper
{
    public static class RetryHelper
    {
        public static Polly.Wrap.AsyncPolicyWrap GetDbTimeoutRetryStrategyAsync(IBaseLoggerAdapter logger)
        {
            var retryPolicy = PollyPolicyHelpers.GetDbTimeoutRetryAsyncPolicy(logger: logger);
            var circuitBreakerPolicy = PollyPolicyHelpers.GetDbTimeoutCircuitBreakerAsyncPolicy();

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        public static Polly.Wrap.AsyncPolicyWrap GetDbTimeoutRetryStrategyAsync(ILogger logger)
        {
            var retryPolicy = PollyPolicyHelpers.GetDbTimeoutRetryAsyncPolicy(logger: logger);
            var circuitBreakerPolicy = PollyPolicyHelpers.GetDbTimeoutCircuitBreakerAsyncPolicy();

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        public static Polly.Wrap.PolicyWrap GetDbTimeoutRetryStrategySync(ILogger logger)
        {
            var retryPolicy = PollyPolicyHelpers.GetDbTimeoutRetrySyncPolicy(logger: logger);
            var circuitBreakerPolicy = PollyPolicyHelpers.GetDbTimeoutCircuitBreakerSyncPolicy();

            return Policy.Wrap(retryPolicy, circuitBreakerPolicy);
        }
    }
}
