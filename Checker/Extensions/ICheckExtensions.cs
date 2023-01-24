using Checker.Checks.DnsCheck;
using Checker.Checks.HttpCheck;
using Checker.Checks.PingCheck;
using Checker.Checks.SocketCheck;
using Checker.Checks.TlsCheck;
using Checker.Checks;
using Checker.Configuration;
using CheckerLib.Checks.ExternalAppCheck;
using CheckerLib.Common.Factories;

namespace CheckerLib.Extensions
{
    public static class ICheckExtensions
    {
        public static IDictionary<CheckGroup, List<ICheck>> LoadCheckGroups(this CheckGroup[]? checkGroups)
        {
            var result = new Dictionary<CheckGroup, List<ICheck>>();
            if (checkGroups != null)
            {
                foreach (var checkGroup in checkGroups)
                {
                    var checks = checkGroup.GetChecks().ToList();
                    if (checks.Any())
                    {
                        result.Add(checkGroup, checks);
                    }
                }
            }

            return result;
        }

        public static IEnumerable<ICheck> GetChecks(this CheckGroup checkGroup)
        {
            foreach (var checkConfiguration in checkGroup.CheckConfigurations)
            {
                switch (checkConfiguration.Type)
                {
                    case CheckTypeEnum.RawSocket:
                        if (checkConfiguration is RawSocketCheckConfiguration rawSocketCheckConfiguration)
                        {
                            yield return new RawSocketCheck(rawSocketCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.Http:
                        if (checkConfiguration is HttpCheckConfiguration httpCheckConfiguration)
                        {
                            yield return new HttpCheck(httpCheckConfiguration, checkGroup.MinInterval, HttpClientFactory.HttpClientProvider);
                        }
                        break;
                    case CheckTypeEnum.TCP:
                        if (checkConfiguration is TCPCheckConfiguration tcpCheckConfiguration)
                        {
                            yield return new TCPCheck(tcpCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.UDP:
                        if (checkConfiguration is UDPCheckConfiguration udpCheckConfiguration)
                        {
                            yield return new UDPCheck(udpCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.DNS:
                        if (checkConfiguration is DnsCheckConfiguration dnsCheckConfiguration)
                        {
                            yield return new DnsCheck(dnsCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.TLS:
                        if (checkConfiguration is TLSCheckConfiguration tlsCheckConfiguration)
                        {
                            yield return new TLSCheck(tlsCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.Ping:
                        if (checkConfiguration is PingCheckConfiguration pingCheckConfiguration)
                        {
                            yield return new PingCheck(pingCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                    case CheckTypeEnum.ExternalApp:
                        if (checkConfiguration is ExternalAppCheckConfiguration externalAppCheckConfiguration)
                        {
                            yield return new ExternalAppCheck(externalAppCheckConfiguration, checkGroup.MinInterval);
                        }
                        break;
                }
            }
        }
    }
}
