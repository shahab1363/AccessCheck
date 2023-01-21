using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Diagnostics;

namespace Checker.Checks.HttpCheck
{
    public class HttpCheck : ICheck
    {
        public ICheckConfiguration Configuration => configuration;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly HttpCheckConfiguration configuration;
        private readonly Func<Uri?, HttpClient> httpClientProvider;
        private readonly TimeSpan minInterval;

        public HttpCheck(HttpCheckConfiguration httpCheckConfiguration, TimeSpan? overrideMinInterval, Func<Uri?, HttpClient> httpClientProvider)
        {
            this.configuration = httpCheckConfiguration;
            this.httpClientProvider = httpClientProvider;
            this.minInterval = this.configuration.MinInterval ?? overrideMinInterval ?? TimeSpan.Zero;
        }

        public bool ShouldRun()
        {
            if (DateTimeOffset.UtcNow - this.LastRun > minInterval)
            {
                return true;
            }

            return false;
        }

        public async Task<CheckResult> RunCheck(CancellationToken cancellationToken)
        {
            LastRun = DateTimeOffset.UtcNow;

            if (configuration.Uris?.Any() != true)
            {
                throw new ArgumentNullException(nameof(configuration.Uris));
            }

            var pendingTasks = new Dictionary<string, Task<CheckResult>>();

            foreach (var uri in configuration.Uris)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                pendingTasks.Add(uri.ToString(), Task.Run(async () =>
                {
                    try
                    {
                        return await MethodExtensions.RunWithRetries(
                            ct => InternalHttpUriCheck(configuration.Name, uri, ct),
                            TimeSpan.FromMinutes(5), // configuration.PerUriTimeOut,
                            configuration.MaxRetries,
                            configuration.RetryDelay,
                            exc => true,
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        return CheckResult.FromException(this.GetType().Name, exception);
                    }
                }));
            }

            var timeOutTask = Task.Delay(configuration.TimeOut);
            await Task.WhenAny(timeOutTask, Task.WhenAll(pendingTasks.Values));

            var uriResults = pendingTasks.ToDictionary(
                x => x.Key,
                x => x.Value.IsCompletedSuccessfully
                    ? x.Value.Result
                    : x.Value.IsFaulted
                        ? CheckResult.FromException(this.GetType().Name, x.Value.Exception ?? (Exception)new UnknownException($"Uri {x.Key} check failed with unknown error"))
                        : CheckResult.FromException(this.GetType().Name, new TimeoutException($"Uri {x.Key} check timed out after {configuration.TimeOut}.")));

            var checkResult = CheckResult.CreateBasedOnThreshold(
                configuration.Uris.Length,
                configuration.SuccessThresholdPercent,
                $"{configuration.Name}",
                uriResults);

            return checkResult;
        }

        private async Task<CheckResult> InternalHttpUriCheck(string name, Uri uri, CancellationToken ct)
        {
            HttpClient httpClient;

            httpClient = httpClientProvider(configuration.ProxyUri);

            if (httpClient == null)
            {
                throw new Exception("HttpClient is null");
            }

            var httpMethod = GetHttpMethod(configuration.HttpMethod);

            var request = new HttpRequestMessage(httpMethod, uri);
            if (configuration.Headers?.Any() == true)
            {
                foreach (var kv in configuration.Headers)
                {
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }

            if (!string.IsNullOrEmpty(configuration.Body))
            {
                request.Content = new StringContent(configuration.Body);
            }

            var stopWatch = Stopwatch.StartNew();
            var response = await httpClient.SendAsync(request, ct);
            stopWatch.Stop();
            var tags = new Dictionary<string, string>
            {
                { "RequestDuration", stopWatch.Elapsed.ToString() },
                { "RequestDuration." + uri, stopWatch.Elapsed.ToString() }
            }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            if (configuration.HttpValidations == null || !configuration.HttpValidations.Any())
            {
                try
                {
                    response.EnsureSuccessStatusCode();
                    return new CheckResult(CheckResultEnum.Success, null, tags);
                }
                catch (Exception exc)
                {
                    var failedResult = CheckResult.FromException($"{this.GetType().Name}.{name}({uri})", exc);
                    tags.ForEach(t => failedResult.Tags.TryAdd(t.Key, t.Value));
                    return failedResult;
                }
            }
            else
            {
                var results = new Dictionary<string, CheckResult>();

                var responseBody = string.Empty;
                try
                {
                    responseBody = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    ;
                }

                foreach (var validation in configuration.HttpValidations)
                {
                    var validationResult = await validation.Validate(response, responseBody);
                    tags.ForEach(t => validationResult.Tags.TryAdd(t.Key, t.Value));
                    results.Add(validation.Name, validationResult);
                }

                var checkResult = CheckResult.CreateBasedOnThreshold(
                    configuration.HttpValidations.Length,
                    configuration.PerUriSuccessThresholdPercent,
                    $"{name}({uri})",
                    results);

                if (checkResult.Result == CheckResultEnum.Failure)
                {
                    throw new CheckResultException(checkResult);
                }

                return checkResult;
            }
        }

        private System.Net.Http.HttpMethod GetHttpMethod(HttpMethodEnum httpMethod)
        {
            switch (httpMethod)
            {
                case HttpMethodEnum.Post:
                    return System.Net.Http.HttpMethod.Post;
                case HttpMethodEnum.Put:
                    return System.Net.Http.HttpMethod.Put;
                case HttpMethodEnum.Delete:
                    return System.Net.Http.HttpMethod.Delete;
                case HttpMethodEnum.Get:
                default:
                    return System.Net.Http.HttpMethod.Get;
            }
        }
    }
}
