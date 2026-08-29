using FlittSDK;
using FlittSDK.Checkout;
using FlittSDK.P2pcredit;
using FlittSDK.Payment;

const int MerchantId = 1549901;
const string SecretKey = "test";
const string CreditKey = "testcredit";
const string ApiHost = "pay.flitt.com";
const string Card = "4444555511116666";
const string Card3Ds = "4444555566661111";
const string Cvv = "111";
const string Expiry = "0130";

var failures = new List<string>();
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));

await Run("checkout-json", async () =>
{
    var response = await new Url(Client(FlittContentType.Json)).PostAsync(
        Checkout("live-checkout-json", "USD"),
        cancellation.Token
    );
    Require(response.Error == null, Format(response.Error));
    Require(response.response_status == "success", "response_status=" + response.response_status);
    Require(response.payment_id != 0, "payment_id is empty");
});

#pragma warning disable CS0618
await Run("checkout-xml", async () =>
{
    var response = await new Url(Client(FlittContentType.Xml)).PostAsync(
        Checkout("live-checkout-xml", "GEL"),
        cancellation.Token
    );
    Require(response.Error == null, Format(response.Error));
    Require(response.response_status == "success", "response_status=" + response.response_status);
    Require(response.payment_id != 0, "payment_id is empty");
});
#pragma warning restore CS0618

await Run("token-json", () => TestToken(FlittContentType.Json, cancellation.Token));
#pragma warning disable CS0618
await Run("token-xml", () => TestToken(FlittContentType.Xml, cancellation.Token));
#pragma warning restore CS0618
await Run("token-form", () => TestToken(FlittContentType.Form, cancellation.Token));

await Run("p2pcredit-form", async () =>
{
    string orderId = OrderId("live-p2p");
    var response = await new P2Pcredit(Client(FlittContentType.Form)).PostAsync(
        new P2PcreditRequest
        {
            order_id = orderId,
            amount = 10000,
            order_desc = "Flitt C# SDK live P2P test",
            currency = "GEL",
            receiver_card_number = Card
        },
        cancellation.Token
    );
    Require(response.Error == null, Format(response.Error));
    Require(response.order_id == orderId, "unexpected order_id");
    Require(!string.IsNullOrEmpty(response.order_status), "order_status is empty");
});

await Run("pcidss-non-3ds", async () =>
{
    string orderId = OrderId("live-pcidss");
    var response = await new Pcidss(Client(FlittContentType.Json)).StepOneAsync(
        CardRequest(orderId, Card),
        cancellation.Token
    );
    Require(response.Error == null, Format(response.Error));
    Require(response.order_status == "approved", "order_status=" + response.order_status);
    Require(response.order_id == orderId, "unexpected order_id");
});

await Run("pcidss-3ds-form", async () =>
{
    var response = await new Pcidss(Client(FlittContentType.Form)).StepOneAsync(
        CardRequest(OrderId("live-pcidss-3ds"), Card3Ds),
        cancellation.Token
    );
    Require(response.Error == null, Format(response.Error));
    Require(!string.IsNullOrEmpty(response.md), "md is empty");
    Require(!string.IsNullOrEmpty(response.pareq), "pareq is empty");
});

if (failures.Count != 0)
{
    Console.Error.WriteLine("Live harness failures: " + string.Join(", ", failures));
    Environment.ExitCode = 1;
}
else
{
    Console.WriteLine("All live Flitt API scenarios passed.");
}

async Task Run(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine("PASS " + name);
    }
    catch (Exception exception)
    {
        failures.Add(name);
        Console.Error.WriteLine("FAIL " + name + ": " + exception.Message);
    }
}

async Task TestToken(FlittContentType contentType, CancellationToken cancellationToken)
{
    var response = await new Token(Client(contentType)).PostAsync(
        new TokenRequest
        {
            order_id = OrderId("live-token"),
            amount = 10500,
            order_desc = "Flitt C# SDK live token test",
            currency = "GEL"
        },
        cancellationToken
    );
    Require(response.Error == null, Format(response.Error));
    Require(response.response_status == "success", "response_status=" + response.response_status);
    Require(!string.IsNullOrEmpty(response.token), "token is empty");
}

IFlittClient Client(FlittContentType contentType)
{
    return new FlittClient(new FlittClientOptions
    {
        MerchantId = MerchantId,
        SecretKey = SecretKey,
        CreditKey = CreditKey,
        ApiHost = ApiHost,
        Protocol = "1.0",
        ContentType = contentType,
        Timeout = TimeSpan.FromSeconds(30)
    });
}

CheckoutRequest Checkout(string prefix, string currency)
{
    return new CheckoutRequest
    {
        order_id = OrderId(prefix),
        amount = 10000,
        order_desc = "Flitt C# SDK live checkout test",
        currency = currency
    };
}

StepOneRequest CardRequest(string orderId, string card)
{
    return new StepOneRequest
    {
        order_id = orderId,
        amount = 10000,
        order_desc = "Flitt C# SDK live PCI DSS test",
        currency = "GEL",
        card_number = card,
        cvv2 = Cvv,
        expiry_date = Expiry
    };
}

string OrderId(string prefix)
{
    return prefix + "-" + Guid.NewGuid().ToString("N");
}

void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string Format(ClientException error)
{
    return error == null
        ? "unknown SDK error"
        : "code=" + error.ErrorCode + ", message=" + error.ErrorMessage;
}
