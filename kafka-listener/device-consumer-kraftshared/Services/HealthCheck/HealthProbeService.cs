using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.KraftShared.Services.HealthCheck;
public class HealthProbeService : BackgroundService
{
    private readonly ILogger<HealthProbeService> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly TcpListener _listener;
    public HealthProbeService(
        IConfiguration configuration, 
        ILogger<HealthProbeService> logger,
        HealthCheckService healthCheckService
        )
    {
        _logger = logger;
        var port = configuration.GetValue<int?>("HealthProbe:TcpPort") ?? 80;
        _listener = new TcpListener(IPAddress.Any, port);
        _healthCheckService = healthCheckService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started health check service.");
        await Task.Yield();
        _listener.Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateHeartbeatAsync(stoppingToken);
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }
        _listener.Stop();
    }

    private async Task UpdateHeartbeatAsync(CancellationToken token)
    {
        try
        {
            // Get health check results
            var result = await _healthCheckService.CheckHealthAsync(token);
            var isHealthy = result.Status == HealthStatus.Healthy;
            if (!isHealthy)
            {
                _listener.Stop();
                _logger.LogInformation("Service is unhealthy. Listener stopped.");
                return;
            }
            _listener.Start();
            while (_listener.Server.IsBound && _listener.Pending())
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                client.Close();
                _logger.LogDebug("Successfully processed health check request.");
            }

            _logger.LogDebug("Heartbeat check executed.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An error occurred while checking heartbeat.");
        }
    }
}
