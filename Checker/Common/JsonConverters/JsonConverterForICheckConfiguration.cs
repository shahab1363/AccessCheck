using Checker.Checks.SocketCheck;
using Checker.Checks.HttpCheck;
using Checker.Checks.DnsCheck;
using Checker.Checks.TlsCheck;
using Checker.Checks.PingCheck;
using Checker.Checks;
using CheckerLib.Checks.ExternalAppCheck;

namespace Checker.Common.JsonConverters
{
    public class JsonConverterForICheckConfiguration : JsonConverterWithTypeDiscriminator<ICheckConfiguration>
    {
        public override string TypeDescriminatorProperty => "CheckConfigurationType";
        public override string TypeValueProperty => "CheckConfiguration";
        public override string GetTypeDescriminatorValue(ICheckConfiguration toBeSerialized)
        {
            return toBeSerialized.Type.ToString();
        }

        public override Type GetTypeFromDescriminator(string? descriminatorValue)
        {
            if (Enum.TryParse<CheckTypeEnum>(descriminatorValue, out var checkTypeEnum))
            {
                switch (checkTypeEnum)
                {
                    case CheckTypeEnum.RawSocket:
                        return typeof(RawSocketCheckConfiguration);
                    case CheckTypeEnum.Http:
                        return typeof(HttpCheckConfiguration);
                    case CheckTypeEnum.TCP:
                        return typeof(TCPCheckConfiguration);
                    case CheckTypeEnum.UDP:
                        return typeof(UDPCheckConfiguration);
                    case CheckTypeEnum.DNS:
                        return typeof(DnsCheckConfiguration);
                    case CheckTypeEnum.TLS:
                        return typeof(TLSCheckConfiguration);
                    case CheckTypeEnum.Ping:
                        return typeof(PingCheckConfiguration);
                    case CheckTypeEnum.ExternalApp:
                        return typeof(ExternalAppCheckConfiguration);
                }
            }

            throw new NotSupportedException();
        }
    }
}
