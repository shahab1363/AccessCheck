namespace CheckerApp.Configuration
{
    internal class AppConfig
    {
        public Guid? AppGuid { get; set; }
        public string ClientId { get; set; }
        public bool? IncludeUserInformationInGeneratedDeviceId { get; set; }
        public RunnerType? RunnerType { get; set; }
    }
}
