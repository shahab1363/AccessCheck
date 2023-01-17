using Checker.Validations;

namespace Checker.Checks.HttpCheck
{
    public class HttpCheckConfiguration : ICheckConfiguration
    {
        public CheckTypeEnum Type => CheckTypeEnum.Http;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public HttpMethodEnum HttpMethod { get; set; } = HttpMethodEnum.Get;
        public Uri[] Uris { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public TimeSpan PerUriTimeOut { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(300);
        public IHttpValidation[] HttpValidations { get; set; }
        public int PerUriSuccessThresholdPercent { get; set; } = 99;
        public int SuccessThresholdPercent { get; set; } = 99;
        public Uri ProxyUri { get; set; }
    }
}

