using Checker.Common.JsonConverters;
using Checker.Configuration;
using System.Text.Json.Serialization;

namespace Checker.Checks
{
    [JsonConverter(typeof(JsonConverterForICheckConfiguration))]
    public interface ICheckConfiguration : IHaveOrder, IHaveName
    {
        public CheckTypeEnum Type { get; }
        public TimeSpan? MinInterval { get; }
        public string[] ReportGroups { get; }
    }
}
