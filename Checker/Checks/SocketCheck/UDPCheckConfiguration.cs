using System.Net.Sockets;

namespace Checker.Checks.SocketCheck
{
    public class UDPCheckConfiguration : RawSocketCheckConfiguration
    {
        public override CheckTypeEnum Type => CheckTypeEnum.UDP;
        public override SocketType? SocketType => System.Net.Sockets.SocketType.Dgram;
        public override ProtocolType? ProtocolType => System.Net.Sockets.ProtocolType.Udp;
    }
}

