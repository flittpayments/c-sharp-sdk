using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Verification url Api
    /// </summary>
    public class Verification
    {
        private readonly IFlittClient _client;

        public Verification()
            : this(null)
        {
        }

        public Verification(IFlittClient client)
        {
            _client = client;
        }

        public VerificationResponse Post(VerificationRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<VerificationResponse> PostAsync(
            VerificationRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create();
            req.merchant_id = client.MerchantId;
            req.verification = "Y";
            req.version = client.Protocol;
            if (req.verification_type == null)
            {
                req.verification_type = "code";
            }

            req.signature = Signature.GetRequestSignature(
                RequiredParams.GetHashProperties(req, client.ContentType),
                false,
                client.SecretKey
            );
            try
            {
                return await EndpointInvoker.InvokeAsync<VerificationRequest, VerificationResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new VerificationResponse {Error = c};
            }
        }
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class VerificationRequest : Models.CheckoutRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"checkout/url/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class VerificationResponse : Models.CheckoutResponseModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }

        [JsonProperty(PropertyName = "checkout_url")]
        public string checkout_url { get; set; }

        /// <summary>
        /// Backward-compatible alias for the historically mis-cased property.
        /// </summary>
        [JsonIgnore]
        public string Verification_url
        {
            get { return checkout_url; }
            set { checkout_url = value; }
        }
    }
}
