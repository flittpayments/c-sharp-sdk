using FlittSDK.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Settlement url Api
    /// </summary>
    public class Settlement
    {
        private readonly IFlittClient _client;

        public Settlement()
            : this(null)
        {
        }

        public Settlement(IFlittClient client)
        {
            _client = client;
        }

        public SettlementResponse Post(SettlementRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<SettlementResponse> PostAsync(
            SettlementRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create("2.0", "json");
            if (client.Protocol != "2.0" || client.ContentType != "json")
            {
                throw new InvalidOperationException("Settlement requires protocol 2.0 with JSON.");
            }

            req.merchant_id = client.MerchantId;
            req.order_type = "settlement";
            try
            {
                var response = await client.InvokeAsync<SettlementRequest, SettlementResponse>(
                    req,
                    req.ActionUrl,
                    true,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);
                return response.data == null
                    ? response
                    : JsonFormatter.ConvertFromJson<SettlementResponse>(response.data, true, "order");
            }
            catch (ClientException c)
            {
                return new SettlementResponse {Error = c};
            }
        }
    }

    [JsonObject(Title = "request")]
    public class SettlementRequest : Models.CheckoutRequestModel
    {
        [JsonIgnore] public readonly string ActionUrl = @"settlement/";
    }

    [JsonObject(Title = "response")]
    public class SettlementResponse : Models.ResponseModel
    {
    }
}
