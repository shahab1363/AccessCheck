using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Net.NetworkInformation;

namespace Checker.Checks.PingCheck
{
    public class PingCheck : ICheck
    {
        public CheckTypeEnum Type => CheckTypeEnum.Ping;
        public string Name => configuration.Name;
        public int Order => configuration.Order;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly PingCheckConfiguration configuration;
        private readonly TimeSpan minInterval;

        public PingCheck(PingCheckConfiguration pingCheckConfiguration, TimeSpan? overrideMinInterval)
        {
            configuration = pingCheckConfiguration;
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
        
            if (configuration.HostNames?.Any() != true)
            {
                throw new ArgumentNullException(nameof(configuration.HostNames));
            }

            var results = new Dictionary<string, CheckResult>();

            foreach (var hostName in configuration.HostNames)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                CheckResult hostPingResult;
                try
                {
                    hostPingResult = await MethodExtensions.RunWithRetries(
                        ct => InternalPingCheck(configuration.Name, hostName, ct),
                        configuration.PerHostTimeOut,
                        configuration.MaxRetries,
                        configuration.RetryDelay,
                        exc => true,
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    hostPingResult = CheckResult.FromException(this.GetType().Name, exception);
                }

                results.Add(hostName, hostPingResult);
            }

            return CheckResult.CreateBasedOnThreshold(
                configuration.HostNames.Length,
                configuration.SuccessThresholdPercent,
                $"{configuration.Name}",
                results);
        }

        private async Task<CheckResult> InternalPingCheck(string name, string hostName, CancellationToken ct)
        {
            using var ping = new Ping();

            var reply = await ping.SendPingAsync(hostName, (int)configuration.PerHostTimeOut.TotalMilliseconds);

            var tags = new Dictionary<string, string>
            {
                { "RoundtripTime", reply.RoundtripTime.ToString() },
                { "Status", reply.Status.ToString() },
                { "IPAddress", reply.Address.ToString() },
                { "HostName", hostName },
            }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            if (reply is { Status: IPStatus.Success })
            {
                if (configuration.IPValidations == null || !configuration.IPValidations.Any())
                {
                    return new CheckResult(CheckResultEnum.Success, $"{name}: {hostName} ping was successful. Address: {reply.Address} - Roundtrip time: {reply.RoundtripTime} - Ttl: {reply.Options?.Ttl}", tags);
                }
                else
                {
                    var ipHostEntry = new System.Net.IPHostEntry
                    {
                        AddressList = new[] { reply.Address }
                    };
                    var results = new Dictionary<string, CheckResult>();

                    foreach (var validation in configuration.IPValidations)
                    {
                        var validationResult = await validation.Validate(ipHostEntry);
                        tags.ForEach(t => validationResult.Tags.TryAdd(t.Key, t.Value));
                        results.Add(validation.Name, validationResult);
                    }

                    var checkResult = CheckResult.CreateBasedOnThreshold(
                        configuration.IPValidations.Length,
                        configuration.PerHostSuccessThresholdPercent,
                        $"{name}({hostName})",
                        results);

                    if (checkResult.Result == CheckResultEnum.Failure)
                    {
                        throw new CheckResultException(checkResult);
                    }

                    return checkResult;
                }
            }
            else
            {
                throw new Exception("Ping failed");
            }
        }
    }
}
