using Checker.Checks;
using Checker.Common.Exceptions;
using Checker.Extensions;
using System.Diagnostics;

namespace CheckerLib.Checks.ExternalAppCheck
{
    public class ExternalAppCheck : ICheck
    {
        public CheckTypeEnum Type => CheckTypeEnum.ExternalApp;
        public string Name => configuration.Name;
        public int Order => configuration.Order;
        public TimeSpan? MinInterval => this.minInterval;
        public DateTimeOffset LastRun { get; private set; } = DateTimeOffset.MinValue;

        private readonly ExternalAppCheckConfiguration configuration;
        private readonly TimeSpan minInterval;

        public ExternalAppCheck(ExternalAppCheckConfiguration externalAppCheckConfiguration, TimeSpan? overrideMinInterval)
        {
            configuration = externalAppCheckConfiguration;
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

            if (string.IsNullOrWhiteSpace(configuration.Command))
            {
                throw new ArgumentNullException(nameof(configuration.Command));
            }

            //if (!File.Exists(configuration.Command))
            //{
            //    throw new FileNotFoundException(nameof(configuration.Command));
            //}

            try
            {
                return await MethodExtensions.RunWithRetries(
                    ct => InternalAppRunCheck(ct),
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


        private async Task<CheckResult> InternalAppRunCheck(CancellationToken ct)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var (exitCode, stdOut, stdErr) = await RunProcess(
                configuration.Command,
                configuration.Args,
                configuration.EnvironmentVariables,
                configuration.WaitForExit,
                configuration.CaptureStdOut,
                configuration.CaptureStdError,
                configuration.KillIfRunningAfterWait,
                configuration.MinWait,
                ct);

            var commandDuration = stopWatch.Elapsed;

            var command = string.Join(" ", new[] { configuration.Command }.Union(configuration.Args ?? Enumerable.Empty<string>()));
            var tags = new Dictionary<string, string>
            {
                { "Command", command },
                { "CommandDuration", commandDuration.ToString() },
                { "CommandDuration." + command, commandDuration.ToString() },
                { "ExitCode", exitCode.ToString() ?? "RUNNING" },
                { "ExitCode." + command, exitCode.ToString() ?? "RUNNING" },
            };

            var cleanUpDuration = TimeSpan.Zero;

            if (!string.IsNullOrEmpty(configuration.CleanUpCommand))
            {
                stopWatch.Restart();
                var (cleanUpExitCode, _, _) = await RunProcess(
                    configuration.CleanUpCommand,
                    configuration.CleanUpArgs?.Any() == true
                        ? configuration.CleanUpArgs.Union(new[] { $"exitCode={exitCode}" }).ToArray()
                        : new[] { $"exitCode={exitCode}" },
                    configuration.CleanUpEnvironmentVariables,
                    configuration.CleanUpWaitForExit,
                    configuration.CaptureStdOut,
                    configuration.CaptureStdError,
                    configuration.CleanUpKillIfRunningAfterWait,
                    configuration.CleanUpMinWait,
                    ct);

                cleanUpDuration = stopWatch.Elapsed;

                var cleanUpCommand = string.Join(" ", new[] { configuration.CleanUpCommand }.Union(configuration.CleanUpArgs ?? Enumerable.Empty<string>()));
                tags.Add("CleanUpCommand", cleanUpCommand);
                tags.Add("CleanUpDuration", cleanUpDuration.ToString());
                tags.Add("CleanUpDuration." + cleanUpCommand, cleanUpDuration.ToString());
                tags.Add("CleanUpExitCode", exitCode.ToString() ?? "RUNNING");
                tags.Add("CleanUpExitCode." + cleanUpCommand, exitCode.ToString() ?? "RUNNING");
            }
            stopWatch.Stop();

            tags.Add("TotalDuration", (commandDuration + cleanUpDuration).ToString());
            tags.Add("TotalDuration." + configuration.Command, (commandDuration + cleanUpDuration).ToString());

            tags = tags.ToDictionary(kv => this.GetType().Name + "." + kv.Key, kv => kv.Value);

            CheckResult checkResult;

            if (configuration.ExternalAppValidations?.Any() == true)
            {
                var results = new Dictionary<string, CheckResult>();
                foreach (var validation in configuration.ExternalAppValidations)
                {
                    var validationResult = await validation.Validate(exitCode, stdOut, stdErr);
                    tags.ForEach(t => validationResult.Tags.TryAdd(t.Key, t.Value));
                    results.Add(validation.Name, validationResult);
                }

                checkResult = CheckResult.CreateBasedOnThreshold(
                    configuration.ExternalAppValidations.Length,
                    configuration.SuccessThresholdPercent,
                    configuration.Name,
                    results);
            }
            else
            {
                checkResult = new CheckResult(
                    exitCode == 0 ? CheckResultEnum.Success : CheckResultEnum.Failure,
                    $"Exit code: {exitCode}",
                    tags);
            }

            if (checkResult.Result == CheckResultEnum.Failure)
            {
                throw new CheckResultException(checkResult);
            }

            return checkResult;
        }

        private async Task<(int? exitCode, string? stdOut, string? stdErr)> RunProcess(
            string command,
            string[] args,
            Dictionary<string, string> environmentVariables,
            bool waitForExit,
            bool captureStdOut,
            bool captureStdErr,
            bool killIfRunningAfterWait,
            TimeSpan minWait,
            CancellationToken ct)
        {
            var stdOut = string.Empty;
            var stdErr = string.Empty;

            var basePath =
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location) ??
                Environment.ProcessPath ??
                string.Empty;

            using var p = new Process()
            {
                StartInfo = new ProcessStartInfo(command)
                {
                    WorkingDirectory = basePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = captureStdErr,
                    RedirectStandardOutput = captureStdOut,
                },
            };

            p.OutputDataReceived += new DataReceivedEventHandler((sender, e) => { stdOut += e.Data; });
            p.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => { stdErr += e.Data; });

            args?.ForEach(a => p.StartInfo.ArgumentList.Add(a));
            environmentVariables?.ForEach(kv => p.StartInfo.Environment.TryAdd(kv.Key, kv.Value));

            var stopWatch = Stopwatch.StartNew();

            p.Start();

            // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.  
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            if (waitForExit)
            {
                await p.WaitForExitAsync(ct);
            }

            var elapsed = stopWatch.Elapsed;
            if (elapsed < minWait)
            {
                await Task.Delay(minWait - elapsed, ct);
            }

            if (killIfRunningAfterWait && !p.HasExited)
            {
                p.Kill();
            }

            var exitCode = p.HasExited ? p.ExitCode : (int?)null;

            return (exitCode, captureStdOut ? stdOut : null, captureStdErr ? stdErr : null);
        }
    }
}
