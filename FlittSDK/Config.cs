using System;

namespace FlittSDK
{
    /// <summary>
    /// Legacy process-wide configuration. New code should construct a
    /// FlittClient with FlittClientOptions and inject IFlittClient.
    /// </summary>
    [Obsolete("Static Config is retained for compatibility only. Use FlittClientOptions and FlittClient.")]
    public static class Config
    {
        /// <summary>
        /// Merchant identification
        /// </summary>
        public static int MerchantId { get; set; }

        /// <summary>
        /// Merchant Secret Key
        /// </summary>
        public static string SecretKey { get; set; }

        /// <summary>
        /// Merchant Credit Key
        /// </summary>
        public static string CreditKey { get; set; }

        /// <summary>
        /// Get content type
        /// </summary>
        private static string contentType = "json";

        public static string ContentType
        {
            get { return contentType; }
            set { contentType = value; }
        }

        /// <summary>
        /// Protocol version supported (1.0/2.0)
        /// </summary>
        public static string Protocol = "1.0";

        /// <summary>
        /// api host
        /// </summary>
        public static string ApiHost = "pay.flitt.com";

        /// <summary>
        /// HTTP request timeout. The default matches the Python SDK.
        /// </summary>
        public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// HTTP transport used by the SDK. Replace it to integrate a custom
        /// HttpClient pipeline or a deterministic test transport.
        /// </summary>
        public static IFlittTransport Transport { get; set; } = new HttpClientTransport();


        /// <summary>
        /// Set api endpoint
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string Endpoint(string url)
        {
            string domain = @"https://{0}/api/";
            if (url == null)
            {
                url = ApiHost;
            }

            return string.Format(domain, url);
        }
    }
}
