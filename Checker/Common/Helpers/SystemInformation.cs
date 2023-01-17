using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace CheckerLib.Common.Helpers
{
    public static class SystemInformation
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out _, 0);
        }

        public static IEnumerable<string> GetNetworkInformation()
        {
            yield return $"Machine {(IsConnectedToInternet() ? "is" : "IS NOT")} connected to internet";
            yield return "Network Interfaces Status:";
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                yield return $"\t{networkInterface.Name} ({networkInterface.Description}) is {networkInterface.OperationalStatus} [Type: {networkInterface.NetworkInterfaceType} - Physical Address: {networkInterface.GetPhysicalAddress()}]";

                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                var ipInterfaceProperties = networkInterface.GetIPProperties();

                yield return $"\t\tDNS Suffix:\t{ipInterfaceProperties.DnsSuffix}";
                yield return $"\t\tDNS Servers:\t{string.Join(",", ipInterfaceProperties.DnsAddresses.Select(x => x.ToString()))}";
                yield return $"\t\tGateway:\t{string.Join(",", ipInterfaceProperties.GatewayAddresses.Select(x => x.Address.ToString()))}";

                var ipv4Addresses = ipInterfaceProperties.UnicastAddresses?.Where(x => x.IPv4Mask != IPAddress.Any);
                if (ipv4Addresses?.Any() == true)
                {
                    yield return $"\t\tIPv4 Addresses:";
                    foreach (var ipv4 in ipv4Addresses)
                    {
                        yield return $"\t\t\t{ipv4.PrefixOrigin}:\t{ipv4.Address} - Subnet Mask: {ipv4.IPv4Mask}\t({ipv4.SuffixOrigin})";
                    }
                }

                var otherAddresses = ipInterfaceProperties.UnicastAddresses?.Where(x => x.IPv4Mask == IPAddress.Any);
                if (otherAddresses?.Any() == true)
                {
                    yield return $"\t\tOther Addresses:";
                    foreach (var other in otherAddresses)
                    {
                        yield return $"\t\t\t{other.PrefixOrigin}:\t{other.Address}\t({other.SuffixOrigin})";
                    }
                }

                //var dnsServers = ipInterfaceProperties.DnsAddresses;
                //if (dnsServers != null)
                //{
                //    foreach (var dns in dnsServers)
                //    {
                //        yield return $"\t\tDNS Servers:\t{dns}";
                //    }
                //}

                //var anyCast = ipInterfaceProperties.AnycastAddresses;
                //if (anyCast != null)
                //{
                //    foreach (var any in anyCast)
                //    {
                //        yield return $"\t\tAnycast Address:\t{any.Address}{(any.IsTransient ? " Transient" : "")}{(any.IsDnsEligible ? " DNS Eligible" : "")}";
                //    }
                //}

                //var multiCast = ipInterfaceProperties.MulticastAddresses;
                //if (multiCast != null)
                //{
                //    foreach (var multi in multiCast)
                //    {
                //        yield return $"\t\tMulticast Address:\t{multi.Address}{(multi.IsTransient ? " Transient" : "")}{(multi.IsDnsEligible ? " DNS Eligible" : "")}";
                //    }
                //}

                //var uniCast = ipInterfaceProperties.UnicastAddresses;
                //if (uniCast != null)
                //{
                //    string lifeTimeFormat = "dddd, MMMM dd, yyyy  hh:mm:ss tt";
                //    foreach (var uni in uniCast)
                //    {
                //        yield return $"\t\tUnicast Address:\t{uni.Address} (Prefix Origin: {uni.PrefixOrigin} - Suffix Origin: {uni.SuffixOrigin} - Duplicate Address Detection: {uni.DuplicateAddressDetectionState}) - ipv4Mask: {uni.IPv4Mask} - isTransient: {uni.IsTransient}";

                //        // Format the lifetimes as Sunday, February 16, 2003 11:33:44 PM
                //        // if en-us is the current culture.

                //        // Calculate the date and time at the end of the lifetimes.
                //        var when = DateTime.UtcNow + TimeSpan.FromSeconds(uni.AddressValidLifetime);
                //        when = when.ToLocalTime();
                //        yield return $"\t\t\tValid Life Time:\t{when.ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture)}";

                //        when = DateTime.UtcNow + TimeSpan.FromSeconds(uni.AddressPreferredLifetime);
                //        when = when.ToLocalTime();
                //        yield return $"\t\t\tPreferred Life Time:\t{when.ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture)}";

                //        when = DateTime.UtcNow + TimeSpan.FromSeconds(uni.DhcpLeaseLifetime);
                //        when = when.ToLocalTime();
                //        yield return $"\t\t\tDHCP Leased Life Time:\t{when.ToString(lifeTimeFormat, System.Globalization.CultureInfo.CurrentCulture)}";
                //    }
                //}

                if (networkInterface.Supports(NetworkInterfaceComponent.IPv4))
                {

                    var ipv4Properties = ipInterfaceProperties.GetIPv4Properties();

                    if (ipv4Properties != null)
                    {
                        yield return $"\t\tIPv4 Properties:";
                        //yield return $"\t\t\tIndex:\t{ipv4Properties.Index}";
                        yield return $"\t\t\tMTU:\t{ipv4Properties.Mtu}";
                        //yield return $"\t\t\tAPIPA active:\t{ipv4Properties.IsAutomaticPrivateAddressingActive}";
                        //yield return $"\t\t\tAPIPA enabled:\t{ipv4Properties.IsAutomaticPrivateAddressingEnabled}";
                        //yield return $"\t\t\tForwarding enabled:\t{ipv4Properties.IsForwardingEnabled}";
                        //yield return $"\t\t\tUses WINS:\t{ipv4Properties.UsesWins}";
                    }
                }
            }

            foreach (var line in NetworkInterfaceComponentStatistics(NetworkInterfaceComponent.IPv4))
            {
                yield return line;
            }
            foreach (var line in NetworkInterfaceComponentStatistics(NetworkInterfaceComponent.IPv6))
            {
                yield return line;
            }
        }

        private static IEnumerable<string> NetworkInterfaceComponentStatistics(NetworkInterfaceComponent version)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var stats = version switch
            {
                NetworkInterfaceComponent.IPv4 => properties.GetTcpIPv4Statistics(),
                _ => properties.GetTcpIPv6Statistics()
            };

            yield return $"TCP/{version} Statistics";
            yield return $"\tMinimum Transmission Timeout:\t{stats.MinimumTransmissionTimeout:#,#}";
            yield return $"\tMaximum Transmission Timeout:\t{stats.MaximumTransmissionTimeout:#,#}";
            yield return "\tConnection Data";
            yield return $"\t\tCurrent:\t{stats.CurrentConnections:#,#}";
            yield return $"\t\tCumulative:\t{stats.CumulativeConnections:#,#}";
            yield return $"\t\tInitiated:\t{stats.ConnectionsInitiated:#,#}";
            yield return $"\t\tAccepted:\t{stats.ConnectionsAccepted:#,#}";
            yield return $"\t\tFailed Attempts:\t{stats.FailedConnectionAttempts:#,#}";
            yield return $"\t\tReset:\t{stats.ResetConnections:#,#}";
            yield return "\tSegment Data";
            yield return $"\t\tReceived:\t{stats.SegmentsReceived:#,#}";
            yield return $"\t\tSent:\t{stats.SegmentsSent:#,#}";
            yield return $"\t\tRetransmitted:\t{stats.SegmentsResent:#,#}";
        }

        public static IEnumerable<string> GetVersionInformation()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            yield return $"{assembly.GetName().Name} version {assembly.GetName().Version}";
            yield return $"{{{assembly.GetName().FullName}}}";
            yield return $"Copyright (c) {DateTime.Now.Year}";
        }
    }
}
