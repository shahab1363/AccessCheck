using Checker.Checks;
namespace Checker.Reports
{
    public interface IReport
    {
        public ReportTypeEnum Type { get; }
        public Task<bool> ReportResult(IEnumerable<KeyValuePair<string, CheckResult>> checkResults, CancellationToken cancellationToken);
    }
}
