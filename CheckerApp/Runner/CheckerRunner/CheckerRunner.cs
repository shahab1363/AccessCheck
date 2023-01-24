using Checker.Checks;
using Checker.Configuration;
using Checker.Extensions;
using Checker.Reports.AppInsightReport;
using Checker.Reports.WebhookReport;
using CheckerLib.Common.Factories;
using CheckerLib.Common.Helpers;
using CheckerLib.Extensions;
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
        private Task appStartedTask = Task.CompletedTask;
        private Task appShutdownTask = Task.CompletedTask;

        private Dictionary<CheckerStep, StepCache> checksCache;

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

            checksCache = configuration.PeriodicChecksSteps.ToDictionary(
                    s => s,
                    s => new StepCache(s));
            //s => new StepCache(s)
            //    periodicCheks: LoadChecks(s.CheckGroups),
            //    runningChecksTask: Task.CompletedTask,
            //    runBeforePeriodicChecks: LoadChecks(s.RunBeforeStep?.CheckGroups),
            //    runBeforeStepTask: Task.CompletedTask,
            //    runAfterPeriodicCheck: LoadChecks(s.RunAfterStep?.CheckGroups),
            //    runAfterStepTask: Task.CompletedTask));

            IsInitialized = true;
            return Task.CompletedTask;
        }

        private async Task AwaitAndGetNewTask(Func<Task> taskFunc, params Task[] tasksToWaitBeforeCreatingNewOne)
        {
            if (tasksToWaitBeforeCreatingNewOne?.Any() == true)
            {
                await Task.WhenAll(tasksToWaitBeforeCreatingNewOne).ConfigureAwait(false);
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
                        return RunTask(
                            configuration.AppStartStep.CheckGroups.LoadCheckGroups(),
                            configuration.AppStartStep?.MinDuration,
                            configuration.AppStartStep?.MaxDuration,
                            configuration.AppStartStep?.SendReport,
                            cleanUpCTS.Token);
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
                        return RunTask(
                            configuration.AppShutdownStep.CheckGroups.LoadCheckGroups(),
                            configuration.AppShutdownStep?.MinDuration,
                            configuration.AppShutdownStep?.MaxDuration,
                            configuration.AppShutdownStep?.SendReport,
                            cleanUpCTS.Token);
                    }
                    return Task.CompletedTask;
                }, appShutdownTask, appStartedTask);
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

                runCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cleanUpCTS.Token);
                var ct = runCTS.Token;
                var lastStartTime = DateTime.MinValue;

                if (configuration.PeriodicChecksSteps?.Any() != true)
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
                        foreach (var step in checksCache.Keys)
                        {
                            await RunStep(
                                step,
                                checksCache[step],
                                step.MinDuration,
                                step.MaxDuration,
                                step.SendReport,
                                ct).ConfigureAwait(false);

                            if (ct.IsCancellationRequested)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        this.IsBusy = false;
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

        private async Task RunStep(
            CheckerStep step,
            StepCache stepCache,
            TimeSpan? fallBackMinDuration,
            TimeSpan? fallBackMaxDuration,
            bool? fallBackSendReport,
            CancellationToken ct,
            params Task[] additionalTaskToWaitBeforeStart)
        {
            if (step == null || stepCache == null)
            {
                return;
            }

            var minDuration = step.MinDuration ?? fallBackMinDuration;
            var maxDuration = step.MaxDuration ?? fallBackMaxDuration;
            var sendReport = step.SendReport ?? fallBackSendReport;

            try
            {
                if (step.RunBeforeStep != null && stepCache.BeforeStepCache != null)
                {
                    await RunStep(
                        step.RunBeforeStep,
                        stepCache.BeforeStepCache,
                        minDuration,
                        maxDuration,
                        sendReport,
                        ct,
                        stepCache.AfterStepCache?.RunningTask ?? Task.CompletedTask);
                }

                var tasksToWaitFor = additionalTaskToWaitBeforeStart?.Any() == true
                    ? additionalTaskToWaitBeforeStart.Union(new[] { stepCache.RunningTask }).ToArray()
                    : new[] { stepCache.RunningTask };

                if (stepCache.PeriodicChecks?.Any() == true)
                {
                    stepCache.RunningTask = AwaitAndGetNewTask(
                        () => RunTask(
                            stepCache.PeriodicChecks,
                            minDuration,
                            maxDuration,
                            sendReport,
                            ct),
                        tasksToWaitFor);
                }

                if (step?.FinishBeforeNextStep == true)
                {
                    await stepCache.RunningTask.ConfigureAwait(false);
                }
            }
            finally
            {
                if (step.RunAfterStep != null && stepCache.AfterStepCache != null)
                {
                    await RunStep(
                        step.RunAfterStep,
                        stepCache.AfterStepCache,
                        minDuration,
                        maxDuration,
                        sendReport,
                        ct,
                        stepCache.BeforeStepCache?.RunningTask ?? Task.CompletedTask);
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

            await Task.WhenAll(checksCache.Values.Select(c => c.CleanUpTask));
            await Task.Delay(TimeSpan.FromSeconds(1));

            await appShutdownTask.ConfigureAwait(false);
        }

        private int GetIntervalDelay(DateTime lastStartTime)
            => Math.Max(0, (int)(configuration.Interval - (DateTime.Now - lastStartTime)).TotalMilliseconds);

        private async Task RunTask(IDictionary<CheckGroup, List<ICheck>> checksToRun, TimeSpan? minDurationN, TimeSpan? maxDurationN, bool? sendReportN, CancellationToken cancellationToken)
        {
            var minDuration = minDurationN ?? TimeSpan.Zero;
            var maxDuration = maxDurationN ?? TimeSpan.FromDays(30);
            var sendReport = sendReportN ?? true;

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
                            var webhookReport = new WebhookReport(webhookReportConfiguration, HttpClientFactory.HttpClientProvider, clientId);
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
    }
}
