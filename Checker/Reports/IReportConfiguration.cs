using Checker.Common.JsonConverters;
using System.Text.Json.Serialization;

namespace Checker.Reports
{
    [JsonConverter(typeof(JsonConverterForIReportConfiguration))]
    public interface IReportConfiguration
    {
        public ReportTypeEnum Type { get; }
        public string Name { get; }
    }
}
