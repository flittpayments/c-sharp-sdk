using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Payment
{
    public class Rectoken
    {
        private readonly IFlittClient _client;

        public Rectoken()
            : this(null)
        {
        }

        public Rectoken(IFlittClient client)
        {
            _client = client;
        }

        public RectokenResponse Post(RectokenRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<RectokenResponse> PostAsync(
            RectokenRequest req,
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
                return await EndpointInvoker.InvokeAsync<RectokenRequest, RectokenResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new RectokenResponse {Error = c};
            }
        }
    }
    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class RectokenRequest : Models.ReccuringRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"recurring/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class RectokenResponse : Models.ResponseModel
    {
        [JsonIgnore] [XmlIgnore] public new ClientException Error { get; set; }
    }
}
