using Checker.Checks;

namespace Checker.Common.Exceptions
{
    public class CheckResultException : Exception
    {
        public CheckResult FailedCheckResult { get; private set; }

        public CheckResultException(CheckResult failedCheckResult)
        {
            FailedCheckResult = failedCheckResult;
        }

        public CheckResultException(CheckResult failedCheckResult, string? message) : base(message)
        {
            FailedCheckResult = failedCheckResult;
        }

        public CheckResultException(CheckResult failedCheckResult, string? message, Exception? innerException) : base(message, innerException)
        {
            FailedCheckResult = failedCheckResult;
        }
    }
}
