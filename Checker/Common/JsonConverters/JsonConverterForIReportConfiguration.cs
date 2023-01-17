using Checker.Reports;
using Checker.Reports.WebhookReport;
using Checker.Reports.AppInsightReport;

namespace Checker.Common.JsonConverters
{
    public class JsonConverterForIReportConfiguration : JsonConverterWithTypeDiscriminator<IReportConfiguration>
    {
        public override string TypeDescriminatorProperty => "ReportConfigurationType";
        public override string TypeValueProperty => "ReportConfiguration";
        public override string GetTypeDescriminatorValue(IReportConfiguration toBeSerialized)
        {
            return toBeSerialized.Type.ToString();
        }

        public override Type GetTypeFromDescriminator(string? descriminatorValue)
        {
            if (Enum.TryParse<ReportTypeEnum>(descriminatorValue, out var reportTypeEnum))
            {
                switch (reportTypeEnum)
                {
                    case ReportTypeEnum.Webhook:
                        return typeof(WebhookReportConfiguration);
                    case ReportTypeEnum.AppInsight:
                        return typeof(AppInsightReportConfiguration);
                }
            }

            throw new NotSupportedException();
        }
    }
}
