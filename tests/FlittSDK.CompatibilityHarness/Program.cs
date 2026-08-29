using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using FlittSDK;
using FlittSDK.Checkout;
using FlittSDK.Order;
using FlittSDK.Payment;
using FlittSDK.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static async Task Main()
    {
        var transport = new FakeTransport();
        IFlittClient client = CreateClient(transport, 1549901, "test", "1.0");
        IFlittClient v2Client = CreateClient(transport, 1549901, "test", "2.0");
        IFlittClient secondMerchant = CreateClient(transport, 2549901, "second-secret", "2.0");

#pragma warning disable CS0618
        Config.Transport = transport;
        Config.ApiHost = "pay.flitt.test";
        Config.MerchantId = 1549901;
        Config.SecretKey = "test";
        Config.CreditKey = "testcredit";
        Config.ContentType = "json";
#pragma warning restore CS0618

        Assert(
            SignatureValue() ==
            "0a4b4c2202d74cf5c94494d1825e2393c0b3b4db8b6f657868fa16185b4389167d89812e9affb2359e2fea9325edab29ac5131922bab59da343b643637ac1f77",
            "Company Reports SHA-512 signature"
        );

        await TestOpenBanking(client, transport);
        await TestInstallments(client, transport);
        await TestV2EnvelopeAndMultiMerchant(v2Client, secondMerchant, transport);
        await TestIbanCredit(client, transport);
        await TestFullAmounts(client, transport);
        await TestFiscalDataAndAtolShim(client, transport);
        await TestCompanyReports(client, transport);
        await TestCancellationAndTimeout();
        await TestDependencyInjectionAndFactory(transport);
        await TestHttpClientFactoryLifetime();
        await TestLegacyFacade(transport);
        TestDeprecationMetadata();

        Console.WriteLine("Compatibility harness passed.");
    }

    private static IFlittClient CreateClient(
        IFlittTransport transport,
        int merchantId,
        string secretKey,
        string protocol,
        TimeSpan? timeout = null
    )
    {
        return new FlittClient(new FlittClientOptions
        {
            MerchantId = merchantId,
            SecretKey = secretKey,
            CreditKey = "testcredit",
            BaseAddress = new Uri("https://pay.flitt.test/api/"),
            Protocol = protocol,
            ContentType = FlittContentType.Json,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            Transport = transport
        });
    }

    private static string SignatureValue()
    {
        return FlittSDK.Utils.Signature.GetReportsSignature(
            "test",
            "1019",
            "2026-08-29 12:00:00.000000"
        );
    }

    private static async Task TestOpenBanking(IFlittClient client, FakeTransport transport)
    {
        var response = await new OpenBanking(client).PostAsync(new OpenBankingRequest
        {
            order_id = "opb-order",
            order_desc = "Open banking",
            amount = 10000,
            currency = "GEL",
            payment_method = "tbc"
        });
        Assert(response.checkout_url == "https://bank.example/opb-order", "Open Banking response");
        var request = transport.Last("checkout/url/").Root;
        Assert((string) request["payment_systems"] == "opb", "Open Banking payment_systems");
        Assert((string) request["payment_method"] == "tbc", "Open Banking payment_method");
    }

    private static async Task TestInstallments(IFlittClient client, FakeTransport transport)
    {
        var response = await new Installments(client).PostAsync(new InstallmentsRequest
        {
            order_id = "installments-order",
            order_desc = "Installments",
            amount = 5000,
            currency = "GEL"
        });
        Assert(response.checkout_url == "https://bank.example/installments-order", "Installments response");
        var request = transport.Last("checkout/url/").Root;
        Assert((string) request["payment_systems"] == "installments", "Installments payment_systems");
        Assert((string) request["payment_method"] == "x", "Installments default payment_method");
    }

    private static async Task TestV2EnvelopeAndMultiMerchant(
        IFlittClient firstClient,
        IFlittClient secondClient,
        FakeTransport transport
    )
    {
        var first = new Url(firstClient).PostAsync(new CheckoutRequest
        {
            order_id = "parallel-1",
            order_desc = "First",
            amount = 100,
            currency = "GEL"
        });
        var second = new Url(secondClient).PostAsync(new CheckoutRequest
        {
            order_id = "parallel-2",
            order_desc = "Second",
            amount = 200,
            currency = "GEL"
        });
        var responses = await Task.WhenAll(first, second);
        Assert(responses[0].checkout_url.EndsWith("parallel-1"), "Concurrent response one");
        Assert(responses[1].checkout_url.EndsWith("parallel-2"), "Concurrent response two");

        var requests = transport.All("checkout/url/")
            .Where(request => (string) request.Root["order_id"] is "parallel-1" or "parallel-2")
            .ToArray();
        Assert(requests.Length == 2, "Concurrent request count");
        Assert(
            requests.Select(request => (int) request.Root["merchant_id"]).OrderBy(value => value)
                .SequenceEqual(new[] {1549901, 2549901}),
            "Concurrent merchant credentials stay isolated"
        );
        Assert(
            requests.Select(request => request.EnvelopeSignature).Distinct().Count() == 2,
            "Concurrent merchant signatures stay isolated"
        );
        foreach (var request in requests)
        {
            Assert(request.Root["signature"] == null, "No legacy signature inside v2 order");
            Assert(request.Root["version"] == null, "No legacy version inside v2 order");
        }
    }

    private static async Task TestIbanCredit(IFlittClient client, FakeTransport transport)
    {
        var response = await new IbanCredit(client).PostAsync(new IbanCreditRequest
        {
            order_id = "iban-order",
            amount = 10000,
            currency = "GEL",
            receiver_iban = "GE00TB0000000000000001"
        });
        Assert(response.order_id == "iban-order", "IBAN response");
        Assert(
            (string) transport.Last("ibancredit/").Root["receiver_iban"] == "GE00TB0000000000000001",
            "IBAN request"
        );
    }

    private static async Task TestFullAmounts(IFlittClient client, FakeTransport transport)
    {
        var capture = await new Capture(client).FullAsync(new CaptureRequest
        {
            order_id = "amount-order",
            currency = "GEL"
        });
        Assert(capture.Error == null, "Full capture response");
        Assert((int) transport.Last("capture/order_id/").Root["amount"] == 950, "Full capture amount");

        var reverse = await new Reverse(client).FullAsync(new ReverseByOrder
        {
            order_id = "amount-order",
            currency = "GEL"
        });
        Assert(reverse.Error == null, "Full reverse response");
        Assert((int) transport.Last("reverse/order_id/").Root["amount"] == 650, "Full reverse amount");
    }

    private static async Task TestFiscalDataAndAtolShim(IFlittClient client, FakeTransport transport)
    {
        var fiscal = await new FiscalData(client)
            .PostAsync(new FiscalDataRequest {order_id = "fiscal-order"});
        Assert(fiscal.fiscalisation_data["9002999267"].receipt_id == 3340, "Fiscal data response");

#pragma warning disable CS0618
        var legacy = await new Atol(client)
            .PostAsync(new AtolRequest {order_id = "legacy-fiscal-order"});
#pragma warning restore CS0618
        Assert(legacy.fiscalisation_data.ContainsKey("9002999267"), "ATOL compatibility shim");
        Assert(transport.Last("fiscal_data/").Url.EndsWith("/api/fiscal_data/"), "Fiscal endpoint path");
    }

    private static async Task TestCompanyReports(IFlittClient client, FakeTransport transport)
    {
        var response = await new CompanyReports(client, "portal.flitt.test")
            .GetAsync(new CompanyReportsRequest
        {
            application_id = "1019",
            key = "test",
            report_id = 745,
            merchant_id = 1549902
        });
        Assert(response.rows_count == 1, "Company Reports response");
        Assert(
            transport.Last("api/extend/company/report/").Authorization == "Token report-token",
            "Company Reports authorization"
        );
    }

    private static async Task TestCancellationAndTimeout()
    {
        var cancelledClient = CreateClient(
            new HangingTransport(),
            1,
            "secret",
            "1.0",
            Timeout.InfiniteTimeSpan
        );
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            try
            {
                await new Url(cancelledClient).PostAsync(new CheckoutRequest
                {
                    order_id = "cancel",
                    order_desc = "Cancellation",
                    amount = 1,
                    currency = "GEL"
                }, cancellation.Token);
                throw new InvalidOperationException("Cancellation did not fail");
            }
            catch (OperationCanceledException exception)
            {
                Assert(exception.CancellationToken == cancellation.Token, "Caller cancellation token");
            }
        }

        var timeoutClient = CreateClient(
            new HangingTransport(),
            1,
            "secret",
            "1.0",
            TimeSpan.FromMilliseconds(20)
        );
        try
        {
            await timeoutClient.InvokeAsync<CheckoutRequest, CheckoutResponse>(
                new CheckoutRequest {order_id = "timeout"},
                "timeout/"
            );
            throw new InvalidOperationException("Timeout did not fail");
        }
        catch (ClientException exception)
        {
            Assert(exception.ErrorCode == "408", "Configurable timeout status");
        }
    }

    private static async Task TestDependencyInjectionAndFactory(FakeTransport transport)
    {
        var services = new ServiceCollection();
        services.AddFlitt(options =>
        {
            options.MerchantId = 1549901;
            options.SecretKey = "test";
            options.CreditKey = "testcredit";
            options.BaseAddress = new Uri("https://pay.flitt.test/api/");
            options.Protocol = "2.0";
            options.Transport = transport;
        });

        using (var provider = services.BuildServiceProvider())
        {
            var defaultClient = provider.GetRequiredService<IFlittClient>();
            var factory = provider.GetRequiredService<IFlittClientFactory>();
            Assert(defaultClient.BaseAddress.AbsoluteUri == "https://pay.flitt.test/api/",
                "DI BaseAddress");

            var merchantClient = factory.CreateClient(3549901, "dynamic-secret", "dynamic-credit");
            Assert(merchantClient.MerchantId == 3549901, "Factory merchant ID");
            Assert(merchantClient.SecretKey == "dynamic-secret", "Factory merchant secret");

            var response = await new Url(merchantClient).PostAsync(new CheckoutRequest
            {
                order_id = "factory-merchant",
                order_desc = "Dynamic merchant",
                amount = 300,
                currency = "GEL"
            });
            Assert(response.checkout_url.EndsWith("factory-merchant"), "Factory client request");
            Assert((int) transport.Last("checkout/url/").Root["merchant_id"] == 3549901,
                "Factory request merchant isolation");
        }
    }

    private static async Task TestLegacyFacade(FakeTransport transport)
    {
#pragma warning disable CS0618
        Config.Protocol = "1.0";
        var response = await new Url().PostAsync(new CheckoutRequest
        {
            order_id = "legacy-config",
            order_desc = "Legacy compatibility",
            amount = 1,
            currency = "GEL"
        });
#pragma warning restore CS0618
        Assert(response.checkout_url.EndsWith("legacy-config"), "Legacy Config compatibility");
        Assert(transport.Last("checkout/url/").Root["merchant_id"].Value<int>() == 1549901,
            "Legacy merchant compatibility");
    }

    private static async Task TestHttpClientFactoryLifetime()
    {
        var httpClientFactory = new RecordingHttpClientFactory();
        var factory = new FlittClientFactory(new FlittClientOptions
        {
            MerchantId = 4549901,
            SecretKey = "factory-http-secret",
            BaseAddress = new Uri("https://pay.flitt.test/api/")
        }, httpClientFactory);
        var client = factory.CreateClient(4549901, "factory-http-secret");

        for (int index = 0; index < 2; index++)
        {
            var response = await new Url(client).PostAsync(new CheckoutRequest
            {
                order_id = "http-factory-" + index,
                order_desc = "HTTP factory lifetime",
                amount = 1,
                currency = "GEL"
            });
            Assert(response.Error == null, "IHttpClientFactory response");
        }

        Assert(httpClientFactory.CreateCount == 2, "IHttpClientFactory client per request");
        Assert(httpClientFactory.DisposeCount == 2, "IHttpClientFactory client disposal");
    }

    private static void TestDeprecationMetadata()
    {
#pragma warning disable CS0618
        Assert(Attribute.IsDefined(typeof(Config), typeof(ObsoleteAttribute)), "Config is obsolete");
        Assert(Attribute.IsDefined(typeof(XmlFormatter), typeof(ObsoleteAttribute)), "XML is obsolete");
        var xmlMember = typeof(FlittContentType).GetMember(nameof(FlittContentType.Xml)).Single();
#pragma warning restore CS0618
        Assert(Attribute.IsDefined(xmlMember, typeof(ObsoleteAttribute)), "XML enum is obsolete");
    }

    private static void Assert(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Assertion failed: " + name);
        }
    }
}

internal sealed class FakeTransport : IFlittTransport
{
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string body = await request.Content.ReadAsStringAsync(cancellationToken);
        string url = request.RequestUri.ToString();
        string authorization = request.Headers.TryGetValues("Authorization", out var values)
            ? values.Single()
            : null;
        JToken root = ParseOrder(body);
        string envelopeSignature = (string) JObject.Parse(body)["request"]?["signature"];
        _requests.Enqueue(new RecordedRequest(url, body, authorization, root, envelopeSignature));

        string response;
        if (url.EndsWith("/authorizer/token/application/get"))
        {
            response = "{\"token\":\"report-token\"}";
        }
        else if (url.EndsWith("/api/extend/company/report/"))
        {
            response = "{\"data\":[[1]],\"fields\":[\"id\"],\"rows_count\":1,\"rows_on_page\":1,\"rows_page\":1}";
        }
        else if (url.EndsWith("/api/status/order_id"))
        {
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["order_id"] = (string) root["order_id"],
                ["actual_amount"] = "1000",
                ["reversal_amount"] = "100",
                ["additional_info"] = new JObject
                {
                    ["client_fee"] = 50,
                    ["capture_amount"] = 800
                }
            }, body);
        }
        else if (url.EndsWith("/api/fiscal_data/"))
        {
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["order_id"] = (string) root["order_id"],
                ["fiscalisation_data"] = new JObject
                {
                    ["9002999267"] = new JObject
                    {
                        ["status_code"] = 0,
                        ["message"] = "",
                        ["receipt_id"] = 3340,
                        ["processed_date"] = 1736320610322L
                    }
                }
            }, body);
        }
        else if (url.EndsWith("/api/capture/order_id/"))
        {
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["capture_status"] = "captured",
                ["order_id"] = (string) root["order_id"]
            }, body);
        }
        else if (url.EndsWith("/api/reverse/order_id/"))
        {
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["reverse_status"] = "approved",
                ["order_id"] = (string) root["order_id"]
            }, body);
        }
        else if (url.EndsWith("/api/ibancredit/"))
        {
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["order_status"] = "approved",
                ["order_id"] = (string) root["order_id"],
                ["currency"] = (string) root["currency"]
            }, body);
        }
        else
        {
            string orderId = (string) root["order_id"];
            response = ApiResponse(new JObject
            {
                ["response_status"] = "success",
                ["checkout_url"] = "https://bank.example/" + orderId,
                ["payment_id"] = 1
            }, body);
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(response, Encoding.UTF8, "application/json")
        };
    }

    public RecordedRequest Last(string path)
    {
        return _requests.Last(request => request.Url.Contains(path));
    }

    public IEnumerable<RecordedRequest> All(string path)
    {
        return _requests.Where(request => request.Url.Contains(path));
    }

    private static JToken ParseOrder(string body)
    {
        var request = JObject.Parse(body)["request"];
        var encoded = (string) request?["data"];
        if (encoded == null)
        {
            return request;
        }

        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        return JObject.Parse(decoded)["order"];
    }

    private static string ApiResponse(JObject order, string requestBody)
    {
        var request = JObject.Parse(requestBody)["request"];
        if (request?["data"] == null)
        {
            return new JObject(new JProperty("response", order)).ToString(Newtonsoft.Json.Formatting.None);
        }

        string payload = new JObject(new JProperty("order", order))
            .ToString(Newtonsoft.Json.Formatting.None);
        return new JObject(new JProperty("response", new JObject
        {
            ["version"] = "2.0",
            ["data"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),
            ["signature"] = "not-validated-by-request-client"
        })).ToString(Newtonsoft.Json.Formatting.None);
    }
}

internal sealed class HangingTransport : IFlittTransport
{
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }
}

internal sealed class RecordingHttpClientFactory : IHttpClientFactory
{
    private int _createCount;
    private int _disposeCount;

    public int CreateCount => _createCount;

    public int DisposeCount => _disposeCount;

    public HttpClient CreateClient(string name)
    {
        Interlocked.Increment(ref _createCount);
        return new HttpClient(new RecordingHandler(this))
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly RecordingHttpClientFactory _owner;

        internal RecordingHandler(RecordingHttpClientFactory owner)
        {
            _owner = owner;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"response\":{\"response_status\":\"success\",\"checkout_url\":\"https://bank.example/factory\",\"payment_id\":1}}",
                    Encoding.UTF8,
                    "application/json"
                )
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Increment(ref _owner._disposeCount);
            }

            base.Dispose(disposing);
        }
    }
}

internal sealed record RecordedRequest(
    string Url,
    string Body,
    string Authorization,
    JToken Root,
    string EnvelopeSignature
);
