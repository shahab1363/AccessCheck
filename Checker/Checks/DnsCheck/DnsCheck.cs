using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Diagnostics;
using System.Net;

namespace Checker.Checks.DnsCheck
{
    public class DnsCheck : ICheck
    {
        public CheckTypeEnum Type => CheckTypeEnum.DNS;
        public string Name => configuration.Name;
        public int Order => configuration.Order;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly DnsCheckConfiguration configuration;
        private readonly TimeSpan minInterval;

        public DnsCheck(DnsCheckConfiguration dnsCheckConfiguration, TimeSpan? overrideMinInterval)
        {
            configuration = dnsCheckConfiguration;
            minInterval = configuration.MinInterval ?? overrideMinInterval ?? TimeSpan.Zero;
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

            if (configuration.HostNameOrAddress == null)
            {
                throw new ArgumentNullException(nameof(configuration.HostNameOrAddress));
            }

            if (configuration.IPValidations?.Any() != true)
            {
                throw new ArgumentNullException(nameof(configuration.IPValidations));
            }

            try
            {
                return await MethodExtensions.RunWithRetries(
                    ct => InternalDnsCheck(ct),
                    configuration.TimeOut,
                    configuration.MaxRetries,
                    configuration.RetryDelay,
                    exc => true,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                return CheckResult.FromException(this.GetType().Name, exception);
            }
        }

        private async Task<CheckResult> InternalDnsCheck(CancellationToken ct)
        {
            var stopWatch = Stopwatch.StartNew();
            var iPHostEntry = await Dns.GetHostEntryAsync(configuration.HostNameOrAddress, ct);
            stopWatch.Stop();
            var tags = new Dictionary<string, string>
            {
                { "RequestDuration", stopWatch.Elapsed.ToString() },
                { "RequestDuration." + configuration.HostNameOrAddress, stopWatch.Elapsed.ToString() },
            };

            if (iPHostEntry.AddressList?.Any() == true)
            {
                tags.Add("IPAddress." + configuration.HostNameOrAddress, string.Join(",", iPHostEntry.AddressList.Select(a => a.ToString()).OrderBy(x => x)));
            }
            if (iPHostEntry.Aliases?.Any() == true)
            {
                tags.Add("Aliases." + configuration.HostNameOrAddress, string.Join(",", iPHostEntry.Aliases.OrderBy(x => x)));
            }
            if (!string.IsNullOrEmpty(iPHostEntry.HostName))
            {
                tags.Add("ResultHostName." + configuration.HostNameOrAddress, iPHostEntry.HostName);
            }

            tags = tags.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            var results = new Dictionary<string, CheckResult>();

            foreach (var validation in configuration.IPValidations)
            {
                var validationResult = await validation.Validate(iPHostEntry);
                tags.ForEach(t => validationResult.Tags.TryAdd(t.Key, t.Value));
                results.Add(validation.Name, validationResult);
            }

            var checkResult = CheckResult.CreateBasedOnThreshold(
                configuration.IPValidations.Length,
                configuration.SuccessThresholdPercent,
                configuration.Name,
                results);

            if (checkResult.Result == CheckResultEnum.Failure)
            {
                throw new CheckResultException(checkResult);
            }

            return checkResult;
        }
    }
}
