using Checker.Checks.DnsCheck;
using Checker.Checks.HttpCheck;
using Checker.Checks.PingCheck;
using Checker.Checks.TlsCheck;
using Checker.Common.Helpers;
using Checker.Configuration;
using Checker.Extensions;
using Checker.Reports.AppInsightReport;
using Checker.Reports.WebhookReport;
using Checker.Validations;
using CheckerLib.Checks.ExternalAppCheck;
using System.Text.Json;

var configuration = new CheckerConfiguration
{
    RunBeforePeriodicChecksStep = new CheckerStep
    {
        CheckGroups = new[] {
            new CheckGroup
            {
                Name = "StartVPN",
                CheckConfigurations = new Checker.Checks.ICheckConfiguration[]
                {
                    new ExternalAppCheckConfiguration
                    {
                        Name = "LaunchVPN",
                        Command = "..\\..\\..\\sscli\\Debug\\net6.0\\sscli.exe",
                        Args = "c --listen-socks 127.0.0.1:8888".Split(" "),
                        WaitForExit = false,
                        KillIfRunningAfterWait = false,
                        CaptureStdOut = true,
                        CaptureStdError = true,
                        ExternalAppValidations = new[]
                        {
                            new ExpectExitCode
                            {
                                ExpectedExitCodes = new int?[] {
                                    null,
                                    999
                                },
                            }
                        },
                        MinWait = TimeSpan.FromSeconds(5),
                    }
                }
            }
        },
        MinDuration = TimeSpan.FromSeconds(10),
        FinishBeforeNextStep = true,
        SendReport = true,
    },
    PeriodicChecksStep = new CheckerStep
    {
        CheckGroups = new[]
        {
            new CheckGroup
            {
                Name = "Facebook",
                Order = 1,
                MinInterval = TimeSpan.FromSeconds(30),
                CheckConfigurations = new Checker.Checks.ICheckConfiguration[]
                {
                    new DnsCheckConfiguration
                    {
                        Name = "DNSCheck",
                        Order = 1001,
                        HostNameOrAddress = "facebook.com",
                        IPValidations = new[]
                        {
                            new MustNotContain
                            {
                                StringToCheck = "10.10.34.34"
                            }
                        }
                    },
                    new HttpCheckConfiguration
                    {
                        Name = "HttpCheck",
                        Order = 1002,
                        HttpMethod = Checker.Checks.HttpCheck.HttpMethodEnum.Get,
                        Uris = new[] {
                            new Uri("https://facebook.com"),
                            new Uri("https://fb.com")
                        },
                        HttpValidations = new IHttpValidation[]
                        {
                            new MustContain
                            {
                                Name = "MustContainsFBCDN.NET",
                                StringToCheck = "fbcdn.net/"
                            },
                            new ExpectStatusCodes
                            {
                                ExpectedStatusCodes = new[] { 200 }
                            },
                            new ExpectContentLength
                            {
                                ExpectedContentLength = 1000,
                                ThresholdPercent= 100000
                            }
                        },
                        ProxyUri = new Uri("socks5://127.0.0.1:8888"),
                        PerUriTimeOut = TimeSpan.FromMinutes(2),
                        TimeOut = TimeSpan.FromMinutes(10),
                    },
                    new TLSCheckConfiguration
                    {
                        Name = "TLSCheck",
                        Order = 1003,
                        HostName = "facebook.com",
                        SslProtocol = System.Security.Authentication.SslProtocols.None,
                        EncryptionPolicy = System.Net.Security.EncryptionPolicy.RequireEncryption
                    },
                    new PingCheckConfiguration
                    {
                        Name = "PingCheck",
                        Order = 1004,
                        HostNames = new[] { "facebook.com", "fb.com" },
                    },
                }
            },
        },
        FinishBeforeNextStep = true,
        SendReport = true,
    },
    RunAfterPeriodicChecksStep = new CheckerStep
    {
        CheckGroups = new[] {
            new CheckGroup
            {
                Name = "StopVPN",
                CheckConfigurations = new Checker.Checks.ICheckConfiguration[]
                {
                    new ExternalAppCheckConfiguration
                    {
                        Name="KillVPN",
                        Command = "taskkill",
                        Args= "/im sscli.exe /F".Split(" "),
                        WaitForExit = true,
                        MinWait = TimeSpan.Zero,
                        KillIfRunningAfterWait = true,
                        CaptureStdOut = true,
                        CaptureStdError = true,
                        ExternalAppValidations = new IExternalAppValidation[]
                        {
                            new ExpectExitCode
                            {
                                Name = "ExpectExitCode0",
                                ExpectedExitCodes = new int?[]{ 0 },
                            },
                            new MustContain
                            {
                                Name = "MustContainSuccess",
                                CaseSensitive = false,
                                StringToCheck = "SUCCESS: The process"
                            },
                            new MustContain
                            {
                                Name = "MustContainHasBeenTerminated",
                                CaseSensitive = false,
                                StringToCheck = "has been terminated."
                            }
                        }
                    }
                }
            }
        },
        MaxDuration = TimeSpan.FromSeconds(30),
        SendReport = true,
    },
    ReportConfigurations = new Checker.Reports.IReportConfiguration[]
    {
        new WebhookReportConfiguration
        {
            Name = "TestWebhook",
            Uris = new[]
            {
                new Uri("https://webhook")
            }
        },
        new AppInsightReportConfiguration
        {
            Name = "TestAppInsights",
            ConnectionString = "Connection string from azure app insight",
            Tags = new Dictionary<string, string>
            {
                { "Env", "Test" },
            }
        }
    },
    //ReportSelection = ReportSelectionEnum.All,
    //ReportSelectionPercent = 100,
    Interval = TimeSpan.FromMinutes(10),
    ScheduledRunTime = "*[*]"
};

var serializationOptions = SerializationExtensions.GetDefaultSerializationOptions(true);

var configJson = JsonSerializer.Serialize(configuration, serializationOptions);

var reconstructedConfig = JsonSerializer.Deserialize<CheckerConfiguration>(configJson, serializationOptions);
var reSerializedJson = JsonSerializer.Serialize(reconstructedConfig, serializationOptions);

// compare config json and json from reconstructed config object to make sure the serialization/deserialization was successful
var comparer = new JsonElementComparer();
using var doc1 = JsonDocument.Parse(configJson);
using var doc2 = JsonDocument.Parse(reSerializedJson);

if (!comparer.Equals(doc1.RootElement, doc2.RootElement))
{
    throw new Exception("Serialization/Deserialization failed");
}

Console.WriteLine(configJson);
Console.ReadLine();
