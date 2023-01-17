namespace Checker.Checks.SocketCheck
{
    public class TCPCheck : RawSocketCheck
    {
        public override CheckTypeEnum Type => CheckTypeEnum.TCP;

        public TCPCheck(TCPCheckConfiguration tcpCheckConfiguration, TimeSpan? overrideMinInterval)
            : base(tcpCheckConfiguration, overrideMinInterval)
        {
        }
    }
}
