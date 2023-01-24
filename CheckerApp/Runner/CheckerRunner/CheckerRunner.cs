using Checker.Checks;
using Checker.Checks.DnsCheck;
using Checker.Checks.HttpCheck;
using Checker.Checks.PingCheck;
using Checker.Checks.SocketCheck;
using Checker.Checks.TlsCheck;
using Checker.Configuration;
using Checker.Extensions;
using Checker.Reports.AppInsightReport;
using Checker.Reports.WebhookReport;
using CheckerLib.Checks.ExternalAppCheck;
using CheckerLib.Common.Helpers;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CheckerApp.Runner.CheckerRunner
{
    internal class CheckerRunner : IRunner<CheckerConfiguration>
    {
        private CheckerConfiguration configuration;
        private string clientId;
        private Scheduler scheduler;
        private CancellationTokenSource runCTS;
        private CancellationTokenSource cleanUpCTS = new CancellationTokenSource();

        private bool _isRunning = false;
        private Task runningChecksTask = Task.CompletedTask;
        private Task appStartedTask = Task.CompletedTask;
        private Task appShutdownTask = Task.CompletedTask;
        private Task startBusyTask = Task.CompletedTask;
        private Task endBusyTask = Task.CompletedTask;

        private Lazy<IDictionary<CheckGroup, List<ICheck>>?> periodicChecks;
        private Lazy<IDictionary<CheckGroup, List<ICheck>>?> runBeforePeriodicChecks;
        private Lazy<IDictionary<CheckGroup, List<ICheck>>?> runAfterPeriodicCheck;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value)
                {
                    return;
                }
                _isRunning = value;
                IsRunningChanged();
                IsRunningChange?.Invoke(this, value);
            }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }
                _isBusy = value;
                IsBusyChanged();
                IsBusyChange?.Invoke(this, value);
            }
        }

        public bool IsInitialized { get; private set; }

        public event EventHandler<bool> IsRunningChange;
        public event EventHandler<bool> IsBusyChange;

        public Task Initialize(CheckerConfiguration configuration, string clientId)
        {
            this.configuration = configuration;
            this.clientId = clientId;
            this.scheduler = new Scheduler(configuration.ScheduledRunTime);

            periodicChecks = new Lazy<IDictionary<CheckGroup, List<ICheck>>?>(() =>
            {
                if (configuration.PeriodicChecksStep?.CheckGroups?.Any() == true)
                {
                    return LoadChecks(configuration.PeriodicChecksStep.CheckGroups);
                }
                return null;
            });

            runBeforePeriodicChecks = new Lazy<IDictionary<CheckGroup, List<ICheck>>?>(() =>
            {
                if (configuration.RunBeforePeriodicChecksStep?.CheckGroups?.Any() == true)
                {
                    return LoadChecks(configuration.RunBeforePeriodicChecksStep.CheckGroups);
                }
                return null;
            });

            runAfterPeriodicCheck = new Lazy<IDictionary<CheckGroup, List<ICheck>>?>(() =>
            {
                if (configuration.RunAfterPeriodicChecksStep?.CheckGroups?.Any() == true)
                {
                    return LoadChecks(configuration.RunAfterPeriodicChecksStep.CheckGroups);
                }
                return null;
            });

            IsInitialized = true;
            return Task.CompletedTask;
        }

        private async Task AwaitAndGetNewTask(Func<Task> taskFunc, params Task[] tasksToWaitBeforeCreatingNewOne)
        {
            foreach (var task in tasksToWaitBeforeCreatingNewOne)
            {
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                }
            }

            await taskFunc();
        }

        private void IsRunningChanged()
        {
            if (IsRunning)
            {
                appStartedTask = AwaitAndGetNewTask(() =>
                {
                    if (configuration.AppStartStep?.CheckGroups?.Any() == true)
                    {
                        return RunTask(LoadChecks(configuration.AppStartStep.CheckGroups), configuration.AppStartStep, cleanUpCTS.Token);
                    }
                    return Task.CompletedTask;
                }, appStartedTask, appShutdownTask);
            }
            else
            {
                appStartedTask = AwaitAndGetNewTask(() =>
                {
                    if (configuration.AppShutdownStep?.CheckGroups?.Any() == true)
                    {
                        return RunTask(LoadChecks(configuration.AppShutdownStep.CheckGroups), configuration.AppShutdownStep, cleanUpCTS.Token);
                    }
                    return Task.CompletedTask;
                }, appShutdownTask, appStartedTask);
            }
        }

        private void IsBusyChanged()
        {
            if (IsBusy)
            {
                startBusyTask = AwaitAndGetNewTask(() =>
                {
                    if (runBeforePeriodicChecks.Value != null && runBeforePeriodicChecks.Value.Any())
                    {
                        return RunTask(runBeforePeriodicChecks.Value, configuration.RunBeforePeriodicChecksStep, cleanUpCTS.Token);
                    }
                    return Task.CompletedTask;
                }, startBusyTask, endBusyTask);
            }
            else
            {
                startBusyTask = AwaitAndGetNewTask(() =>
                {
                    if (runAfterPeriodicCheck.Value != null && runAfterPeriodicCheck.Value.Any())
                    {
                        return RunTask(runAfterPeriodicCheck.Value, configuration.RunAfterPeriodicChecksStep, cleanUpCTS.Token);
                    }
                    return Task.CompletedTask;
                }, endBusyTask, startBusyTask);
            }
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            if (!IsInitialized)
            {
                throw new Exception("Runner is not initialized");
            }

            if (IsRunning)
            {
                throw new Exception("Runner is already running");
            }

            try
            {
                this.IsRunning = true;
                if (configuration.AppStartStep?.FinishBeforeNextStep ?? false)
                {
                    await appStartedTask.ConfigureAwait(false);
                }
                runCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var ct = runCTS.Token;
                var lastStartTime = DateTime.MinValue;

                if (periodicChecks.Value == null || !periodicChecks.Value.Any())
                {
                    return;
                }

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(GetIntervalDelay(lastStartTime), ct);
                    lastStartTime = DateTime.Now;

                    if (!scheduler.IsInTimeWindows())
                    {
                        continue;
                    }

                    try
                    {
                        this.IsBusy = true;

                        if (configuration.RunBeforePeriodicChecksStep?.FinishBeforeNextStep ?? false)
                        {
                            await startBusyTask.ConfigureAwait(false); // finish trigger run before strating checks
                        }

                        if (!runningChecksTask.IsCompleted)
                        {
                            await runningChecksTask.ConfigureAwait(false);
                        }

                        runningChecksTask = RunTask(periodicChecks.Value, configuration.PeriodicChecksStep, ct);
                        if (configuration.PeriodicChecksStep?.FinishBeforeNextStep ?? true)
                        {
                            await runningChecksTask.ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        this.IsBusy = false;

                        if (configuration.RunAfterPeriodicChecksStep?.FinishBeforeNextStep ?? false)
                        {
                            await endBusyTask.ConfigureAwait(false); // wait for trigger run to finish before ending current check
                        }
                    }
                }
            }
            finally
            {
                this.IsRunning = false;

                if (configuration.AppShutdownStep?.FinishBeforeNextStep ?? false)
                {
                    await appShutdownTask.ConfigureAwait(false);
                }
            }
        }

        private IDictionary<CheckGroup, List<ICheck>> LoadChecks(CheckGroup[] checkGroups)
        {
            var result = new Dictionary<CheckGroup, List<ICheck>>();
            foreach (var checkGroup in checkGroups)
            {
                var checks = LoadCheckGroup(checkGroup).ToList();
                if (checks.Any())
                {
                    result.Add(checkGroup, checks);
                }
            }

            return result;
        }

        private IEnumerable<ICheck> LoadCheckGroup(CheckGroup checkGroup)
        {
            foreach (var checkConfiguration in checkGroup.CheckConfigurations)
            {
                switch (checkConfiguration.Type)
                {
                    case CheckTypeEnum.RawSocket:
                        if (checkConfiguration is RawSocketCheckConfiguration rawSocketCheckConfiguration)
                        {
                            yield return new RawSocketCheck(rawSocketCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.Http:
                        if (checkConfiguration is HttpCheckConfiguration httpCheckConfiguration)
                        {
                            yield return new HttpCheck(httpCheckConfiguration, checkGroup.MinInterval, HttpClientProvider);
                        }
                        break;
                    case CheckTypeEnum.TCP:
                        if (checkConfiguration is TCPCheckConfiguration tcpCheckConfiguration)
                        {
                            yield return new TCPCheck(tcpCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.UDP:
                        if (checkConfiguration is UDPCheckConfiguration udpCheckConfiguration)
                        {
                            yield return new UDPCheck(udpCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.DNS:
                        if (checkConfiguration is DnsCheckConfiguration dnsCheckConfiguration)
                        {
                            yield return new DnsCheck(dnsCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.TLS:
                        if (checkConfiguration is TLSCheckConfiguration tlsCheckConfiguration)
                        {
                            yield return new TLSCheck(tlsCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.Ping:
                        if (checkConfiguration is PingCheckConfiguration pingCheckConfiguration)
                        {
                            yield return new PingCheck(pingCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.ExternalApp:
                        if (checkConfiguration is ExternalAppCheckConfiguration externalAppCheckConfiguration)
                        {
                            yield return new ExternalAppCheck(externalAppCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                }
            }
        }

        public Task Stop()
        {
            if (IsRunning)
            {
                runCTS?.Cancel();
            }

            return Task.CompletedTask;
        }

        public async Task Cleanup(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => cleanUpCTS.Cancel());

            await appStartedTask.ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await startBusyTask.ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await runningChecksTask.ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await endBusyTask.ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await appShutdownTask.ConfigureAwait(false);
        }

        private int GetIntervalDelay(DateTime lastStartTime)
            => Math.Max(0, (int)(configuration.Interval - (DateTime.Now - lastStartTime)).TotalMilliseconds);

        private Task RunTask(IDictionary<CheckGroup, List<ICheck>> checksToRun, CheckerStep step, CancellationToken cancellationToken)
            => RunTask(checksToRun, step?.MinDuration ?? TimeSpan.Zero, step?.MaxDuration ?? TimeSpan.FromDays(30), step?.SendReport ?? true, cancellationToken);

        private async Task RunTask(IDictionary<CheckGroup, List<ICheck>> checksToRun, TimeSpan minDuration, TimeSpan maxDuration, bool sendReport, CancellationToken cancellationToken)
        {
            if (maxDuration == TimeSpan.Zero)
            {
                maxDuration = TimeSpan.FromDays(30);
            }

            var maxDurationCTS = new CancellationTokenSource(maxDuration);
            var ct = CancellationTokenSource.CreateLinkedTokenSource(maxDurationCTS.Token, cancellationToken).Token;

            var stopwatch = Stopwatch.StartNew();
            var checkResults = await RunChecks(checksToRun, ct);

            if (sendReport)
            {
                await SendReport(clientId, stopwatch.Elapsed, checkResults, ct);
            }

            var waitFor = minDuration - stopwatch.Elapsed;
            if (waitFor > TimeSpan.Zero)
            {
                await Task.Delay(waitFor, ct);
            }
        }

        private async Task<IDictionary<CheckGroup, IDictionary<ICheck, CheckResult>>> RunChecks(IDictionary<CheckGroup, List<ICheck>> checkGroups, CancellationToken cancellationToken)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 1, // we can run checks in parallel if needed
                CancellationToken = cancellationToken,
            };

            var results = new ConcurrentDictionary<CheckGroup, IDictionary<ICheck, CheckResult>>();

            var checksToRun = checkGroups
                .OrderBy(kv => kv.Key.Order)
                .SelectMany(kv => kv.Value
                    .Where(c => c.ShouldRun())
                    .OrderBy(c => c.Configuration.Order)
                    .Select(c => new KeyValuePair<CheckGroup, ICheck>(kv.Key, c))
                );

            await Parallel.ForEachAsync(checksToRun, async (checkKV, cancellationToken) =>
            {
                var groupResultsDic = results.GetOrAdd(checkKV.Key, g => new ConcurrentDictionary<ICheck, CheckResult>());
                var check = checkKV.Value;
                try
                {
                    var runResult = await check.RunCheck(cancellationToken);
                    groupResultsDic.TryAdd(check, runResult);
                }
                catch (Exception exc)
                {
                    groupResultsDic.TryAdd(check, CheckResult.FromException(nameof(RunChecks), exc));
                }
            });

            return results;
        }

        private async Task SendReport(string clientId, TimeSpan checkDuration, IDictionary<CheckGroup, IDictionary<ICheck, CheckResult>> checkResults, CancellationToken cancellationToken)
        {
            var addedTags = new Dictionary<string, string>
            {
                { "clientId", clientId },
                { "checkDuration", checkDuration.ToString() },
            };

            var allCheckResultsToReport =
                checkResults
                .SelectMany(x => x.Value.Select(c => (ICheck: c.Key, Name: $"{x.Key.Name}.{c.Key.Configuration.Name}({c.Key.Configuration.Type})", CheckResult: c.Value)))
                .ToList();

            if (!allCheckResultsToReport.Any())
            {
                return;
            }

            foreach (var checkResult in allCheckResultsToReport)
            {
                addedTags.ForEach(kv => checkResult.CheckResult.Tags.TryAdd(kv.Key, kv.Value));

            }

            var tasksToWait = new List<Task>();

            foreach (var reportConfiguration in configuration.ReportConfigurations)
            {
                var reportGroups = reportConfiguration.Groups?.Any() == true
                        ? reportConfiguration.Groups
                        : new[] { "*" };

                var checksToReport = allCheckResultsToReport
                    .Where(x =>
                    {
                        if (x.ICheck.Configuration.ReportGroups?.Any() == true)
                        {
                            return reportGroups.Intersect(x.ICheck.Configuration.ReportGroups).Any();
                        }

                        return reportGroups.Contains("*");
                    })
                    .ToList();

                var reportListKVs = checksToReport.Select(x => new KeyValuePair<string, CheckResult>(x.Name, x.CheckResult));

                switch (reportConfiguration.Type)
                {
                    case Checker.Reports.ReportTypeEnum.Webhook:
                        if (reportConfiguration is WebhookReportConfiguration webhookReportConfiguration)
                        {
                            var webhookReport = new WebhookReport(webhookReportConfiguration, HttpClientProvider, clientId);
                            tasksToWait.Add(webhookReport.ReportResult(reportListKVs, cancellationToken));
                        }
                        break;
                    case Checker.Reports.ReportTypeEnum.AppInsight:
                        if (reportConfiguration is AppInsightReportConfiguration appInsightsReportConfiguration)
                        {
                            var appInsightReport = new AppInsightReport(appInsightsReportConfiguration, clientId);
                            tasksToWait.Add(appInsightReport.ReportResult(reportListKVs, cancellationToken));
                        }
                        break;
                    default:
                        break;
                }
            }

            await Task.WhenAll(tasksToWait);
        }

        private HttpClient HttpClientProvider()
            => HttpClientProvider(null);

        private HttpClient HttpClientProvider(Uri? configProxyUri)
        {
            if (configProxyUri == null)
            {
                configProxyUri = new Uri("noproxy://");
            }

            return proxiedHttpClientPool.GetOrAdd(configProxyUri, (proxyUri) =>
            {
                var socketHandler = new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2), // Recreate every 2 minutes
                };

                var proxyClient = ProxyClientProvider.GetProxyClient(proxyUri);
                if (proxyClient != null)
                {
                    socketHandler.Proxy = proxyClient;
                }

                return new HttpClient(socketHandler);
            });
        }

        private static ConcurrentDictionary<Uri, HttpClient> proxiedHttpClientPool = new ConcurrentDictionary<Uri, HttpClient>();

    }
}
