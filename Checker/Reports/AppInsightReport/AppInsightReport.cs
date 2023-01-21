using Checker.Checks;
using Checker.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace Checker.Reports.AppInsightReport
{
    // todo: add app insight tags based on user network information
    public class AppInsightReport : IReport
    {
        public IReportConfiguration Configuration => configuration;

        private readonly AppInsightReportConfiguration configuration;
        private readonly string clientUID;
        private readonly Lazy<TelemetryClient> telemetryClient;
        public AppInsightReport(AppInsightReportConfiguration appInsightReportConfiguration, string clientUID)
        {
            this.configuration = appInsightReportConfiguration;
            this.clientUID = clientUID;
            this.telemetryClient = new Lazy<TelemetryClient>(GetTelemetryClient, isThreadSafe: true);
        }

        private TelemetryClient GetTelemetryClient()
        {
            var telemetryConfiguration = new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration
            {
                ConnectionString = configuration.ConnectionString,
                TelemetryChannel = new InMemoryChannel()
            };

            var telemetryClient = new TelemetryClient(telemetryConfiguration);
            telemetryClient.Context.GlobalProperties.Add("ClientUID", clientUID);
            configuration.Tags?.ForEach(globalTag => telemetryClient.Context.GlobalProperties.Add(globalTag));

            return telemetryClient;
        }

        public async Task<bool> ReportResult(IEnumerable<KeyValuePair<string, CheckResult>> checkResults, CancellationToken cancellationToken)
        {
            var timestamp = DateTimeOffset.UtcNow;
            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                throw new ArgumentNullException(nameof(configuration.ConnectionString));
            }

            if (checkResults?.Any() != true)
            {
                return true;
            }

            var result = false;

            try
            {
                foreach (var checkResultKV in checkResults)
                {
                    var availabilityTelemetry = new AvailabilityTelemetry
                    {
                        RunLocation = clientUID,
                        Name = checkResultKV.Key,
                        Success = checkResultKV.Value.Result == CheckResultEnum.Success,
                        Message = $"{checkResultKV.Value.Result}: {checkResultKV.Value.Description}",
                        Sequence = timestamp.Ticks.ToString(),
                        Timestamp = timestamp,
                    };

                    checkResultKV.Value.Tags?.ForEach(kv => availabilityTelemetry.Properties.Add(kv));

                    telemetryClient.Value.TrackAvailability(availabilityTelemetry);
                }
            }
            finally
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    new CancellationTokenSource(configuration.TimeOut).Token);

                result = await telemetryClient.Value.FlushAsync(cts.Token);
            }

            return result;
        }
    }
}
