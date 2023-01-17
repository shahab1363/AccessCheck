namespace CheckerApp.Configuration
{
    internal class AppConfig
    {
        public Guid AppGuid { get; set; }
        public string ClientId { get; set; }
    }

    internal class AppConfig<T> : AppConfig
    {
        public T RunnerConfiguration { get; set; }
    }
}
