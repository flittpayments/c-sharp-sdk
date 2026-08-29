namespace FlittSDK
{
    internal static class LegacyConfigClientFactory
    {
        internal static string GetContentType()
        {
#pragma warning disable 0618
            return Config.ContentType;
#pragma warning restore 0618
        }

        internal static string GetSecretKey(bool isCredit)
        {
#pragma warning disable 0618
            return isCredit ? Config.CreditKey : Config.SecretKey;
#pragma warning restore 0618
        }

        internal static IFlittClient Create(
            string protocol = null,
            string contentType = null,
            string apiHost = null,
            string secretKey = null,
            bool isCredit = false
        )
        {
#pragma warning disable 0618
            return new FlittClient(new FlittClientOptions
            {
                MerchantId = Config.MerchantId,
                SecretKey = isCredit ? Config.SecretKey : (secretKey ?? Config.SecretKey),
                CreditKey = isCredit ? (secretKey ?? Config.CreditKey) : Config.CreditKey,
                BaseAddress = new System.Uri(
                    "https://" + (apiHost ?? Config.ApiHost).TrimEnd('/') + "/api/",
                    System.UriKind.Absolute
                ),
                Protocol = protocol ?? Config.Protocol,
                ContentType = ParseContentType(contentType ?? Config.ContentType),
                Timeout = Config.Timeout,
                Transport = Config.Transport
            });
#pragma warning restore 0618
        }

        private static FlittContentType ParseContentType(string contentType)
        {
            switch (contentType)
            {
#pragma warning disable 0618
                case "xml":
                    return FlittContentType.Xml;
#pragma warning restore 0618
                case "form":
                    return FlittContentType.Form;
                default:
                    return FlittContentType.Json;
            }
        }
    }
}
