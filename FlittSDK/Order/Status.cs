using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Order
{
    /// <summary>
    /// Order Status Api
    /// </summary>
    public class Status
    {
        private readonly IFlittClient _client;

        public Status()
            : this(null)
        {
        }

        public Status(IFlittClient client)
        {
            _client = client;
        }

        public StatusResponse StatusByOrderId(StatusByOrderRequest req)
        {
            return StatusByOrderIdAsync(req).GetAwaiter().GetResult();
        }

        public async Task<StatusResponse> StatusByOrderIdAsync(
            StatusByOrderRequest req,
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
                return await EndpointInvoker.InvokeAsync<StatusByOrderRequest, StatusResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new StatusResponse {Error = c};
            }
        }

        public StatusResponse StatusByPaymentId(StatusByPaymentRequest req)
        {
            return StatusByPaymentIdAsync(req).GetAwaiter().GetResult();
        }

        public async Task<StatusResponse> StatusByPaymentIdAsync(
            StatusByPaymentRequest req,
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
                return await EndpointInvoker.InvokeAsync<StatusByPaymentRequest, StatusResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new StatusResponse {Error = c};
            }
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class StatusByOrderRequest
    {
        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }

        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"status/order_id";
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class StatusByPaymentRequest
    {
        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }

        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"status/payment_id";
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class StatusResponse : Models.ResponseModel
    {
        [JsonIgnore] [XmlIgnore] public new ClientException Error { get; set; }
    }
}
