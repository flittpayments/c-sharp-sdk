using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Creates an Open Banking checkout URL.
    /// The returned URL is a bank deeplink/SCA URL and must be passed to the
    /// customer unchanged. Confirm payment through callbacks or order status.
    /// </summary>
    public class OpenBanking
    {
        private readonly IFlittClient _client;
        private static readonly string[] AllowedPaymentMethods =
            {"tbc", "bog", "liberty", "credo", "x"};

        public OpenBanking()
            : this(null)
        {
        }

        public OpenBanking(IFlittClient client)
        {
            _client = client;
        }

        public CheckoutResponse Post(OpenBankingRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public Task<CheckoutResponse> PostAsync(
            OpenBankingRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            req.payment_systems = "opb";
            req.payment_method = PaymentMethod.Validate(
                req.payment_method,
                AllowedPaymentMethods
            );
            return new Url(_client).PostAsync(req, cancellationToken);
        }
    }

    [JsonObject(Title = "request")]
    public class OpenBankingRequest : CheckoutRequest
    {
    }

    internal static class PaymentMethod
    {
        internal static string Validate(string value, string[] allowed)
        {
            value = string.IsNullOrEmpty(value) ? "x" : value;
            if (Array.IndexOf(allowed, value) < 0)
            {
                throw new ArgumentException(
                    "Incorrect payment_method. Allowed values: " + string.Join(", ", allowed),
                    "payment_method"
                );
            }

            return value;
        }
    }
}
