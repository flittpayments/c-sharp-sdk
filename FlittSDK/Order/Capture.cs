using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Order
{
    /// <summary>
    /// Order capture Api
    /// </summary>
    public class Capture
    {
        private readonly IFlittClient _client;

        public Capture()
            : this(null)
        {
        }

        public Capture(IFlittClient client)
        {
            _client = client;
        }

        public CaptureResponse Post(CaptureRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<CaptureResponse> PostAsync(
            CaptureRequest req,
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
                return await EndpointInvoker.InvokeAsync<CaptureRequest, CaptureResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new CaptureResponse {Error = c};
            }
        }

        /// <summary>
        /// Capture the full available amount, excluding the client fee.
        /// The current order status is fetched before capture.
        /// </summary>
        public CaptureResponse Full(CaptureRequest req)
        {
            return FullAsync(req).GetAwaiter().GetResult();
        }

        public async Task<CaptureResponse> FullAsync(
            CaptureRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var status = await new Status(_client).StatusByOrderIdAsync(new StatusByOrderRequest
            {
                order_id = req.order_id
            }, cancellationToken).ConfigureAwait(false);
            if (status.Error != null)
            {
                return new CaptureResponse {Error = status.Error};
            }

            req.amount = OrderAmounts.CaptureAmount(status);
            return await PostAsync(req, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Alias matching Python SDK's capture_full method.
        /// </summary>
        public CaptureResponse CaptureFull(CaptureRequest req)
        {
            return Full(req);
        }

        public Task<CaptureResponse> CaptureFullAsync(
            CaptureRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return FullAsync(req, cancellationToken);
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class CaptureRequest : Models.CaptureRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"capture/order_id/";
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class CaptureResponse : Models.CaptureResponseModel
    {
    }
}
