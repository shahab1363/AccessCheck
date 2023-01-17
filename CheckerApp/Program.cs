using Checker.Configuration;
using Checker.Extensions;
using CheckerApp.Configuration;
using CheckerApp.Runner;
using CheckerApp.Runner.CheckerRunner;
using CheckerApp.Runner.DummyRunner;
using CheckerLib.Common.Helpers;
using ClientId;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckerApp
{
    internal class Program
    {
        static ManualResetEventSlim ExitEvent = new ManualResetEventSlim(false);
        static IRunner activeRunner;
        static CancellationTokenSource ctsRun, ctsCleanup;

        private const bool IncludeUserInformationInGeneratedDeviceId = true;
        static Task Main(string[] args)
        {
            Console.CancelKeyPress += async (sender, e) =>
            {
                if (!Program.ExitEvent.IsSet)
                {
                    Program.ExitEvent.Set();
                    if (activeRunner?.IsRunning == true)
                    {
                        await activeRunner.Stop();
                    }

                    ctsRun?.Cancel();

                    Log("End requested, will end after cleanup. Press Ctrl+C again to cancel cleanup.", true);
                    e.Cancel = true;
                }
                else
                {
                    ctsCleanup?.Cancel();
                }
            };

            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location) ?? Environment.ProcessPath ?? string.Empty;
            var appsettingsFile = "appsettings.json";
            var jsonSettings = File.ReadAllText(Path.Combine(basePath, appsettingsFile));

            var generalAppConfig = JsonSerializer.Deserialize<AppConfig>(jsonSettings);

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(appsettingsFile, optional: false, reloadOnChange: false)
                .AddEnvironmentVariables("CHECKER_")
                .AddCommandLine(args)
                //.AddCommandLine(args, new Dictionary<string, string>
                //{
                //    { "schedule", nameof(AppConfig<T>.RunnerConfiguration) + ":" + nameof(CheckerConfiguration.ScheduledRunTime) }
                //})
                .Build();

            //var appGuid = config.GetValue("AppGuid", new Guid("53596D38-FD48-4024-AB5A-2E378691F1AF"));

            var appGuid = config.GetValue( // first try to get config from environment variables or command line
                "AppGuid",
                generalAppConfig == null || generalAppConfig.AppGuid == Guid.Empty // if not found, will use value from json config
                    ? new Guid("53596D38-FD48-4024-AB5A-2E378691F1AF") // or use default value if still not found
                    : generalAppConfig.AppGuid);

            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    Log("Another instance of the application is already running");
                    return Task.CompletedTask;
                }

                DumpVersionInformation();
                DumpNetworkInformation();

                var con = config.Get<AppConfig<CheckerConfiguration>>();
                var runnerType = config.GetValue("runner", "checker") ?? "checker";

                var serializationOptions = SerializationExtensions.GetDefaultSerializationOptions();

                if (runnerType.Equals("dummy", StringComparison.OrdinalIgnoreCase))
                {
                    return StartProgram(new DummyRunner(), new AppConfig<DummyRunnerConfig>());
                }

                var checkerAppConfig = JsonSerializer.Deserialize<AppConfig<CheckerConfiguration>>(jsonSettings, serializationOptions);

                return StartProgram(new CheckerRunner(), checkerAppConfig);
            }
        }

        static async Task StartProgram<T>(IRunner<T> runner, AppConfig<T>? appConfig)
        {
            activeRunner = runner;

            runner.IsRunningChange += (sender, isRunning) =>
            {
                if (isRunning)
                {
                    // run start running
                }
                else
                {
                    // run end running
                }
            };

            runner.IsBusyChange += (sender, isBusy) =>
            {
                if (isBusy)
                {
                    // run start busy
                }
                else
                {
                    // run end busy
                }
            };

            ctsRun = new CancellationTokenSource();
            ctsCleanup = new CancellationTokenSource();

            if (appConfig == null || appConfig.RunnerConfiguration == null)
            {
                throw new Exception("Invalid configuration file provided");
            }

            if (string.IsNullOrEmpty(appConfig.ClientId))
            {
                appConfig.ClientId = ClientIdProvider.GetUniqueId(IncludeUserInformationInGeneratedDeviceId);
            }

            Log("Client Id: {0}", appConfig.ClientId);

            Log("Press Ctrl+C to end the app", true);

            await runner.Initialize(appConfig.RunnerConfiguration, appConfig.ClientId);

            // todo: add tasks to run before and after runner - initialize, cleanup

            try
            {
                // do the job
                await runner.Start(ctsRun.Token);
            }
            catch (TaskCanceledException)
            {
                Log("Run was interrupted");
            }

            //Program.ExitEvent.Wait();

            Log("Starting clean up");
            try
            {
                await runner.Cleanup(ctsCleanup.Token);
            }
            catch (TaskCanceledException)
            {
                Log("Clean up was interrupted");
            }

            Log("Application terminated");
            PressAnyKey();
        }

        private static void Log()
        {
            // todo: replace these methods with appropriate log library methods
            Console.WriteLine();
        }

        private static void Log(string message, params string[]? formatArgs)
        {
            // todo: replace these methods with appropriate log library methods
            if (formatArgs?.Any() == true)
            {
                Console.WriteLine(message, formatArgs);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
        private static void Log(string message, bool onlyInteractive, params string[]? formatArgs)
        {
            // todo: replace these methods with appropriate log library methods
            if (!onlyInteractive || Environment.UserInteractive)
            {
                if (formatArgs != null)
                {
                    Console.WriteLine(message, formatArgs);
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
        }

        private static void DumpVersionInformation()
        {
            SystemInformation.GetVersionInformation().ForEach(line => Log(line));
            Log();
        }

        private static void DumpNetworkInformation()
        {
            Log("Network Information:");
            SystemInformation.GetNetworkInformation().ForEach(line => Log(line));
            Log();
        }

        private static void PressAnyKey()
        {
            if (Environment.UserInteractive)
            {
                Log("Press any key to continue", true);
                Console.ReadKey(true);
            }
        }
    }
}