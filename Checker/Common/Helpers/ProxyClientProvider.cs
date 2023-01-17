using Yove.Proxy;

namespace CheckerLib.Common.Helpers
{
    public static class ProxyClientProvider
    {
        public static ProxyClient? GetProxyClient(Uri proxyUri)
        {
            if (proxyUri == null)
            {
                return null;
            }

            string? username = null, password = null;

            if (!string.IsNullOrEmpty(proxyUri.UserInfo))
            {
                var userInfoParts = proxyUri.UserInfo.Split(':');

                if (userInfoParts.Length > 0)
                {
                    username = userInfoParts[0];
                }

                if (userInfoParts.Length > 1)
                {
                    password = userInfoParts[1];
                }
            }

            switch (proxyUri.Scheme)
            {
                case "noproxy":
                    return null;
                case "http":
                case "https":
                    return new ProxyClient(proxyUri.Host, proxyUri.Port, username, password, ProxyType.Http);
                case "socks4a":
                case "socks4":
                    return new ProxyClient(proxyUri.Host, proxyUri.Port, username, password, ProxyType.Socks4);
                case "socks5":
                    return new ProxyClient(proxyUri.Host, proxyUri.Port, username, password, ProxyType.Socks5);
                default:
                    throw new NotSupportedException($"Proxy scheme {proxyUri.Scheme} is not supported.");
            }
        }
    }
}
