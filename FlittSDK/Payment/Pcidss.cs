using System.Xml.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;

namespace FlittSDK.Payment
{
    /// <summary>
    /// Pcidss api
    /// </summary>
    public class Pcidss
    {
        private readonly IFlittClient _client;

        public Pcidss()
            : this(null)
        {
        }

        public Pcidss(IFlittClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Authorization
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public PcidssResponse StepOne(StepOneRequest req)
        {
            return StepOneAsync(req).GetAwaiter().GetResult();
        }

        public async Task<PcidssResponse> StepOneAsync(
            StepOneRequest req,
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
                return await EndpointInvoker.InvokeAsync<StepOneRequest, PcidssResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new PcidssResponse {Error = c};
            }
        }

        /// <summary>
        /// Submit if card 3ds
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public PcidssResponse StepTwo(StepTwoRequest req)
        {
            return StepTwoAsync(req).GetAwaiter().GetResult();
        }

        public async Task<PcidssResponse> StepTwoAsync(
            StepTwoRequest req,
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
                return await EndpointInvoker.InvokeAsync<StepTwoRequest, PcidssResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new PcidssResponse {Error = c};
            }
        }
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class StepOneRequest : Models.PcidssRequestModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"3dsecure_step1/";
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class StepTwoRequest : Models.PcidssAuthorizeModel
    {
        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"3dsecure_step2/";
    }

    [XmlRoot("response")]
    [JsonObject(Title = "response")]
    public class PcidssResponse : Models.PcidssResponseModel
    {
        [JsonIgnore] [XmlIgnore] public new ClientException Error { get; set; }
    }
}
