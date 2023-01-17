using System.Text.Json.Serialization;

namespace Checker.Checks
{
    public enum CheckTypeEnum
    {
        RawSocket,
        Http,
        TCP,
        UDP,
        DNS,
        TLS,
        Ping,
        ExternalApp,
    }
}
