using Checker.Checks;
using Checker.Extensions;
using System.Net.Http.Json;

namespace Checker.Reports.WebhookReport
{
    public class WebhookReport : IReport
    {
        public IReportConfiguration Configuration => configuration;

        private readonly WebhookReportConfiguration configuration;
        private readonly Func<HttpClient> httpClientProvider;
        private readonly string clientUID;

        public WebhookReport(WebhookReportConfiguration webhookReportConfiguration, Func<HttpClient> httpClientProvider, string clientUID)
        {
            this.configuration = webhookReportConfiguration;
            this.httpClientProvider = httpClientProvider;
            this.clientUID = clientUID;
        }

        public async Task<bool> ReportResult(IEnumerable<KeyValuePair<string, CheckResult>> checkResults, CancellationToken cancellationToken)
        {
            if (configuration.Uris?.Any() != true)
            {
                throw new ArgumentNullException(nameof(configuration.Uris));
            }

            if (checkResults?.Any() != true)
            {
                return true;
            }

            var pendingTasks = new Dictionary<string, Task<bool>>();
            foreach (var uri in configuration.Uris)
            {
                pendingTasks.Add(uri.ToString(), Task.Run(async () =>
                {
                    try
                    {
                        return await MethodExtensions.RunWithRetries(
                            ct => InternalSendWebhook(uri, checkResults, ct),
                            configuration.PerUriTimeOut,
                            configuration.MaxRetries,
                            configuration.RetryDelay,
                            exc => true,
                            cancellationToken);
                    }
                    catch
                    {
                        return false;
                    }
                }));
            }

            var timeOutTask = Task.Delay(configuration.TimeOut, cancellationToken);
            await Task.WhenAny(timeOutTask, Task.WhenAll(pendingTasks.Values));

            if (pendingTasks.Values.Any(x => x.IsCompletedSuccessfully && x.Result))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> InternalSendWebhook(Uri uri, IEnumerable<KeyValuePair<string, CheckResult>> checkResults, CancellationToken ct)
        {
            var httpClient = this.httpClientProvider();
            if (httpClient == null)
            {
                throw new Exception("HttpClient is null");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            if (configuration.Headers?.Any() == true)
            {
                foreach (var kv in configuration.Headers)
                {
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }

            if (!string.IsNullOrEmpty(clientUID))
            {
                request.Headers.TryAddWithoutValidation("ClientUID", clientUID);
            }

            request.Content = JsonContent.Create(checkResults, null, SerializationExtensions.GetDefaultSerializationOptions(true));

            var response = await httpClient.SendAsync(request, ct);

            response.EnsureSuccessStatusCode();
            return true;
        }
    }
}
