using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

/// <summary>
/// End-to-end HTTP tests against the full API pipeline.
/// The acquiring bank is replaced with <see cref="BankStubClient"/> so Docker is not required.
/// </summary>
public class PaymentsApiE2ETests : IDisposable
{
    private readonly BankStubClient _bankClient = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PaymentsApiE2ETests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBankClient>();
                services.AddSingleton<IBankClient>(_bankClient);
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PostPayment_WhenBankAuthorizes_ReturnsAuthorized_AndGetRetrievesIt()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH-123"
        });

        var postResponse = await _client.PostAsJsonAsync("/api/Payments", ValidRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(PaymentStatus.Authorized, created.Status);
        Assert.Equal(8877, created.CardNumberLastFour);
        Assert.Equal("GBP", created.Currency);
        Assert.Equal(100, created.Amount);
        Assert.Equal(1, _bankClient.CallCount);

        var getResponse = await _client.GetAsync($"/api/Payments/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(PaymentStatus.Authorized, fetched.Status);
    }

    [Fact]
    public async Task PostPayment_WhenBankDeclines_ReturnsDeclined_AndGetRetrievesIt()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = false
        });

        var postResponse = await _client.PostAsJsonAsync("/api/Payments", ValidRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(PaymentStatus.Declined, created.Status);

        var getResponse = await _client.GetAsync($"/api/Payments/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetched);
        Assert.Equal(PaymentStatus.Declined, fetched.Status);
    }

    [Fact]
    public async Task PostPayment_WhenBankFails_ReturnsBadRequestRejected_AndIsNotStored()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Failure();

        var postResponse = await _client.PostAsJsonAsync("/api/Payments", ValidRequest());
        var body = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(PaymentStatus.Rejected, body.Status);
        Assert.Equal(1, _bankClient.CallCount);

        var getResponse = await _client.GetAsync($"/api/Payments/{body.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostPayment_WhenRequestInvalid_ReturnsBadRequestRejected_AndDoesNotCallBank()
    {
        var request = ValidRequest();
        request.CardNumber = "123";

        var postResponse = await _client.PostAsJsonAsync("/api/Payments", request);
        var body = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(PaymentStatus.Rejected, body.Status);
        Assert.Equal(0, _bankClient.CallCount);
    }

    [Fact]
    public async Task GetPayment_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };
}
