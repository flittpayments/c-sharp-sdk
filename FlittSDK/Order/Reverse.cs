using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Order
{
    /// <summary>
    /// Reverse Api
    /// </summary>
    public class Reverse
    {
        private readonly IFlittClient _client;

        public Reverse()
            : this(null)
        {
        }

        public Reverse(IFlittClient client)
        {
            _client = client;
        }

        /// <summary>
        /// By Order Id
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public ReverseByOrderResponse ByOrderID(ReverseByOrder req)
        {
            return ByOrderIDAsync(req).GetAwaiter().GetResult();
        }

        public async Task<ReverseByOrderResponse> ByOrderIDAsync(
            ReverseByOrder req,
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
                return await EndpointInvoker.InvokeAsync<ReverseByOrder, ReverseByOrderResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new ReverseByOrderResponse {Error = c};
            }
        }
        /// <summary>
        /// Reverse By Payment ID
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public ReverseByPaymentResponse ByPaymentID(ReverseByPayment req)
        {
            return ByPaymentIDAsync(req).GetAwaiter().GetResult();
        }

        public async Task<ReverseByPaymentResponse> ByPaymentIDAsync(
            ReverseByPayment req,
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
                return await EndpointInvoker.InvokeAsync<ReverseByPayment, ReverseByPaymentResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new ReverseByPaymentResponse {Error = c};
            }
        }
        /// <summary>
        /// Reverse By Transaction Id
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public ReverseByTransactionId ByTransactionID(ReverseByTransaction req)
        {
            return ByTransactionIDAsync(req).GetAwaiter().GetResult();
        }

        public async Task<ReverseByTransactionId> ByTransactionIDAsync(
            ReverseByTransaction req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create();
            try
            {
                return await EndpointInvoker.InvokeAsync<ReverseByTransaction, ReverseByTransactionId>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new ReverseByTransactionId {Error = c};
            }
        }

        /// <summary>
        /// Reverse the full available amount, excluding client fees and any
        /// amount that has already been reversed.
        /// </summary>
        public ReverseByOrderResponse Full(ReverseByOrder req)
        {
            return FullAsync(req).GetAwaiter().GetResult();
        }

        public async Task<ReverseByOrderResponse> FullAsync(
            ReverseByOrder req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var status = await new Status(_client).StatusByOrderIdAsync(new StatusByOrderRequest
            {
                order_id = req.order_id
            }, cancellationToken).ConfigureAwait(false);
            if (status.Error != null)
            {
                return new ReverseByOrderResponse {Error = status.Error};
            }

            req.amount = OrderAmounts.ReverseAmount(status);
            return await ByOrderIDAsync(req, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Alias matching Python SDK's reverse_full method.
        /// </summary>
        public ReverseByOrderResponse ReverseFull(ReverseByOrder req)
        {
            return Full(req);
        }

        public Task<ReverseByOrderResponse> ReverseFullAsync(
            ReverseByOrder req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return FullAsync(req, cancellationToken);
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class ReverseByOrder : Models.ReverseRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"reverse/order_id/";
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class ReverseByPayment : Models.ReverseRequestModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"reverse/payment_id/";
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class ReverseByTransaction : Models.ReverseByTransactionModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"reverse/order_id/";
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class ReverseByOrderResponse : Models.ReverseResponseModel
    {
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class ReverseByPaymentResponse : Models.ReverseResponseModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class ReverseByTransactionId : Models.ReverseResponseModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }
    }
}
