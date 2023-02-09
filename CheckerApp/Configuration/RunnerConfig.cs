namespace CheckerApp.Configuration
{
    internal class RunnerConfig<T>
    {
        public Uri RemoteRunnerConfiguration { get; set; }
        public T RunnerConfiguration { get; set; }
    }
}
