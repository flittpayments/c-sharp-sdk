using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FlittSDK.Models;
using FlittSDK.Utils;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlittSDK.Payment
{
    public class Reports
    {
        private readonly IFlittClient _client;

        public Reports()
            : this(null)
        {
        }

        public Reports(IFlittClient client)
        {
            _client = client;
        }

        public ReportsResponse Post(ReportsRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public async Task<ReportsResponse> PostAsync(
            ReportsRequest req,
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
                return await EndpointInvoker.InvokeAsync<ReportsRequest, ReportsResponse>(
                    client,
                    req,
                    req.ActionUrl,
                    false,
                    false,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (ClientException c)
            {
                return new ReportsResponse {Error = c};
            }
        }

        /// <summary>
        /// Compatibility shortcut to the separate Company Reports service.
        /// The existing Post method continues to call the legacy /reports/
        /// merchant endpoint.
        /// </summary>
        public CompanyReportsResponse GetCompanyReport(CompanyReportsRequest req)
        {
            return new CompanyReports(_client ?? LegacyConfigClientFactory.Create()).Get(req);
        }

        public Task<CompanyReportsResponse> GetCompanyReportAsync(
            CompanyReportsRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            return new CompanyReports(_client ?? LegacyConfigClientFactory.Create())
                .GetAsync(req, cancellationToken);
        }
    }

    [XmlRoot("request")]
    [JsonObject(Title = "request")]
    public class ReportsRequest
    {
        [JsonProperty(PropertyName = "merchant_id")]
        public int merchant_id { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string signature { get; set; }

        [JsonProperty(PropertyName = "date_from")]
        public string date_from { get; set; }

        [JsonProperty(PropertyName = "date_to")]
        public string date_to { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "version")]
        public string version { get; set; }

        [JsonIgnore] [XmlIgnore] public readonly string ActionUrl = @"reports/";
    }
    [XmlRoot("response")]
    public class ReportsResponse : ResponseV2
    {
        [JsonProperty(PropertyName = "response")]
        [XmlElement("order")]
        public List<ResponseModel> response { get; set; }

        [JsonIgnore] [XmlIgnore] public ClientException Error { get; set; }
    }
}
