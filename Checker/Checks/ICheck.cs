using Checker.Configuration;

namespace Checker.Checks
{
    public interface ICheck
    {
        public ICheckConfiguration Configuration { get; }
        public DateTimeOffset LastRun { get; }
        public bool ShouldRun();
        public Task<CheckResult> RunCheck(CancellationToken cancellationToken);
    }
}
