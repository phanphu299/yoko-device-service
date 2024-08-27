using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Device.Consumer.KraftShared.Services.HealthCheck;
public class KafkaHealthCheck : IHealthCheck
{
    private readonly KafkaHealthCheckService _kafkaHCS;

    public KafkaHealthCheck(KafkaHealthCheckService kafkaHealthCheckService)
    {
        _kafkaHCS = kafkaHealthCheckService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_kafkaHCS.KafkaCompleted
        ? HealthCheckResult.Unhealthy("Kafka Consumer has been stopped")
        : HealthCheckResult.Healthy("Kafka Consumer is working"));
    }
}