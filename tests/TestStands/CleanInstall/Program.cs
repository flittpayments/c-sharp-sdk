using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FlittSDK;
using FlittSDK.Checkout;
using Microsoft.Extensions.DependencyInjection;

var transport = new StandTransport();
var services = new ServiceCollection();
services.AddFlitt(options =>
{
    options.MerchantId = 1549901;
    options.SecretKey = "test";
    options.CreditKey = "testcredit";
    options.BaseAddress = new Uri("https://pay.flitt.test/api/");
    options.Protocol = "2.0";
    options.ContentType = FlittContentType.Json;
    options.Timeout = TimeSpan.FromSeconds(2);
    options.Transport = transport;
});
using var provider = services.BuildServiceProvider();
IFlittClient client = provider.GetRequiredService<IFlittClient>();
IFlittClientFactory factory = provider.GetRequiredService<IFlittClientFactory>();
IFlittClient dynamicClient = factory.CreateClient(2549901, "dynamic-secret", "dynamic-credit");

var response = await new Url(client).PostAsync(new CheckoutRequest
{
    order_id = "clean-install",
    order_desc = "Clean NuGet installation",
    amount = 100,
    currency = "GEL"
});

Assert(response.Error == null, "response error");
Assert(response.checkout_url == "https://bank.example/clean-install", "checkout URL");
Assert(transport.RequestCount == 1, "request count");
Assert(transport.LastMerchantId == 1549901, "merchant ID");
Assert(transport.LastEnvelopeSignatureLength == 40, "v2 signature");
Assert(dynamicClient.MerchantId == 2549901, "dynamic merchant factory");
Assert(dynamicClient.BaseAddress == client.BaseAddress, "factory defaults");
Console.WriteLine("Clean install stand passed with FlittSDK 2.0.0.");

static void Assert(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException("Assertion failed: " + name);
    }
}

internal sealed class StandTransport : IFlittTransport
{
    public int RequestCount { get; private set; }

    public int LastMerchantId { get; private set; }

    public int LastEnvelopeSignatureLength { get; private set; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string body = await request.Content.ReadAsStringAsync(cancellationToken);
        using var envelope = JsonDocument.Parse(body);
        var requestNode = envelope.RootElement.GetProperty("request");
        string encoded = requestNode.GetProperty("data").GetString();
        LastEnvelopeSignatureLength = requestNode.GetProperty("signature").GetString().Length;

        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        using var orderDocument = JsonDocument.Parse(decoded);
        LastMerchantId = orderDocument.RootElement
            .GetProperty("order")
            .GetProperty("merchant_id")
            .GetInt32();
        RequestCount++;

        string payload = "{\"order\":{\"response_status\":\"success\"," +
                         "\"checkout_url\":\"https://bank.example/clean-install\"," +
                         "\"payment_id\":1}}";
        string responseBody = "{\"response\":{\"version\":\"2.0\",\"data\":\"" +
                              Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)) +
                              "\",\"signature\":\"stand\"}}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}
