using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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
    /// Default transport backed by a reusable HttpClient.
    /// </summary>
    public sealed class HttpClientTransport : IFlittTransport
    {
        private readonly HttpClient _httpClient;

        public HttpClientTransport()
            : this(new HttpClient {Timeout = System.Threading.Timeout.InfiniteTimeSpan})
        {
        }

        public HttpClientTransport(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return _httpClient.SendAsync(request, cancellationToken);
        }
    }
}
