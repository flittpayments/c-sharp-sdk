using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FlittSDK
{
    /// <summary>
    /// Pluggable HTTP transport used by all Flitt API clients.
    /// </summary>
    public interface IFlittTransport
    {
        Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        );
    }

    /// <summary>
    /// Default transport backed by a process-wide IHttpClientFactory. The factory
    /// pools handlers and rotates them periodically so long-running applications
    /// reuse connections without retaining stale DNS endpoints. When a HttpClient
    /// is supplied explicitly, its lifetime remains owned by the caller.
    /// </summary>
    public sealed class HttpClientTransport : IFlittTransport
    {
        private const string DefaultHttpClientName = "FlittSDK.Default";
        private static readonly TimeSpan DefaultHandlerLifetime = TimeSpan.FromMinutes(5);
        private static readonly IHttpClientFactory SharedHttpClientFactory =
            CreateSharedHttpClientFactory();

        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _httpClientName;

        public HttpClientTransport()
            : this(SharedHttpClientFactory, DefaultHttpClientName)
        {
        }

        public HttpClientTransport(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        internal HttpClientTransport(IHttpClientFactory httpClientFactory, string httpClientName)
        {
            _httpClientFactory = httpClientFactory ??
                                 throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClientName = string.IsNullOrWhiteSpace(httpClientName)
                ? throw new ArgumentException("HTTP client name is required.", nameof(httpClientName))
                : httpClientName;
        }

        public async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (_httpClient != null)
            {
                return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            using (var client = _httpClientFactory.CreateClient(_httpClientName))
            {
                return await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken
                ).ConfigureAwait(false);
            }
        }

        private static IHttpClientFactory CreateSharedHttpClientFactory()
        {
            var services = new ServiceCollection();
            services.AddHttpClient(DefaultHttpClientName, client =>
                {
                    client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                })
                .SetHandlerLifetime(DefaultHandlerLifetime);

            // The shared provider intentionally has process lifetime, matching the
            // handler pool it owns. IHttpClientFactory disposes expired handlers.
            return services.BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>();
        }
    }
}
