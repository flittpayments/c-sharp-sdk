using System.Xml.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;
namespace FlittSDK.Order
{
    [Obsolete("The ATOL endpoint was replaced by FiscalData. Use FlittSDK.Order.FiscalData.")]
    public class Atol
    {
        private readonly IFlittClient _client;

        public Atol()
            : this(null)
        {
        }

        public Atol(IFlittClient client)
        {
            _client = client;
        }

        public AtolResponse Post(AtolRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<AtolResponse> PostAsync(
            AtolRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var response = await new FiscalData(_client).PostAsync(
                new FiscalDataRequest {order_id = req.order_id},
                cancellationToken
            ).ConfigureAwait(false);
            return new AtolResponse
            {
                order_id = response.order_id,
                fiscalisation_data = response.fiscalisation_data,
                response_status = response.response_status,
                error = response.error,
                Error = response.Error
            };
        }
    }
    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class AtolRequest
    {
        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }
        
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"fiscal_data/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class AtolResponse
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
}
