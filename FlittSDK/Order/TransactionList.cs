using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Order
{
    /// <summary>
    /// Transaction list Api
    /// </summary>
    public class TransactionList
    {
        private readonly IFlittClient _client;

        public TransactionList()
            : this(null)
        {
        }

        public TransactionList(IFlittClient client)
        {
            _client = client;
        }

        public TransactionListResponse Post(TransactionListRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<TransactionListResponse> PostAsync(
            TransactionListRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var client = _client ?? LegacyConfigClientFactory.Create();
            req.merchant_id = client.MerchantId;
            req.signature = Signature.GetRequestSignature(
                RequiredParams.GetHashProperties(req, "json"),
                false,
                client.SecretKey
            );
            try
            {
                return await client.InvokeAsync<TransactionListRequest, TransactionListResponse>(
                    req,
                    req.ActionUrl,
                    false,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new TransactionListResponse {Error = c};
            }
        }
    }
    
    [JsonObject(Title = "request")]
    public class TransactionListRequest
    {
        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }
        
        [JsonProperty(PropertyName = "order_id")]
        public string order_id { get; set; }

        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"transaction_list/";
    }
    public class TransactionListResponse
    {
        [JsonProperty(PropertyName = "response")]
        public List<Models.TransactionModel> response { get; set; }
        [JsonIgnore] [XmlIgnore] public ClientException Error { get; set; }
    }
}
