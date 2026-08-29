using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Models;
using FlittSDK.Utils;

namespace FlittSDK
{
    /// <summary>
    /// Thread-safe, instance-based Flitt API client suitable for dependency
    /// injection and simultaneous use with multiple merchant accounts.
    /// </summary>
    public sealed class FlittClient : IFlittClient
    {
        private const string Agent = "FlittPay-csharp-sdk/2.0.0";
        private readonly IFlittTransport _transport;
        private readonly TimeSpan _timeout;

        public FlittClient(FlittClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Protocol != "1.0" && options.Protocol != "2.0")
            {
                throw new ArgumentException("Protocol must be '1.0' or '2.0'.", nameof(options));
            }

            string contentType = options.ContentTypeName;
            if (options.Protocol == "2.0" && contentType != "json")
            {
                throw new ArgumentException("Protocol 2.0 supports JSON only.", nameof(options));
            }

            BaseAddress = NormalizeBaseAddress(options.BaseAddress);

            MerchantId = options.MerchantId;
            SecretKey = options.SecretKey;
            CreditKey = options.CreditKey;
            Protocol = options.Protocol;
            ContentType = contentType;
            _timeout = options.Timeout;
            _transport = options.Transport ?? new HttpClientTransport();
        }

        public int MerchantId { get; }

        public string SecretKey { get; }

        public string CreditKey { get; }

        public Uri BaseAddress { get; }

        [Obsolete("Use BaseAddress.")]
        public string ApiHost
        {
            get { return BaseAddress.Authority; }
        }

        public string Protocol { get; }

        public string ContentType { get; }

        public TResponse Invoke<TRequest, TResponse>(
            TRequest request,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false
        )
        {
            return InvokeAsync<TRequest, TResponse>(request, actionUrl, isRoot, isCredit)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
            TRequest request,
            string actionUrl,
            bool isRoot = true,
            bool isCredit = false,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            string secretKey = isCredit ? CreditKey : SecretKey;
            string data = Protocol == "2.0"
                ? RequiredParams.GetParamsV2(request, isCredit, secretKey)
                : RequiredParams.ConvertRequestByContentType(request, ContentType);

            string responseBody = await SendAsync(
                new Uri(BaseAddress, actionUrl.TrimStart('/')),
                data,
                GetContentTypeHeader(ContentType),
                null,
                cancellationToken
            ).ConfigureAwait(false);

            ErrorResponseModel errorResponse;
            try
            {
                errorResponse = RequiredParams.ConvertResponseByContentType<ErrorResponseModel>(
                    responseBody,
                    isRoot,
                    ContentType
                );
            }
            catch (Exception exception)
            {
                throw new ClientException
                {
                    ErrorCode = "500",
                    ErrorMessage = "Unable to parse API response: " + exception.Message,
                    RequestId = "Invalid response"
                };
            }

            if (errorResponse != null &&
                (errorResponse.response_status == "failure" || errorResponse.error_message != null))
            {
                throw new ClientException
                {
                    ErrorCode = errorResponse.error_code,
                    ErrorMessage = errorResponse.error_message,
                    RequestId = errorResponse.request_id
                };
            }

            return RequiredParams.ConvertResponseByContentType<TResponse>(
                responseBody,
                isRoot,
                ContentType
            );
        }

        public Task<string> SendJsonAsync(
            string url,
            string json,
            IDictionary<string, string> headers = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return SendAsync(
                new Uri(url, UriKind.Absolute),
                json,
                "application/json; charset=utf-8",
                headers,
                cancellationToken
            );
        }

        private static Uri NormalizeBaseAddress(Uri baseAddress)
        {
            if (baseAddress == null || !baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException("BaseAddress must be an absolute URI.", nameof(baseAddress));
            }

            if (baseAddress.Scheme != Uri.UriSchemeHttps && baseAddress.Scheme != Uri.UriSchemeHttp)
            {
                throw new ArgumentException("BaseAddress must use HTTP or HTTPS.", nameof(baseAddress));
            }

            var builder = new UriBuilder(baseAddress)
            {
                Path = baseAddress.AbsolutePath.TrimEnd('/') + "/",
                Query = string.Empty,
                Fragment = string.Empty
            };
            return builder.Uri;
        }

        private static string GetContentTypeHeader(string type)
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

        private async Task<string> SendAsync(
            Uri url,
            string body,
            string contentType,
            IDictionary<string, string> headers,
            CancellationToken cancellationToken
        )
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", Agent);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                request.Content = new StringContent(body, Encoding.UTF8);
                request.Content.Headers.Remove("Content-Type");
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

                if (_timeout > TimeSpan.Zero && _timeout != System.Threading.Timeout.InfiniteTimeSpan)
                {
                    timeout.CancelAfter(_timeout);
                }

                HttpResponseMessage response;
                try
                {
                    response = await _transport.SendAsync(request, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new ClientException
                    {
                        ErrorCode = "408",
                        ErrorMessage = "Request timed out",
                        RequestId = exception.Message
                    };
                }
                catch (HttpRequestException exception)
                {
                    throw new ClientException
                    {
                        ErrorCode = "500",
                        ErrorMessage = exception.Message,
                        RequestId = "Transport error"
                    };
                }

                using (response)
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    int statusCode = (int) response.StatusCode;
                    if (statusCode == 200 || statusCode == 201)
                    {
                        return responseBody;
                    }

                    throw new ClientException
                    {
                        ErrorCode = statusCode.ToString(),
                        ErrorMessage = responseBody,
                        RequestId = "HTTP request failed"
                    };
                }
            }
        }
    }
}
