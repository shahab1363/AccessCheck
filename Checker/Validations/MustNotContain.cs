using Checker.Checks;
using System.Net;

namespace Checker.Validations
{
    public class MustNotContain : MustContain
    {
        public override string Name => this.GetType().Name;

        internal override CheckResult CheckForString(string httpResponseBody)
        {
            if (!httpResponseBody.Contains(StringToCheck, CaseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return new CheckResult(CheckResultEnum.Success, null);
            }

            return new CheckResult(CheckResultEnum.Failure, $"{Name}: Found {StringToCheck} ({(CaseSensitive ? "" : "not ")} case sesitive) in response (length: {httpResponseBody.Length})");
        }

        public override Task<CheckResult> Validate(IPHostEntry ipHostEntry)
        {
            if ((IPAddress.TryParse(StringToCheck, out var ipAddress) && ipHostEntry.AddressList.Contains(ipAddress)) ||
                ipHostEntry.HostName.Equals(StringToCheck, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) ||
                ipHostEntry.Aliases.Contains(StringToCheck, CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.Failure, $"{Name}: Found {StringToCheck} ({(CaseSensitive ? "" : "not ")} case sesitive) in IPHostEntry"));
            }

            return Task.FromResult(new CheckResult(CheckResultEnum.Success, null));
        }
    }
}

