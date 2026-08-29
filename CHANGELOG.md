# Changelog

## 2.0.0

- Added Open Banking and installments checkout helpers.
- Added IBAN payouts signed with `Config.CreditKey`.
- Added full capture and reverse helpers that account for client fees.
- Replaced the retired ATOL endpoint with fiscalisation data; the old `Atol`
  class remains as an obsolete adapter.
- Added calendar subscription stop and the separate Company Reports client.
- Added async APIs with cancellation, a 30-second default timeout, and a
  replaceable `HttpClient` transport.
- Added the instance-based `FlittClient`, `FlittClientOptions`, and
  `IFlittClient` for DI, mocks, multi-merchant isolation, and configurable
  timeouts.
- Deprecated the static `Config`/`Client` compatibility path and XML transport;
  both remain available for 1.x compatibility.
- Removed `WebRequest`/`HttpWebRequest`; asynchronous calls now use real
  `async`/`await` through reusable `HttpClient` instances.
- Replaced request-global response state with per-call state in the HTTP
  client, accepted HTTP 201 responses, and improved transport errors.
- Corrected protocol 2.0 envelopes so legacy `signature` and `version` fields
  are not duplicated inside the encoded order.
- Added constant-time callback signature comparison and flexible parsing of
  nested `additional_info` data.
- Retained all public members from package 1.0.0 for binary compatibility.

The package continues to target `netstandard2.0` and `netstandard2.1`.
