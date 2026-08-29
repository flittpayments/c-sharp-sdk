using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.P2pcredit
{
    /// <summary>
    /// Checkout P2pcredit Api
    /// </summary>
    public class P2Pcredit
    {
        private readonly IFlittClient _client;

        public P2Pcredit()
            : this(null)
        {
        }

        public P2Pcredit(IFlittClient client)
        {
            _client = client;
        }

        public P2PcreditResponse Post(P2PcreditRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<P2PcreditResponse> PostAsync(
            P2PcreditRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create();
            req.merchant_id = client.MerchantId;
            req.version = client.Protocol;
            req.signature = Signature.GetRequestSignature(
                RequiredParams.GetHashProperties(req, client.ContentType),
                true,
                client.CreditKey
            );
            try
            {
                return await EndpointInvoker.InvokeAsync<P2PcreditRequest, P2PcreditResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    true,
                    true,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new P2PcreditResponse {Error = c};
            }
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class P2PcreditRequest : Models.P2PcreditRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"p2pcredit/";
    }


    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class P2PcreditResponse : Models.P2PcreditResponseModel
    {
        [JsonIgnore] [XmlIgnore] public SignatureException SignatureError { get; set; }
    }
}
