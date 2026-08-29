using System.Xml.Serialization;
using Newtonsoft.Json;

namespace FlittSDK.Models
{
    /// <summary>
    /// IBAN payout request model.
    /// </summary>
    public class IbanCreditRequestModel
    {
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }

        [JsonProperty(PropertyName = "order_desc")]
        public string order_desc { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public int amount { get; set; }

        [JsonProperty(PropertyName = "currency")]
        public string currency { get; set; }

        [JsonProperty(PropertyName = "receiver_iban")]
        public string receiver_iban { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "merchant_data")]
        public string merchant_data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "server_callback_url")]
        public string server_callback_url { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "product_id")]
        public string product_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "reservation_data")]
        public string reservation_data { get; set; }
    }

    public class IbanCreditResponseModel : P2PcreditResponseModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "receiver_iban")]
        public string receiver_iban { get; set; }

        [JsonIgnore] [XmlIgnore] public new ClientException Error { get; set; }
    }
}
