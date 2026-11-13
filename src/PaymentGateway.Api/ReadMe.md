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

## Manual request scripts

Bank simulator rules (by last digit of `cardNumber`):

| Last digit | Bank result                          | Gateway status |
|------------|--------------------------------------|----------------|
| Odd (1,3,5,7,9) | Authorized                      | `Authorized` |
| Even (2,4,6,8)  | Declined                         | `Declined` |
| `0`             | HTTP 503                         | `Rejected` |

`Status` is serialized as a string enum: `Authorized`, `Declined`, or `Rejected` (see Swagger for descriptions).

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
