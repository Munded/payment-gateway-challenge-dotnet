# Payment Gateway API

ASP.NET Core payment gateway that validates payment requests, calls the acquiring bank simulator, and stores the result for later retrieval.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) (for the bank simulator)

## Run locally

### 1. Start the bank simulator

From the repository root:

```bash
docker compose up -d
```

The simulator listens on `http://localhost:8080` (configured in `appsettings.json` as `BankClient:BaseUrl`).

### 2. Start the API

From the repository root:

```bash
dotnet run --project src/PaymentGateway.Api
```

The API listens on:

- `https://localhost:7092`
- `http://localhost:5067`

Swagger UI: `https://localhost:7092/swagger`

## Run tests

From the repository root:

```bash
dotnet test
```

Or target the test project only:

```bash
dotnet test test/PaymentGateway.Api.Tests
```

### What’s covered

| Suite | Style | Needs Docker? |
|-------|--------|---------------|
| `PaymentValidatorTests` / `PaymentServiceTests` / `PaymentsControllerTests` | Unit | No |
| `PaymentsApiE2ETests` | HTTP e2e via `WebApplicationFactory` (bank stubbed) | No |

Run only the e2e suite:

```bash
dotnet test test/PaymentGateway.Api.Tests --filter FullyQualifiedName~PaymentsApiE2ETests
```

## Payment status

`status` is a `PaymentStatus` enum. ASP.NET serializes it as a **number** in JSON:

| JSON `status` | Name | Meaning |
|---------------|------|---------|
| `0` | Authorized | Acquiring bank approved the payment |
| `1` | Declined | Acquiring bank refused the payment |
| `2` | Rejected | Gateway did not complete processing — validation failed, or the bank call failed / was unavailable |

How it is set:

1. **Rejected (`2`)** — request fails `PaymentValidator`, **or** `IBankClient` returns failure (e.g. bank HTTP 503). Response is HTTP `400` with a Rejected body; payment is **not** stored.
2. **Authorized (`0`)** — bank responds successfully with `authorized: true`. HTTP `200`; payment is stored.
3. **Declined (`1`)** — bank responds successfully with `authorized: false`. HTTP `200`; payment is stored (so it can still be retrieved by id).

## Manual request scripts

Bank simulator rules (by last digit of `cardNumber`):

| Last digit | Bank result | Gateway `status` |
|------------|-------------|------------------|
| Odd (`1`,`3`,`5`,`7`,`9`) | Authorized | `0` (Authorized) |
| Even (`2`,`4`,`6`,`8`) | Declined | `1` (Declined) |
| `0` | HTTP 503 | `2` (Rejected) |

### Authorized payment (POST)

```bash
curl -s -k -X POST https://localhost:7092/api/Payments \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber": "2222405343248877",
    "expiryMonth": 4,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 100,
    "cvv": "123"
  }'
```

### Declined payment (POST)

```bash
curl -s -k -X POST https://localhost:7092/api/Payments \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber": "2222405343248878",
    "expiryMonth": 4,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 100,
    "cvv": "123"
  }'
```

### Bank unavailable (POST → Rejected)

```bash
curl -s -k -X POST https://localhost:7092/api/Payments \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber": "2222405343248870",
    "expiryMonth": 4,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 100,
    "cvv": "123"
  }'
```

### Validation failure (POST → Rejected)

```bash
curl -s -k -X POST https://localhost:7092/api/Payments \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber": "123",
    "expiryMonth": 4,
    "expiryYear": 2030,
    "currency": "GBP",
    "amount": 100,
    "cvv": "123"
  }'
```

### Retrieve a payment (GET)

Replace `<payment-id>` with the `id` from a successful POST response:

```bash
curl -s -k https://localhost:7092/api/Payments/<payment-id>
```

### HTTP (no TLS)

Same paths on `http://localhost:5067` if you prefer not to use `-k` with HTTPS.

## To improve

### Observability
- [ ] Log the exception object in `catch` (`LogError(ex, ...)`) instead of message-only
- [ ] Prefer structured log templates over string interpolation (`{CardLastFour}`, `{StatusCode}`)
- [ ] Add correlation / payment id to logs; consider metrics (count by status, bank latency)

### Code / API
- [ ] Plumb `CancellationToken` from controller → `PaymentService` → `BankClient` / `HttpClient`
- [ ] Optionally serialize `PaymentStatus` as strings (`Authorized` / `Declined` / `Rejected`) via `JsonStringEnumConverter` for clearer API/Swagger
- [ ] Clarify HTTP status for bank outage (400 vs 502/503) vs validation failures
- [ ] Use `GetPaymentResponse` for GET (or remove the unused DTO)
- [ ] Make `PaymentsRepository` thread-safe / encapsulate the public `List`

### Packaging / hardening
- [ ] HttpClient timeout 
- [ ] Dockerfile for the API; optional compose service alongside the bank simulator
- [ ] Durable storage if moving beyond single-process in-memory repo

### Testing / docs
- [ ] If string enums are enabled, add Swagger descriptions for each status value
