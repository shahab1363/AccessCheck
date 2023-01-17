namespace Checker.Reports.WebhookReport
{
    public class WebhookReportConfiguration : IReportConfiguration
    {
        public ReportTypeEnum Type => ReportTypeEnum.Webhook;
        public string Name { get; set; }
        public Uri[] Uris { get; set; }
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(15);
        public Dictionary<string, string> Headers { get; set; }
        public TimeSpan PerUriTimeOut { get; set; } = TimeSpan.FromSeconds(90);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(300);
    }
}
