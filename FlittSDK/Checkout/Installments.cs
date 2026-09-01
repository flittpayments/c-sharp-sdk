using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlittSDK.Checkout
{
    /// <summary>
    /// Creates an installments checkout URL.
    /// </summary>
    public class Installments
    {
        private readonly IFlittClient _client;
        private static readonly string[] AllowedPaymentMethods = {"tbc", "x"};

        public Installments()
            : this(null)
        {
        }

        public Installments(IFlittClient client)
        {
            _client = client;
        }

        public CheckoutResponse Post(InstallmentsRequest req)
        {
            return PostAsync(req).GetAwaiter().GetResult();
        }

        public Task<CheckoutResponse> PostAsync(
            InstallmentsRequest req,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            req.payment_systems = "installments";
            req.payment_method = PaymentMethod.Validate(
                req.payment_method,
                AllowedPaymentMethods
            );
            return new Url(_client).PostAsync(req, cancellationToken);
        }
    }

    [JsonObject(Title = "request")]
    public class InstallmentsRequest : CheckoutRequest
    {
    }
}
