using Checker.Checks;
using System.Net;

namespace Checker.Validations
{
    public class MustContain : IHttpValidation, ITextValidation, IIPValidation
    {
        public  virtual string Name => this.GetType().Name;
        public string StringToCheck { get; set; }
        public bool CaseSensitive { get; set; }

        public async Task<CheckResult> Validate(HttpResponseMessage httpResponse, string httpResponseBody)
        {
            if (string.IsNullOrWhiteSpace(StringToCheck))
            {
                return new CheckResult(CheckResultEnum.BadConfiguration, $"{Name}: {nameof(StringToCheck)} is empty");
            }

            if (httpResponseBody == null)
            {
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }

            return CheckForString(httpResponseBody);
        }

        public Task<CheckResult> Validate(string stringToValidate)
        {
            return Task.FromResult(CheckForString(stringToValidate));
        }

        public virtual Task<CheckResult> Validate(IPHostEntry ipHostEntry)
        {
            if ((IPAddress.TryParse(StringToCheck, out var ipAddress) && ipHostEntry.AddressList != null && ipHostEntry.AddressList.Contains(ipAddress)) ||
                (!string.IsNullOrEmpty(ipHostEntry.HostName) && ipHostEntry.HostName.Equals(StringToCheck, CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) ||
                (ipHostEntry.Aliases != null && ipHostEntry.Aliases.Any() && ipHostEntry.Aliases.Contains(StringToCheck, CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)))
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.Success, null));
            }

            return Task.FromResult(new CheckResult(CheckResultEnum.Failure, $"{Name}: Cannot find {StringToCheck} ({(CaseSensitive ? "" : "not ")} case sesitive) in IPHostEntry"));
        }

        internal virtual CheckResult CheckForString(string httpResponseBody)
        {
            if (httpResponseBody.Contains(StringToCheck, CaseSensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return new CheckResult(CheckResultEnum.Success, null);
            }

            return new CheckResult(CheckResultEnum.Failure, $"{Name}: Cannot find {StringToCheck} ({(CaseSensitive ? "" : "not ")} case sesitive) in response (length: {httpResponseBody.Length})");
        }
    }
}

