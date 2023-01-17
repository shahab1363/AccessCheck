using Checker.Checks;

namespace Checker.Validations
{
    public class ExpectContentLength : IHttpValidation
    {
        public string Name { get; set; } = nameof(ExpectContentLength);
        public int ExpectedContentLength { get; set; }
        public float ThresholdPercent { get; set; }

        public async Task<CheckResult> Validate(HttpResponseMessage httpResponse, string httpResponseBody)
        {
            var contentLength = httpResponse.Headers.TryGetValues("Content-Length", out var lengthHeader) && int.TryParse(lengthHeader.ToString(), out var parsedLength)
                ? parsedLength
                : httpResponseBody == null
                    ? (await httpResponse.Content.ReadAsByteArrayAsync()).Length
                    : httpResponseBody.Length;

            var tags = new Dictionary<string, string>
            {
                { "ContentLength", contentLength.ToString() },
                { "ContentLengthHeader", lengthHeader?.FirstOrDefault() ?? "MISSING_HEADER" }
            }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            if (MeetExpectation(contentLength))
            {
                return new CheckResult(CheckResultEnum.Success, null, tags);
            }

            return new CheckResult(CheckResultEnum.Failure, $"{Name}: Content length expectation failed. Received length: {contentLength}, expected: {ExpectedContentLength} +- {ThresholdPercent}%", tags);
        }

        public bool MeetExpectation(int contentLength)
        {
            if (ThresholdPercent > 0)
            {
                var minLen = Math.Max(0, 100 - ThresholdPercent) / 100 * ExpectedContentLength;
                var maxLen = (100 + ThresholdPercent) / 100 * ExpectedContentLength;

                return (minLen <= contentLength) && (contentLength <= maxLen);
            }

            return contentLength == ExpectedContentLength;
        }
    }
}

