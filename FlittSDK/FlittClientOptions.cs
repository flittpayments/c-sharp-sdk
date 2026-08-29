using System;

namespace FlittSDK
{
    /// <summary>
    /// Supported request serializations. XML remains available for legacy
    /// protocol 1.0 integrations but is deprecated.
    /// </summary>
    public enum FlittContentType
    {
        Json,
        Form,

        [Obsolete("XML transport is deprecated. Migrate to JSON.")]
        Xml
    }

    /// <summary>
    /// Per-client Flitt configuration. Credentials are instance state and can
    /// safely differ between clients in the same process.
    /// </summary>
    public sealed class FlittClientOptions
    {
        private Uri _baseAddress = new Uri("https://pay.flitt.com/api/", UriKind.Absolute);

        public int MerchantId { get; set; }

        public string SecretKey { get; set; }

        public string CreditKey { get; set; }

        /// <summary>
        /// Absolute Flitt API root. A trailing slash is normalized by FlittClient.
        /// </summary>
        public Uri BaseAddress
        {
            get { return _baseAddress; }
            set { _baseAddress = value; }
        }

        /// <summary>
        /// Legacy host-only configuration. Use BaseAddress instead.
        /// </summary>
        [Obsolete("Use BaseAddress with an absolute URI, for example https://pay.flitt.com/api/.")]
        public string ApiHost
        {
            get { return _baseAddress == null ? null : _baseAddress.Authority; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _baseAddress = null;
                    return;
                }

                Uri absolute;
                if (Uri.TryCreate(value, UriKind.Absolute, out absolute))
                {
                    _baseAddress = absolute;
                    return;
                }

                _baseAddress = new Uri(
                    "https://" + value.Trim().TrimEnd('/') + "/api/",
                    UriKind.Absolute
                );
            }
        }

        public string Protocol { get; set; } = "1.0";

        public FlittContentType ContentType { get; set; } = FlittContentType.Json;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public IFlittTransport Transport { get; set; }

        internal string ContentTypeName
        {
            get
            {
                switch (ContentType)
                {
#pragma warning disable 0618
                    case FlittContentType.Xml:
                        return "xml";
#pragma warning restore 0618
                    case FlittContentType.Form:
                        return "form";
                    default:
                        return "json";
                }
            }
        }
    }
}
