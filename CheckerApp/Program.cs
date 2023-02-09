using Checker.Configuration;
using Checker.Extensions;
using CheckerApp.Configuration;
using CheckerApp.Runner;
using CheckerApp.Runner.CheckerRunner;
using CheckerApp.Runner.DummyRunner;
using CheckerLib.Common.Factories;
using CheckerLib.Common.Helpers;
using CheckerLib.Common.Logger;
using ClientId;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace CheckerApp
{
    internal class Program
    {
        static ManualResetEventSlim ExitEvent = new ManualResetEventSlim(false);
        static IRunner activeRunner;
        static CancellationTokenSource ctsRun, ctsCleanup;

        static async Task Main(string[] args)
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

                    Log.Info("End requested, will end after cleanup. Press Ctrl+C again to cancel cleanup.");
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

            var appConfigFromJson = JsonSerializer.Deserialize<AppConfig>(jsonSettings);

            var config = new ConfigurationBuilder()
                //.SetBasePath(basePath)
                //.AddJsonFile(appsettingsFile, optional: false, reloadOnChange: false)
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
                appConfigFromJson?.AppGuid == null || appConfigFromJson.AppGuid == Guid.Empty // if not found, will use value from json config
                    ? new Guid("53596D38-FD48-4024-AB5A-2E378691F1AF") // or use default value if still not found
                    : appConfigFromJson.AppGuid);

            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    Log.Fatal("Another instance of the application is already running");
                    return;
                }

                DumpVersionInformation();
                DumpNetworkInformation();

                var clientId = config.GetValue("ClientId", string.Empty);
                if (string.IsNullOrEmpty(clientId))
                {
                    var includeUserInformationInGeneratedDeviceId = config.GetValue("IncludeUserInformationInGeneratedDeviceId", appConfigFromJson?.IncludeUserInformationInGeneratedDeviceId ?? true);
                    clientId = ClientIdProvider.GetUniqueId(includeUserInformationInGeneratedDeviceId);
                }

                var runnerType = config.GetValue("RunnerType", appConfigFromJson?.RunnerType ?? RunnerType.Checker);


                switch (runnerType)
                {
                    case RunnerType.Checker:
                        var checkerAppConfig = await GetConfiguration<CheckerConfiguration>(jsonSettings);
                        await StartProgram(new CheckerRunner(), checkerAppConfig, clientId);
                        break;
                    case RunnerType.Dummy:
                        var dummyRunnerConfig = await GetConfiguration<DummyRunnerConfig>(jsonSettings) ?? new RunnerConfig<DummyRunnerConfig>();
                        await StartProgram(new DummyRunner(), dummyRunnerConfig, clientId);
                        break;
                    default:
                        break;
                }
            }
        }

        static async Task StartProgram<T>(IRunner<T> runner, RunnerConfig<T>? runnerConfig, string clientId)
        {
            activeRunner = runner;

            runner.IsRunningChange += (sender, isRunning) =>
            {
                if (isRunning)
                {
                    // start running
                    Log.Info("App Started");
                }
                else
                {
                    // end running
                    Log.Info("App Stopped");
                }
            };

            runner.IsBusyChange += (sender, isBusy) =>
            {
                if (isBusy)
                {
                    // start busy
                    Log.Info("Checker Started");
                }
                else
                {
                    // end busy
                    Log.Info("Checker Finished");
                }
            };

            ctsRun = new CancellationTokenSource();
            ctsCleanup = new CancellationTokenSource();

            if (runnerConfig == null || runnerConfig.RunnerConfiguration == null)
            {
                throw new Exception("Invalid configuration file provided");
            }

            Log.Info($"Client Id: {clientId}");

            Log.Info("Press Ctrl+C to end the app");

            await runner.Initialize(runnerConfig.RunnerConfiguration, clientId);

            // todo: add tasks to run before and after runner - initialize, cleanup

            try
            {
                // do the job
                await runner.Start(ctsRun.Token);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Run was interrupted [Cancelled]");
            }

            //Program.ExitEvent.Wait();

            Log.Info("Starting clean up");
            try
            {
                await runner.Cleanup(ctsCleanup.Token);
            }
            catch (TaskCanceledException)
            {
                Log.Warn("Clean up was interrupted [Cancelled]");
            }

            Log.Info("Application terminated");
            PressAnyKey();
        }

        private static async Task<RunnerConfig<T>?> GetConfiguration<T>(string jsonSettings)
        {
            Log.Info($"Loading configuration");
            var serializationOptions = SerializationExtensions.GetDefaultSerializationOptions();
            var config = JsonSerializer.Deserialize<RunnerConfig<T>>(jsonSettings, serializationOptions);
            if (config?.RemoteRunnerConfiguration != null)
            {
                var remoteConfig = await GetRemoteConfig<T>(config.RemoteRunnerConfiguration);
                if (remoteConfig != null)
                {
                    config.RunnerConfiguration = remoteConfig;
                }
            }

            return config;
        }

        private static async Task<T?> GetRemoteConfig<T>(Uri remoteConfigUri)
        {
            Log.Info($"Fetching remote config from: {remoteConfigUri}");
            var httpClient = HttpClientFactory.HttpClientProvider();
            if (httpClient != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, remoteConfigUri);
                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var serializationOptions = SerializationExtensions.GetDefaultSerializationOptions();
                    var deserializedResponse = JsonSerializer.Deserialize<T>(responseContent, serializationOptions);
                    if (deserializedResponse != null)
                    {
                        return deserializedResponse;
                    }
                }
            }

            return default;
        }

        private static void DumpVersionInformation()
        {
            SystemInformation.GetVersionInformation().ForEach(line => Log.Info(line));
        }

        private static void DumpNetworkInformation()
        {
            Log.Info("Network Information:");
            SystemInformation.GetNetworkInformation().ForEach(line => Log.Info(line));
        }

        private static void PressAnyKey()
        {
            if (Environment.UserInteractive)
            {
                Log.Info("Press any key to continue");
                Console.ReadKey(true);
            }
        }
    }
}