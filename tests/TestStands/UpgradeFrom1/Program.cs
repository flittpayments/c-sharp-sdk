using System.Security.Cryptography;
using System.Text;
using FlittSDK;
using FlittSDK.Checkout;
using FlittSDK.Utils;

Config.MerchantId = 1549901;
Config.SecretKey = "test";
Config.CreditKey = "testcredit";
Config.ContentType = "json";
Config.Protocol = "1.0";
Config.ApiHost = "pay.flitt.test";

string signature = Signature.GetRequestSignature(new[] {"one", "two"});
string expected = Convert.ToHexString(
    SHA1.HashData(Encoding.UTF8.GetBytes("test|one|two"))
).ToLowerInvariant();
Assert(signature == expected, "legacy signature");

var request = new CheckoutRequest
{
    order_id = "upgrade-compatible",
    order_desc = "Same source before and after update",
    amount = 100,
    currency = "GEL"
};
request.merchant_id = Config.MerchantId;
request.version = Config.Protocol;
request.signature = signature;

Assert(request.merchant_id == 1549901, "legacy request model");
Assert(request.signature == expected, "legacy request signature");
Assert(Config.Endpoint(null) == "https://pay.flitt.test/api/", "legacy endpoint");

var packageVersion = typeof(Config).Assembly
    .GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false)
    .Cast<System.Reflection.AssemblyFileVersionAttribute>()
    .Single()
    .Version;
Console.WriteLine("Upgrade stand passed with FlittSDK file version " + packageVersion + ".");

static void Assert(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException("Assertion failed: " + name);
    }
}
