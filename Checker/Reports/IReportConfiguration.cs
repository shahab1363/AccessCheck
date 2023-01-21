using Checker.Common.JsonConverters;
using Checker.Configuration;
using System.Text.Json.Serialization;

namespace Checker.Reports
{
    [JsonConverter(typeof(JsonConverterForIReportConfiguration))]
    public interface IReportConfiguration : IHaveName
    {
        public ReportTypeEnum Type { get; }
        public string[] Groups { get; }
    }
}
