using Checker.Checks;
namespace Checker.Reports
{
    public interface IReport
    {
        public IReportConfiguration Configuration { get; }
        public Task<bool> ReportResult(IEnumerable<KeyValuePair<string, CheckResult>> checkResults, CancellationToken cancellationToken);
    }
}
