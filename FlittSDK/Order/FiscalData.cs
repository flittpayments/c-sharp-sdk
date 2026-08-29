using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FlittSDK.Models;
using FlittSDK.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlittSDK.Order
{
    /// <summary>
    /// Polls fiscalisation data for an order.
    /// </summary>
    public class FiscalData
    {
        private readonly IFlittClient _client;

        public FiscalData()
            : this(null)
        {
        }

        public FiscalData(IFlittClient client)
        {
            _client = client;
        }

        public FiscalDataResponse Post(FiscalDataRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<FiscalDataResponse> PostAsync(
            FiscalDataRequest req,
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
                var response = await client.InvokeAsync<FiscalDataRequest, FiscalDataResponse>(
                    req,
                    req.ActionUrl,
                    true,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);

                if (response.data != null && client.Protocol == "2.0")
                {
                    return JsonFormatter.ConvertFromJson<FiscalDataResponse>(response.data, true, "order");
                }

                return response;
            }
            catch (ClientException exception)
            {
                return new FiscalDataResponse {Error = exception};
            }
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class FiscalDataRequest
    {
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"fiscal_data/";
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class FiscalDataResponse : ResponseV2
    {
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "fiscalisation_data")]
        public Dictionary<string, FiscalisationEntry> fiscalisation_data { get; set; }

        [JsonProperty(PropertyName = "response_status")]
        public string response_status { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "error")]
        public string error { get; set; }

        [JsonIgnore] [XmlIgnore] public ClientException Error { get; set; }
    }

    public class FiscalisationEntry
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "status_code")]
        public int? status_code { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "code")]
        public int? code { get; set; }

        // The API returns either a string or a localized object here.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "message")]
        public JToken message { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "terminal_id")]
        public string terminal_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "date")]
        public string date { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "fiscal_sign")]
        public string fiscal_sign { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "qr_code_url")]
        public string qr_code_url { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "type")]
        public int? type { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "receipt_id")]
        public long? receipt_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "external")]
        public bool? external { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "processed_date")]
        public long? processed_date { get; set; }
    }
}
