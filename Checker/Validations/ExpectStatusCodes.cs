using Checker.Checks;
using Checker.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace Checker.Validations
{
    public class ExpectStatusCodes : IHttpValidation
    {
        public string Name => this.GetType().Name;
        public int[] ExpectedStatusCodes { get; set; }

        public Task<CheckResult> Validate(HttpResponseMessage httpResponse, string httpResponseBody)
        {
            var tags = new Dictionary<string, string>
            {
                { "StatusCode", httpResponse.StatusCode.ToString() }
            }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            if (ExpectedStatusCodes?.Any() != true)
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.BadConfiguration, $"{Name}: {nameof(ExpectedStatusCodes)} is empty", tags));
            }

            var statusCode = (int)httpResponse.StatusCode;
            if (ExpectedStatusCodes.Contains(statusCode))
            {
                return Task.FromResult(new CheckResult(CheckResultEnum.Success, null, tags));
            }

            return Task.FromResult(new CheckResult(CheckResultEnum.Failure, $"{Name}: Not expected status code: {statusCode} from resposne.", tags));
        }
    }
}

