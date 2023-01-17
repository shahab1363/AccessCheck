using Checker.Checks;

namespace Checker.Validations
{
    public class ExpectExitCode : IExternalAppValidation
    {
        public string Name { get; set; } = nameof(ExpectExitCode);
        public int?[] ExpectedExitCodes { get; set; }

        public Task<CheckResult> Validate(int? exitCode, string? stdOut, string? stdError)
        {
            var tags = new Dictionary<string, string>
            {
                { "ExitCode", exitCode?.ToString() ?? "NULL" }
            }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            if (ExpectedExitCodes?.Any() != true)
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.BadConfiguration, $"{Name}: {nameof(ExpectedExitCodes)} is empty", tags));
            }

            if (ExpectedExitCodes.Contains(exitCode))
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.Success, null, tags));
            }

            return Task.FromResult(new CheckResult(CheckResultEnum.Failure, $"{Name}: Not expected exit code: {exitCode?.ToString() ?? "NULL"}", tags));
        }
    }
}

