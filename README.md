# Flitt C# SDK

<p align="center">
	<a href="https://www.nuget.org/packages/FlittSDK/"><img src="https://img.shields.io/nuget/v/FlittSDK.svg" /></a>
	<a href="https://www.nuget.org/packages/FlittSDK/"><img src="https://img.shields.io/nuget/dt/FlittSDK.svg" /></a>
</p>

## Payment service provider
A payment service provider (PSP) offers shops online services for accepting electronic payments by a variety of payment methods including credit card, bank-based payments such as direct debit, bank transfer, and real-time bank transfer based on online banking. Typically, they use a software as a service model and form a single payment gateway for their clients (merchants) to multiple payment methods. 
[read more](https://en.wikipedia.org/wiki/Payment_service_provider)

## Installation

SDK availble on [NuGet](https://www.nuget.org/packages/FlittSDK/).

## Requirements

Flitt account - [Register here](https://portal.flitt.com/mportal/#/account/registration)

Newtonsoft.json (JSON.NET)

The package targets `netstandard2.0` and `netstandard2.1`. Existing protocol
1.0 JSON, form, and XML integrations remain binary-compatible. XML is
deprecated; protocol 2.0 uses JSON.


## Simple Start
```csharp
using FlittSDK;
using FlittSDK.Checkout;

IFlittClient client = new FlittClient(new FlittClientOptions {
  MerchantId = 1549901,
  SecretKey = "test",
  CreditKey = "testcredit",
  Protocol = "2.0",
  Timeout = TimeSpan.FromSeconds(30)
});

var req = new CheckoutRequest {
  order_id = Guid.NewGuid().ToString("N"),
  amount = 100000,
  order_desc = "checkout json demo",
  currency = "GEL"
};
var resp = await new Url(client).PostAsync(req, cancellationToken);
if (resp.Error == null) {
 string url = resp.checkout_url;
}
```
# Api

See the [official C# SDK documentation](https://docs.flitt.com/sdk-and-mobile/sdk/csharp/).

## New payment methods

Open Banking:

```csharp
var response = new OpenBanking(client).Post(new OpenBankingRequest {
    order_id = Guid.NewGuid().ToString(),
    order_desc = "Open Banking payment",
    amount = 10000,
    currency = "GEL",
    payment_method = "tbc" // tbc, bog, liberty, credo, x
});
```

Installments use `new Installments(client).Post(...)` with `payment_method` equal to
`tbc` or `x`. The resulting URL is a bank deeplink/SCA URL: pass it to the
customer unchanged and confirm the payment using the server callback or order
status.

IBAN payout:

```csharp
var payout = await new FlittSDK.Payment.IbanCredit(client).PostAsync(new IbanCreditRequest {
    amount = 10000,
    currency = "GEL",
    receiver_iban = "GE00TB0000000000000001"
});
```

IBAN and P2P payouts use the client's `CreditKey`; purchases use `SecretKey`.

## Order updates

```csharp
var capture = new FlittSDK.Order.Capture(client).CaptureFull(
    new CaptureRequest { order_id = orderId, currency = "GEL" });

var reverse = new FlittSDK.Order.Reverse(client).ReverseFull(
    new ReverseByOrder { order_id = orderId, currency = "GEL" });

var fiscal = new FlittSDK.Order.FiscalData(client).Post(
    new FiscalDataRequest { order_id = orderId });
```

`Atol` is retained as an obsolete compatibility adapter and now calls
`/fiscal_data/`. Calendar payments can be stopped with
`new Subscription(client).Stop(orderId)`.

## Dependency injection, async, and transport

`FlittClient` keeps credentials and response state per instance/per request.
Register `IFlittClient` once per merchant in a DI container and inject it into
endpoint classes. All primary endpoint classes provide true asynchronous
counterparts accepting a `CancellationToken`:

```csharp
services.AddSingleton<IFlittClient>(new FlittClient(new FlittClientOptions {
    MerchantId = merchantId,
    SecretKey = secretKey,
    CreditKey = creditKey,
    Protocol = "2.0",
    Timeout = TimeSpan.FromSeconds(15),
    Transport = new HttpClientTransport(httpClient)
}));

var response = await new Token(client).PostAsync(request, cancellationToken);
```

The transport is based exclusively on reusable `HttpClient`; there is no
`WebRequest`/`HttpWebRequest` path. Custom transports implement
`IFlittTransport`, which also makes integration tests deterministic without
sending payment requests. `IFlittClient` can be mocked directly.

The static `Config` and `Client` APIs are obsolete compatibility facades. They
remain available so existing 1.x binaries and source integrations continue to
work, but new multi-merchant or concurrent applications should not use them.
`FlittContentType.Xml` and `XmlFormatter` are also obsolete; migrate to JSON.

## Company Reports

Company Reports is a separate service and uses `application_id`/`key`, not the
merchant secret:

```csharp
var reports = new CompanyReports(client);
var report = await reports.GetAsync(new CompanyReportsRequest {
    application_id = "1019",
    key = "test",
    report_id = 745,
    merchant_id = 1549902
});
```

The old `Payment.Reports.Post` method remains available for the legacy
merchant `/reports/` endpoint. `GetCompanyReport` is a migration shortcut to
the separate Company Reports service.

## Test stands

The repository includes isolated NuGet consumer stands for a clean 2.0.0
installation and an unchanged application upgraded from 1.0.0 to 2.0.0.
The full runner also executes package API validation and can enable real Flitt
API integration tests:

```bash
RUN_LIVE_TESTS=1 FLITT_BASELINE_PACKAGE=/path/to/FlittSDK.1.0.0.nupkg \
  bash tests/run-all.sh
```

See [tests/TestStands/README.md](tests/TestStands/README.md) for details.

## Examples
To check it you can use build-in ISS server
[http://localhost:7777/](http://localhost:7777/)
