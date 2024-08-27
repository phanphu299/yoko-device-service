
namespace Device.Consumer.KraftShared.Services.HealthCheck;
public class KafkaHealthCheckService 
{
    private volatile bool _isLeftGroup;

    public bool KafkaCompleted
    {
        get => _isLeftGroup;
        set => _isLeftGroup = value;
    }
}