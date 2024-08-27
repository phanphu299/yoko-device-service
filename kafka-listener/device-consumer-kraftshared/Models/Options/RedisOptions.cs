using StackExchange.Redis.Extensions.Core.Configuration;

namespace Device.Consumer.KraftShared.Models.Options
{
    public class RedisOptions
    {
        public string[] HostPorts { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool AbortOnConnectFail { get; set; }
        public bool AllowAdmin { get; set; }
        public int AsyncTimeout { get; set; }
        public bool? UseSsl { get; set; }
        public string? ClientName { get; set; }
        public int ConnectRetry { get; set; }
        public int ConnectTimeout { get; set; }
        public int DefaultDatabase { get; set; }
        public int KeepAlive { get; set; }
        public string? Password { get; set; }
        public string? ServiceName { get; set; }
        public bool Ssl { get; set; }
        public string? SslHost { get; set; }
        public string? SslProtocols { get; set; }
        public int SyncTimeout { get; set; }
        public int? WriteBuffer { get; set; }
        public int ConfigCheckSeconds { get; set; }
        public ConnectionSelectionStrategy ConnectionSelectionStrategy {  get; set; }
        public ServerEnumerationStrategy ServerEnumerationStrategy { get; set; }
    }
}
