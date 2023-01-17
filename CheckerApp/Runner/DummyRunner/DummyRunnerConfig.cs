namespace CheckerApp.Runner.DummyRunner
{
    internal class DummyRunnerConfig
    {
        public TimeSpan SleepBetweenTasks { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan MainTaskDelay { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan CleanUpTaskDelay { get; set; } = TimeSpan.FromSeconds(10);
    }
}
