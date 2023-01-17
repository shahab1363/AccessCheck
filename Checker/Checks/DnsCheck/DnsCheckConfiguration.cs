using Checker.Validations;

namespace Checker.Checks.DnsCheck
{
    public class DnsCheckConfiguration : ICheckConfiguration
    {
        public CheckTypeEnum Type => CheckTypeEnum.DNS;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public string HostNameOrAddress { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(30);
        public IIPValidation[] IPValidations { get; set; }
        public int SuccessThresholdPercent { get; set; } = 99;
    }
}

