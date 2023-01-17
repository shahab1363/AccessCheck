using System.Net.Security;
using System.Security.Authentication;

namespace Checker.Checks.TlsCheck
{
    public class TLSCheckConfiguration : ICheckConfiguration
    {
        public CheckTypeEnum Type => CheckTypeEnum.TLS;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; } = 443;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(30);
        public SslProtocols SslProtocol { get; set; }
        public EncryptionPolicy EncryptionPolicy { get; set; } = EncryptionPolicy.RequireEncryption;
    }
}

