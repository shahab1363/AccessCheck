using Checker.Checks;
using Checker.Validations;

namespace CheckerLib.Checks.ExternalAppCheck
{
    public class ExternalAppCheckConfiguration : ICheckConfiguration
    {
        public CheckTypeEnum Type => CheckTypeEnum.ExternalApp;
        public int Order { get; set; }
        public string Name { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public string Command { get; set; }
        public string[] Args { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
        public bool WaitForExit { get; set; } = false;
        public TimeSpan WaitForExitTimeOut { get; set; } = TimeSpan.FromSeconds(300);
        public TimeSpan MinWait { get; set; } = TimeSpan.Zero;
        public bool KillIfRunningAfterWait { get; set; }
        public string CleanUpCommand { get; set; }
        public string[] CleanUpArgs { get; set; }
        public Dictionary<string, string> CleanUpEnvironmentVariables { get; set; }
        public bool CleanUpWaitForExit { get; set; } = true;
        public TimeSpan CleanUpWaitForExitTimeOut { get; set; } = TimeSpan.FromSeconds(300);
        public TimeSpan CleanUpMinWait { get; set; } = TimeSpan.FromSeconds(30);
        public bool CleanUpKillIfRunningAfterWait { get; set; }
        public bool CaptureStdOut { get; set; } = true;
        public bool CaptureStdError { get; set; } = true;
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan TimeOut { get; set; } = TimeSpan.FromHours(1);
        public IExternalAppValidation[] ExternalAppValidations { get; set; }
        public int SuccessThresholdPercent { get; set; } = 99;
    }
}

