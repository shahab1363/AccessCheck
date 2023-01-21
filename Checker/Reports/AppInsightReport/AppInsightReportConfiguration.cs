namespace Checker.Reports.AppInsightReport
{
    public class AppInsightReportConfiguration : IReportConfiguration
    {
        public ReportTypeEnum Type => ReportTypeEnum.AppInsight;
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(300);
        public string[] Groups { get; set; } = new[] { "*", "azure" };
    }
}
