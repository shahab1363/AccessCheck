using Checker.Reports;
namespace Checker.Configuration
{
    public class CheckerConfiguration
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(60);
        public CheckerStep AppStartStep { get; set; }
        public CheckerStep[] PeriodicChecksSteps { get; set; }
        public CheckerStep AppShutdownStep { get; set; }
        public IReportConfiguration[] ReportConfigurations { get; set; }
        //public ReportSelectionEnum ReportSelection { get; set; }
        //public int ReportSelectionPercent { get; set; }
        public string ScheduledRunTime { get; set; }
    }
}
