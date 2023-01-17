using Checker.Checks;

namespace Checker.Configuration
{
    public class CheckGroup : IHaveOrder, IHaveName
    {
        public string Name { get; set; }
        public int Order { get; set; }
        public TimeSpan? MinInterval { get; set; }
        public ICheckConfiguration[] CheckConfigurations { get; set; }
    }
}
