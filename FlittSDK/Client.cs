using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FlittSDK
{
    /// <summary>
    /// Legacy static client facade. New code should inject IFlittClient.
    /// </summary>
    [Obsolete("Static Client is retained for compatibility only. Inject IFlittClient instead.")]
    public static class Client
    {
        public static TCipspResponse Invoke<TCipspRequest, TCipspResponse>(
            TCipspRequest req,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false
        )
        {
            return LegacyConfigClientFactory.Create().Invoke<TCipspRequest, TCipspResponse>(
                req,
                actionUrl,
                isRoot,
                isCredit
            );
        }

        public static Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest req,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return LegacyConfigClientFactory.Create().InvokeAsync<TRequest, TResponse>(
                req,
                actionUrl,
                isRoot,
                isCredit,
                cancellationToken
            );
        }

        internal static TResponse InvokeWithSettings<TRequest, TResponse>(
            TRequest req,
            string actionUrl,
            bool isRoot,
            bool isCredit,
            string protocol,
            string contentType
        )
        {
            return LegacyConfigClientFactory.Create(protocol, contentType).Invoke<TRequest, TResponse>(
                req,
                actionUrl,
                isRoot,
                isCredit
            );
        }

        internal static Task<TResponse> InvokeWithSettingsAsync<TRequest, TResponse>(
            TRequest req,
            string actionUrl,
            bool isRoot,
            bool isCredit,
            string protocol,
            string contentType,
            string apiHost,
            string secretKey,
            CancellationToken cancellationToken
        )
        {
            return LegacyConfigClientFactory.Create(protocol, contentType, apiHost, secretKey, isCredit)
                .InvokeAsync<TRequest, TResponse>(
                    req,
                    actionUrl,
                    isRoot,
                    isCredit,
                    cancellationToken
                );
        }

        internal static Task<string> SendJsonAsync(
            string url,
            string json,
            IDictionary<string, string> headers,
            CancellationToken cancellationToken
        )
        {
            return LegacyConfigClientFactory.Create().SendJsonAsync(url, json, headers, cancellationToken);
        }

        internal static string GetContentTypeHeader(string type)
        {
            switch (type)
            {
                case "xml":
                    return "application/xml; charset=utf-8";
                case "form":
                    return "application/x-www-form-urlencoded; charset=utf-8";
                default:
                    return "application/json; charset=utf-8";
            }
        }

    }
}
