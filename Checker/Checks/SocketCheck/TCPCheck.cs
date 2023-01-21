namespace Checker.Checks.SocketCheck
{
    public class TCPCheck : RawSocketCheck
    {
        public TCPCheck(TCPCheckConfiguration tcpCheckConfiguration, TimeSpan? overrideMinInterval)
            : base(tcpCheckConfiguration, overrideMinInterval)
        {
        }
    }
}
