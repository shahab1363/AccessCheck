using System.Net.Sockets;

namespace Checker.Checks.SocketCheck
{
    public class TCPCheckConfiguration : RawSocketCheckConfiguration
    {
        public override CheckTypeEnum Type => CheckTypeEnum.TCP;
        public override SocketType? SocketType => System.Net.Sockets.SocketType.Stream;
        public override ProtocolType? ProtocolType => System.Net.Sockets.ProtocolType.Tcp;
    }
}

