using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FlittSDK.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlittSDK.Payment
{
    /// <summary>
    /// Client for the separate Flitt Company Reports service.
    /// It uses application credentials rather than merchant credentials.
    /// </summary>
    public class CompanyReports
    {
        private const string DefaultDomain = "portal.flitt.com";
        private readonly IFlittClient _client;
        private readonly string _baseUrl;

        public CompanyReports(string apiDomain = DefaultDomain)
            : this(LegacyConfigClientFactory.Create(), apiDomain)
        {
        }

        public CompanyReports(IFlittClient client)
            : this(client, DefaultDomain)
        {
        }

        public CompanyReports(IFlittClient client, string apiDomain)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(apiDomain))
            {
                throw new ArgumentException("Reports API domain is required", nameof(apiDomain));
            }

            _baseUrl = apiDomain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       apiDomain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? apiDomain.TrimEnd('/')
                : "https://" + apiDomain.TrimEnd('/');
        }

        public CompanyReportsResponse Get(CompanyReportsRequest req)
        {
            return GetAsync(req).GetAwaiter().GetResult();
        }

        public async Task<CompanyReportsResponse> GetAsync(
            CompanyReportsRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (string.IsNullOrWhiteSpace(req.application_id))
            {
                throw new ArgumentException("application_id is required", nameof(req));
            }

            if (string.IsNullOrWhiteSpace(req.key))
            {
                throw new ArgumentException("key is required", nameof(req));
            }

            if (req.report_id == null || string.IsNullOrWhiteSpace(req.report_id.ToString()))
            {
                throw new ArgumentException("report_id is required", nameof(req));
            }

            try
            {
                string date = DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss.ffffff",
                    CultureInfo.InvariantCulture
                );
                var tokenRequest = new
                {
                    application_id = req.application_id,
                    date,
                    signature = Signature.GetReportsSignature(req.key, req.application_id, date)
                };
                string tokenJson = await _client.SendJsonAsync(
                    _baseUrl + "/authorizer/token/application/get",
                    JsonConvert.SerializeObject(tokenRequest),
                    null,
                    cancellationToken
                ).ConfigureAwait(false);
                var token = JsonConvert.DeserializeObject<CompanyReportsTokenResponse>(tokenJson);
                if (token == null || string.IsNullOrWhiteSpace(token.token))
                {
                    return new CompanyReportsResponse
                    {
                        Error = new ClientException
                        {
                            ErrorCode = "500",
                            ErrorMessage = "Company Reports token response does not contain a token",
                            RequestId = "Invalid response"
                        }
                    };
                }

                var reportRequest = new JObject
                {
                    ["report_id"] = JToken.FromObject(req.report_id),
                    ["filters"] = JToken.FromObject(req.filters ?? new List<CompanyReportFilter>()),
                    ["on_page"] = req.on_page,
                    ["page"] = req.page
                };
                if (req.merchant_id.HasValue)
                {
                    reportRequest["merchant_id"] = req.merchant_id.Value;
                }

                string reportJson = await _client.SendJsonAsync(
                    _baseUrl + "/api/extend/company/report/",
                    reportRequest.ToString(Formatting.None),
                    new Dictionary<string, string>
                    {
                        {"Authorization", "Token " + token.token}
                    },
                    cancellationToken
                ).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<CompanyReportsResponse>(reportJson)
                       ?? new CompanyReportsResponse();
            }
            catch (ClientException exception)
            {
                return new CompanyReportsResponse {Error = exception};
            }
            catch (JsonException exception)
            {
                return new CompanyReportsResponse
                {
                    Error = new ClientException
                    {
                        ErrorCode = "500",
                        ErrorMessage = "Unable to parse Company Reports response: " + exception.Message,
                        RequestId = "Invalid response"
                    }
                };
            }
        }

        public CompanyReportsResponse Reports(CompanyReportsRequest req)
        {
            return Get(req);
        }
    }

    public class CompanyReportsRequest
    {
        [JsonProperty(PropertyName = "application_id")]
        public string application_id { get; set; }

        [JsonProperty(PropertyName = "key")]
        public string key { get; set; }

        [JsonProperty(PropertyName = "report_id")]
        public object report_id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "merchant_id")]
        public int? merchant_id { get; set; }

        [JsonProperty(PropertyName = "filters")]
        public List<CompanyReportFilter> filters { get; set; } = new List<CompanyReportFilter>();

        [JsonProperty(PropertyName = "on_page")]
        public int on_page { get; set; } = 10;

        [JsonProperty(PropertyName = "page")]
        public int page { get; set; } = 1;
    }

    public class CompanyReportFilter
    {
        [JsonProperty(PropertyName = "s")]
        public string s { get; set; }

        [JsonProperty(PropertyName = "m")]
        public string m { get; set; }

        [JsonProperty(PropertyName = "v")]
        public string v { get; set; }
    }

    public class CompanyReportsResponse
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "data")]
        public JArray data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "fields")]
        public JArray fields { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "rows_count")]
        public int? rows_count { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "rows_on_page")]
        public int? rows_on_page { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "rows_page")]
        public int? rows_page { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "error")]
        public JToken error { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "err_code")]
        public JToken err_code { get; set; }

        [JsonIgnore] public ClientException Error { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> additional_data { get; set; }
    }

    internal class CompanyReportsTokenResponse
    {
        [JsonProperty(PropertyName = "token")]
        public string token { get; set; }
    }
}
