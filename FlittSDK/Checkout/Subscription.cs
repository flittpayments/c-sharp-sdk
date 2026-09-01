using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FlittSDK.Models;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Subscription Api
    /// </summary>
    public class Subscription
    {
        private readonly IFlittClient _client;

        public Subscription()
            : this(null)
        {
        }

        public Subscription(IFlittClient client)
        {
            _client = client;
        }

        public SubscriptionResponse Post(SubscriptionRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<SubscriptionResponse> PostAsync(
            SubscriptionRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create("2.0", "json");
            if (client.Protocol != "2.0" || client.ContentType != "json")
            {
                throw new InvalidOperationException("Subscription requires protocol 2.0 with JSON.");
            }

            req.merchant_id = client.MerchantId;
            req.version = "2.0";
            req.subscription = "Y";
            ValidateRecurringData(req.recurring_data);
            try
            {
                var response = await client.InvokeAsync<SubscriptionRequest, SubscriptionResponse>(
                    req,
                    req.ActionUrl,
                    true,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);
                return response.data == null
                    ? response
                    : JsonFormatter.ConvertFromJson<SubscriptionResponse>(response.data, true, "order");
            }
            catch (ClientException c)
            {
                return new SubscriptionResponse {Error = c};
            }
        }

        /// <summary>
        /// Stop calendar payments for an order. Protocol 2.0 is used without
        /// changing the process-wide Config values.
        /// </summary>
        public SubscriptionStopResponse Stop(string orderId)
        {
            return StopAsync(orderId).GetAwaiter().GetResult();
        }

        public async Task<SubscriptionStopResponse> StopAsync(
            string orderId,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create("2.0", "json");
            if (client.Protocol != "2.0" || client.ContentType != "json")
            {
                throw new InvalidOperationException("Subscription stop requires protocol 2.0 with JSON.");
            }

            var request = new SubscriptionStopRequest
            {
                order_id = orderId,
                action = "stop",
                merchant_id = client.MerchantId
            };

            try
            {
                var response = await client.InvokeAsync<SubscriptionStopRequest, SubscriptionStopResponse>(
                    request,
                    request.ActionUrl,
                    true,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);
                return response.data == null
                    ? response
                    : JsonFormatter.ConvertFromJson<SubscriptionStopResponse>(response.data, true, "order");
            }
            catch (ClientException exception)
            {
                return new SubscriptionStopResponse {Error = exception};
            }
        }

        private static void ValidateRecurringData(ReccuringData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            DateTime parsed;
            if (!DateTime.TryParseExact(
                    data.start_time,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out parsed))
            {
                throw new ArgumentException("Incorrect date format. 'yyyy-MM-dd' is allowed", nameof(data));
            }

            if (data.period != "day" && data.period != "week" && data.period != "month")
            {
                throw new ArgumentException("Incorrect period. 'day', 'week' or 'month' is allowed", nameof(data));
            }
        }
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class SubscriptionRequest : CheckoutRequestModel
    {
        [JsonProperty(PropertyName = "recurring_data")]
        public ReccuringData recurring_data { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"checkout/url/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class SubscriptionResponse : CheckoutResponseModel
    {
        [JsonProperty(PropertyName = "payment_id")]
        public int payment_id { get; set; }

        [JsonProperty(PropertyName = "checkout_url")]
        public string checkout_url { get; set; }
    }

    [JsonObject(Title = "request")]
    public class SubscriptionStopRequest
    {
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "action")]
        public string action { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonIgnore] public readonly string ActionUrl = @"subscription/";
    }

    [JsonObject(Title = "response")]
    public class SubscriptionStopResponse : ResponseModel
    {
    }
}
