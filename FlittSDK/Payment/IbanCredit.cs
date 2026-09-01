using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Payment
{
    /// <summary>
    /// IBAN payout API. Requests are signed with the client's credit key.
    /// </summary>
    public class IbanCredit
    {
        private readonly IFlittClient _client;

        public IbanCredit()
            : this(null)
        {
        }

        public IbanCredit(IFlittClient client)
        {
            _client = client;
        }

        public IbanCreditResponse Post(IbanCreditRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<IbanCreditResponse> PostAsync(
            IbanCreditRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (string.IsNullOrEmpty(req.order_id))
            {
                req.order_id = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(req.order_desc))
            {
                req.order_desc = "Pay for order #: " + req.order_id;
            }

            if (string.IsNullOrEmpty(req.currency))
            {
                throw new ArgumentException("currency is required", nameof(req));
            }

            if (string.IsNullOrEmpty(req.receiver_iban))
            {
                throw new ArgumentException("receiver_iban is required", nameof(req));
            }

            var client = _client ?? LegacyConfigClientFactory.Create();
            req.merchant_id = client.MerchantId;
            req.version = client.Protocol;
            req.signature = Signature.GetRequestSignature(
                RequiredParams.GetHashProperties(req, client.ContentType),
                true,
                client.CreditKey
            );

            try
            {
                var response = await client.InvokeAsync<IbanCreditRequest, IbanCreditResponse>(
                    req,
                    req.ActionUrl,
                    true,
                    true,
                    cancellationToken
                ).ConfigureAwait(false);

                if (response.data != null && client.Protocol == "2.0")
                {
                    return JsonFormatter.ConvertFromJson<IbanCreditResponse>(response.data, true, "order");
                }

                return response;
            }
            catch (ClientException exception)
            {
                return new IbanCreditResponse {Error = exception};
            }
        }
    }

    /// <summary>
    /// Compatibility spelling matching the Python method name.
    /// </summary>
    public class Ibancredit : IbanCredit
    {
        public Ibancredit()
        {
        }

        public Ibancredit(IFlittClient client)
            : base(client)
        {
        }
    }

    [JsonObject(Title = "request")]
    [XmlRoot("request")]
    public class IbanCreditRequest : Models.IbanCreditRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"ibancredit/";
    }

    [JsonObject(Title = "response")]
    [XmlRoot("response")]
    public class IbanCreditResponse : Models.IbanCreditResponseModel
    {
    }
}
