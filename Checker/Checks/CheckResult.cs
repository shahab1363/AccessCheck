using Checker.Common.Exceptions;
using Checker.Extensions;

namespace Checker.Checks
{
    public class CheckResult
    {
        public CheckResultEnum Result { get; }
        public string? Description { get; }
        public Dictionary<string, string> Tags { get; }

        public CheckResult(CheckResultEnum result, string? description)
            : this(result, description, new Dictionary<string, string>())
        {
        }

        public CheckResult(CheckResultEnum result, string? description, Dictionary<string, string> tags)
        {
            Result = result;
            Description = description;
            Tags = tags;
        }

        public static CheckResult CreateBasedOnThreshold(
            int totalCount,
            int thresholdPercent,
            string name,
            Dictionary<string, CheckResult> results,
            Dictionary<string, string>? parentTags = null,
            Func<Dictionary<string, CheckResult>, Dictionary<string, string>>? tagAggregator = null)
        {
            var successCount = results.Where(x => x.Value.Result == CheckResultEnum.Success).Count();
            var successPercent = successCount * 100.0 / totalCount;

            var aggregatedTags = tagAggregator?.Invoke(results) ?? new Dictionary<string, string>();

            if (tagAggregator == null)
            {
                aggregatedTags = results.Values
                    .SelectMany(cr => cr.Tags)
                    .GroupBy(kv => kv.Key)
                    .ToDictionary(
                        g => g.Key + (g.Distinct().Count() == 1 ? "" : ".Aggregated"),
                        g =>
                        {
                            if (g.Distinct().Count() == 1)
                            {
                                var distinctValue = g.Distinct().FirstOrDefault().Value;
                                if (string.IsNullOrEmpty(distinctValue))
                                {
                                    return "EMPTY";
                                }

                                return distinctValue;
                            }

                            var nonEmptyValues = g.Where(v => !string.IsNullOrWhiteSpace(v.Value)).Select(v => v.Value).ToArray();

                            if (nonEmptyValues.Any())
                            {
                                if (nonEmptyValues.All(v => double.TryParse(v, out _)))
                                {
                                    return nonEmptyValues.Average(v => double.Parse(v)).ToString("#.##");
                                }

                                if (nonEmptyValues.All(v => TimeSpan.TryParse(v, out _)))
                                {
                                    return TimeSpan.FromMilliseconds(nonEmptyValues.Average(v => TimeSpan.Parse(v).TotalMilliseconds)).ToString();
                                }
                            }

                            return string.Join(", ", g.Select(v => string.IsNullOrEmpty(v.Value) ? "EMPTY" : v.Value).OrderBy(v => v));
                        });
            }

            parentTags?.ForEach(kv => aggregatedTags[aggregatedTags.ContainsKey(kv.Key) ? "parent." + kv.Key : kv.Key] = kv.Value);

            if (successPercent < thresholdPercent)
            {
                return new CheckResult(
                    CheckResultEnum.Failure,
                    $"{name}: Less than {thresholdPercent}% passed (Passed {successCount} out of {totalCount}: {successPercent}%). Check Failed. Details: " +
                    string.Join(", ", results.Select(x => $"{x.Key}: {x.Value.Result}{(string.IsNullOrEmpty(x.Value.Description) ? "" : $" ({x.Value.Description})")}")),
                    aggregatedTags);
            }

            return new CheckResult(
                 CheckResultEnum.Success,
                 $"{name}: More than {thresholdPercent}% passed (Passed {successCount} out of {totalCount}: {successPercent}%). Check Succeeded. Details: " +
                 string.Join(", ", results.Select(x => $"{x.Key}: {x.Value.Result}{(string.IsNullOrEmpty(x.Value.Description) ? "" : $" ({x.Value.Description})")}")),
                 aggregatedTags);
        }

        public static CheckResult FromException(string callerName, Exception exception)
        {
            if (exception is CheckResultException checkResultException)
            {
                return checkResultException.FailedCheckResult;
            }

            if (exception is AggregateException aggregateException)
            {
                if (aggregateException.InnerExceptions != null && aggregateException.InnerExceptions.Count() > 1)
                {
                    exception = aggregateException;
                }

                exception = aggregateException.InnerExceptions?.FirstOrDefault() ?? aggregateException.InnerException ?? exception;
            }

            return new CheckResult(
                CheckResultEnum.Failure,
                exception.ToString(),
                new Dictionary<string, string> {
                    { callerName + ".exception", exception.GetType().Name }
                });
        }
    }
}
