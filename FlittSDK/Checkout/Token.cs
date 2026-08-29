using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Checkout token Api
    /// </summary>
    public class Token
    {
        private readonly IFlittClient _client;

        public Token()
            : this(null)
        {
        }

        public Token(IFlittClient client)
        {
            _client = client;
        }

        public TokenResponse Post(TokenRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<TokenResponse> PostAsync(
            TokenRequest req,
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
                return await EndpointInvoker.InvokeAsync<TokenRequest, TokenResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new TokenResponse {Error = c};
            }
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class TokenRequest : Models.CheckoutRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"checkout/token/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class TokenResponse : Models.CheckoutResponseModel
    {
        [JsonProperty(PropertyName = "token")] public string token { get; set; }
    }
}
