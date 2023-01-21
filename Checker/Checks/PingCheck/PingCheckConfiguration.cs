using Checker.Validations;

namespace Checker.Checks.PingCheck
{
    public class PingCheckConfiguration : ICheckConfiguration
    {
        public CheckTypeEnum Type => CheckTypeEnum.Ping;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public string[] HostNames { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan PerHostTimeOut { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(90);
        public IIPValidation[] IPValidations { get; set; }
        public int PerHostSuccessThresholdPercent { get; set; } = 99;
        public int SuccessThresholdPercent { get; set; } = 99;
        public string[] ReportGroups { get; set; } = new[] { "*" };
    }
}

