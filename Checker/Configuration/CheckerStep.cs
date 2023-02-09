namespace Checker.Configuration
{
    public class CheckerStep
    {
        public string Name { get; set; }
        public CheckerStep RunBeforeStep { get; set; }
        public CheckGroup[] CheckGroups { get; set; }
        public CheckerStep RunAfterStep { get; set; }
        public TimeSpan? MinDuration { get; set; } = TimeSpan.Zero;
        public TimeSpan? MaxDuration { get; set; } = TimeSpan.FromDays(30);
        public bool FinishBeforeNextStep { get; set; } = false;
        public bool? SendReport { get; set; } = true;
    }
}
