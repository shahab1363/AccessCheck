namespace Checker.Checks.SocketCheck
{
    public class UDPCheck : RawSocketCheck
    {
        public override CheckTypeEnum Type => CheckTypeEnum.UDP;

        public UDPCheck(UDPCheckConfiguration udpCheckConfiguration, TimeSpan? overrideMinInterval)
            : base(udpCheckConfiguration, overrideMinInterval)
        {
        }
    }
}
