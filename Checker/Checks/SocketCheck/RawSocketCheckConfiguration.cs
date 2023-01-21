using Checker.Validations;
using System.Net.Sockets;

namespace Checker.Checks.SocketCheck
{
    public class RawSocketCheckConfiguration : ICheckConfiguration
    {
        public virtual CheckTypeEnum Type => CheckTypeEnum.RawSocket;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public virtual SocketType? SocketType { get; set; }
        public virtual ProtocolType? ProtocolType { get; set; }
        public Uri Uri { get; set; }
        public int Port { get; set; }
        public string Request { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(30);
        public ITextValidation[] TextValidations { get; set; }
        public int SuccessThresholdPercent { get; set; } = 99;
        public string[] ReportGroups { get; set; } = new[] { "*" };
    }
}

