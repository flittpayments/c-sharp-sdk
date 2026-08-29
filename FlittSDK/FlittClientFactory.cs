using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FlittSDK
{
    /// <summary>
    /// Default multi-merchant client factory backed by IHttpClientFactory.
    /// </summary>
    public sealed class FlittClientFactory : IFlittClientFactory
    {
        public const string HttpClientName = "FlittSDK";

        private readonly FlittClientOptions _defaults;
        private readonly IFlittTransport _factoryTransport;

        public FlittClientFactory(
            FlittClientOptions defaults,
            IHttpClientFactory httpClientFactory
        )
        {
            _defaults = Clone(defaults ?? throw new ArgumentNullException(nameof(defaults)));
            _factoryTransport = new HttpClientFactoryTransport(
                httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory))
            );
        }

        public IFlittClient CreateClient(FlittClientOptions options)
        {
            var copy = Clone(options ?? throw new ArgumentNullException(nameof(options)));
            if (copy.Transport == null)
            {
                copy.Transport = _factoryTransport;
            }

            return new FlittClient(copy);
        }

        public IFlittClient CreateClient(
            int merchantId,
            string secretKey,
            string creditKey = null
        )
        {
            var options = Clone(_defaults);
            options.MerchantId = merchantId;
            options.SecretKey = secretKey;
            options.CreditKey = creditKey;
            return CreateClient(options);
        }

        private static FlittClientOptions Clone(FlittClientOptions source)
        {
            return new FlittClientOptions
            {
                MerchantId = source.MerchantId,
                SecretKey = source.SecretKey,
                CreditKey = source.CreditKey,
                BaseAddress = source.BaseAddress,
                Protocol = source.Protocol,
                ContentType = source.ContentType,
                Timeout = source.Timeout,
                Transport = source.Transport
            };
        }

        private sealed class HttpClientFactoryTransport : IFlittTransport
        {
            private readonly IHttpClientFactory _httpClientFactory;

            internal HttpClientFactoryTransport(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }

            public async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken
            )
            {
                using (var client = _httpClientFactory.CreateClient(HttpClientName))
                {
                    return await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
        }
    }
}
