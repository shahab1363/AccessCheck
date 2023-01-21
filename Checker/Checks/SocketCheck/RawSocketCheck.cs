using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace Checker.Checks.SocketCheck
{
    public class RawSocketCheck : ICheck
    {
        public const string BasicGetRequest = @"GET %%AbsoluteUri%% HTTP/1.1
Host: %%Host%%
Connection: Close

";

        public ICheckConfiguration Configuration => configuration;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly RawSocketCheckConfiguration configuration;
        private readonly TimeSpan minInterval;

        public RawSocketCheck(RawSocketCheckConfiguration rawSocketCheckConfiguration, TimeSpan? overrideMinInterval)
        {
            configuration = rawSocketCheckConfiguration;
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
         
            if (configuration.Uri == null)
            {
                throw new ArgumentNullException(nameof(configuration.Uri));
            }

            if (configuration.TextValidations?.Any() != true)
            {
                throw new ArgumentNullException(nameof(configuration.TextValidations));
            }

            try
            {
                return await MethodExtensions.RunWithRetries(
                    ct => InternalRawSocketCheck(ct),
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

        private async Task<CheckResult> InternalRawSocketCheck(CancellationToken ct)
        {
            var request = string.IsNullOrEmpty(configuration.Request)
                ? BasicGetRequest
                : configuration.Request;

            request = request
                .Replace("%%AbsoluteUri%%", configuration.Uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)
                .Replace("%%Host%%", configuration.Uri.Host, StringComparison.OrdinalIgnoreCase);

            var requestBytes = Encoding.ASCII.GetBytes(request);

            using Socket socket = new Socket(configuration.SocketType ?? SocketType.Stream, configuration.ProtocolType ?? ProtocolType.Tcp);
            try
            {
                var stopWatch = Stopwatch.StartNew();
                await socket.ConnectAsync(configuration.Uri.Host, configuration.Port, ct);
                var connectDuration = stopWatch.Elapsed;
                // Send the request.
                var bytesSent = 0;
                while (bytesSent < requestBytes.Length)
                {
                    bytesSent += await socket.SendAsync(requestBytes.AsMemory(bytesSent), SocketFlags.None);
                }
                var requestSentDuration = stopWatch.Elapsed;

                // Do minimalistic buffering assuming ASCII response
                var responseBytes = new byte[256];
                var responseChars = new char[256];

                var response = string.Empty;
                while (true)
                {
                    int bytesReceived = await socket.ReceiveAsync(responseBytes, SocketFlags.None, ct);

                    // Receiving 0 bytes means EOF has been reached
                    if (bytesReceived == 0) break;

                    // Convert byteCount bytes to ASCII characters using the 'responseChars' buffer as destination
                    int charCount = Encoding.ASCII.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);

                    response += responseChars.AsMemory(0, charCount);
                }
                stopWatch.Stop();
                var tags = new Dictionary<string, string>
                {
                    { "ConnectDuration", connectDuration.ToString() },
                    { "RequestDuration." + configuration.Uri.Host + ":" + configuration.Port, connectDuration.ToString() },
                    { "RequestSentDuration", requestSentDuration.ToString() },
                    { "RequestSentDuration." + configuration.Uri.Host + ":" + configuration.Port, requestSentDuration.ToString() },
                    { "ResponseCompleteDuration", stopWatch.Elapsed.ToString() },
                    { "ResponseCompleteDuration." + configuration.Uri.Host + ":" + configuration.Port, stopWatch.Elapsed.ToString() }
                }.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

                var results = new Dictionary<string, CheckResult>();

                foreach (var validation in configuration.TextValidations)
                {
                    var validationResult = await validation.Validate(response);
                    tags.ForEach(t => validationResult.Tags.TryAdd(t.Key, t.Value));
                    results.Add(validation.Name, validationResult);
                }

                var checkResult = CheckResult.CreateBasedOnThreshold(
                    configuration.TextValidations.Length,
                    configuration.SuccessThresholdPercent,
                    configuration.Name,
                    results);

                if (checkResult.Result == CheckResultEnum.Failure)
                {
                    throw new CheckResultException(checkResult);
                }

                return checkResult;
            }
            finally
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    socket.Close();
                }
            }

        }
    }
}
