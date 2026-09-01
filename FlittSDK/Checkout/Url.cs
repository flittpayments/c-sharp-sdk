using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Checkout url Api
    /// </summary>
    public class Url
    {
        private readonly IFlittClient _client;

        public Url()
            : this(null)
        {
        }

        public Url(IFlittClient client)
        {
            _client = client;
        }

        public CheckoutResponse Post(CheckoutRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<CheckoutResponse> PostAsync(
            CheckoutRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create();
            req.merchant_id = client.MerchantId;
            req.version = client.Protocol;
            req.signature = Signature.GetRequestSignature(
                RequiredParams.GetHashProperties(req, client.ContentType),
                false,
                client.SecretKey
            );
            try
            {
                return await EndpointInvoker.InvokeAsync<CheckoutRequest, CheckoutResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new CheckoutResponse {Error = c};
            }
        }
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class CheckoutRequest : Models.CheckoutRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"checkout/url/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class CheckoutResponse : Models.CheckoutResponseModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }

        [JsonProperty(PropertyName = "checkout_url")]
        public string checkout_url { get; set; }
    }
}
