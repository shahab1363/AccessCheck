namespace Checker.Checks.SocketCheck
{
    public class UDPCheck : RawSocketCheck
    {
        public UDPCheck(UDPCheckConfiguration udpCheckConfiguration, TimeSpan? overrideMinInterval)
            : base(udpCheckConfiguration, overrideMinInterval)
        {
        }
    }
}
