using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlittSDK
{
    /// <summary>
    /// Injectable, mockable Flitt API client.
    /// </summary>
    public interface IFlittClient
    {
        int MerchantId { get; }

        string SecretKey { get; }

        string CreditKey { get; }

        string ApiHost { get; }

        string Protocol { get; }

        string ContentType { get; }

        TResponse Invoke<TRequest, TResponse>(
            TRequest request,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false
        );

        Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest request,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false,
            CancellationToken cancellationToken = default(CancellationToken)
        );

        Task<string> SendJsonAsync(
            string url,
            string json,
            IDictionary<string, string> headers = null,
            CancellationToken cancellationToken = default(CancellationToken)
        );
    }
}
