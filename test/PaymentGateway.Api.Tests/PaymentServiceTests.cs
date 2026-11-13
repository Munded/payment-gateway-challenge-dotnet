using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentServiceTests
{
    private readonly BankStubClient _bankClient = new();
    private readonly PaymentsRepository _paymentsRepository = new();
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _sut = new PaymentService(
            NullLogger<PaymentService>.Instance,
            _paymentsRepository,
            _bankClient);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankAuthorizes_ReturnsAuthorizedAndStoresPayment()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH-123"
        });

        var result = await _sut.ProcessPaymentAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Response);
        Assert.Equal(PaymentStatus.Authorized, result.Response.Status);
        Assert.Equal(8877, result.Response.CardNumberLastFour);
        Assert.Equal(4, result.Response.ExpiryMonth);
        Assert.Equal(2030, result.Response.ExpiryYear);
        Assert.Equal("GBP", result.Response.Currency);
        Assert.Equal(100, result.Response.Amount);
        Assert.Single(_paymentsRepository.Payments);
        Assert.Same(result.Response, _paymentsRepository.Payments[0]);
        Assert.Equal(1, _bankClient.CallCount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankDeclines_ReturnsDeclinedAndStoresPayment()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = false
        });

        var result = await _sut.ProcessPaymentAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Response);
        Assert.Equal(PaymentStatus.Declined, result.Response.Status);
        Assert.Single(_paymentsRepository.Payments);
        Assert.Equal(1, _bankClient.CallCount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankFails_ReturnsFailureAndDoesNotStore()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Failure();

        var result = await _sut.ProcessPaymentAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Null(result.Response);
        Assert.Empty(_paymentsRepository.Payments);
        Assert.Equal(1, _bankClient.CallCount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_MapsRequestToBankRequest()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = true
        });

        var request = ValidRequest();
        request.Currency = "gbp";

        await _sut.ProcessPaymentAsync(request);

        Assert.NotNull(_bankClient.LastRequest);
        Assert.Equal(request.CardNumber, _bankClient.LastRequest.CardNumber);
        Assert.Equal("4/2030", _bankClient.LastRequest.ExpiryDate);
        Assert.Equal("GBP", _bankClient.LastRequest.Currency);
        Assert.Equal(request.Amount, _bankClient.LastRequest.Amount);
        Assert.Equal(request.Cvv, _bankClient.LastRequest.Cvv);
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
