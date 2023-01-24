using System.Collections.Concurrent;

namespace CheckerLib.Common.Factories
{
    public static class HttpClientFactory
    {
        public static HttpClient HttpClientProvider()
          => HttpClientProvider(null);

        public static HttpClient HttpClientProvider(Uri? configProxyUri)
        {
            if (configProxyUri == null)
            {
                configProxyUri = new Uri("noproxy://");
            }

            return proxiedHttpClientPool.GetOrAdd(configProxyUri, (proxyUri) =>
            {
                var socketHandler = new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2), // Recreate every 2 minutes
                };

                var proxyClient = ProxyClientFactory.GetProxyClient(proxyUri);
                if (proxyClient != null)
                {
                    socketHandler.Proxy = proxyClient;
                }

                return new HttpClient(socketHandler);
            });
        }

        private static ConcurrentDictionary<Uri, HttpClient> proxiedHttpClientPool = new ConcurrentDictionary<Uri, HttpClient>();
    }
}
