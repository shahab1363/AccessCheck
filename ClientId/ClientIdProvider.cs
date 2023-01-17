using DeviceId;

namespace ClientId
{
    public static class ClientIdProvider
    {
        public static string GetUniqueId(bool includeUserInfo)
        {
            var deviceId = new DeviceIdBuilder()
                .AddMachineName()
                .OnWindows(windows => windows
                    .AddSystemDriveSerialNumber()
                    .AddMotherboardSerialNumber()
                    .AddProcessorId())
                .OnLinux(linux => linux
                    .AddSystemDriveSerialNumber()
                    .AddMotherboardSerialNumber())
                .OnMac(mac => mac
                    .AddSystemDriveSerialNumber()
                    .AddPlatformSerialNumber())
                .ToString();

            if (includeUserInfo)
            {
                deviceId = $"{Environment.UserName}@{Environment.MachineName}.{deviceId}";
            }

            return deviceId;
        }
    }
}