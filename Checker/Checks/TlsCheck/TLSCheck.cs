using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;

namespace Checker.Checks.TlsCheck
{
    public class TLSCheck : ICheck
    {
        public ICheckConfiguration Configuration => configuration;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly TLSCheckConfiguration configuration;
        private readonly TimeSpan minInterval;

        public TLSCheck(TLSCheckConfiguration tlsCheckConfiguration, TimeSpan? overrideMinInterval)
        {
            configuration = tlsCheckConfiguration;
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

            if (string.IsNullOrWhiteSpace(configuration.HostName))
            {
                throw new ArgumentNullException(nameof(configuration.HostName));
            }

            try
            {
                return await MethodExtensions.RunWithRetries(
                    ct => InternalTlsCheck(ct),
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

        private async Task<CheckResult> InternalTlsCheck(CancellationToken ct)
        {
            var success = false;
            var stopWatch = new Stopwatch();

            using var client = new TcpClient();
            try
            {
                stopWatch.Start();
                await client.ConnectAsync(configuration.HostName, configuration.Port, ct);

                if (client.Connected)
                {
                    var stream = client.GetStream();
                    // Don't dispose underlying stream.
                    using (var sslStream = new SslStream(stream, true))
                    {
                        sslStream.ReadTimeout = 15000;
                        sslStream.WriteTimeout = 15000;
                        var sslClientAuthenticationOptions = new SslClientAuthenticationOptions
                        {
                            EnabledSslProtocols = configuration.SslProtocol,
                            EncryptionPolicy = configuration.EncryptionPolicy,
                            TargetHost = configuration.HostName,
                        };

                        await sslStream.AuthenticateAsClientAsync(sslClientAuthenticationOptions, ct);
                        success = sslStream.IsAuthenticated;
                        sslStream.Close();
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                client.Close();
            }

            var tags = success
                ? new Dictionary<string, string>
                    {
                        { "RequestDuration", stopWatch.Elapsed.ToString() },
                        { "RequestDuration." + configuration.HostName, stopWatch.Elapsed.ToString() },
                    }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value)
                : new Dictionary<string, string>();

            if (success)
            {
                return new CheckResult(CheckResultEnum.Success, null, tags);
            }

            throw new CheckResultException(new CheckResult(CheckResultEnum.Failure, "SSL verification failed", tags));
        }
    }
}
