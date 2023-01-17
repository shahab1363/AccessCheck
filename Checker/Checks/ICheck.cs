using Checker.Configuration;

namespace Checker.Checks
{
    public interface ICheck : ICheckConfiguration
    {
        public DateTimeOffset LastRun { get; }
        public bool ShouldRun();
        public Task<CheckResult> RunCheck(CancellationToken cancellationToken);
    }
}
