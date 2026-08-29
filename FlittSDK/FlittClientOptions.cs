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
        public int MerchantId { get; set; }

        public string SecretKey { get; set; }

        public string CreditKey { get; set; }

        public string ApiHost { get; set; } = "pay.flitt.com";

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
