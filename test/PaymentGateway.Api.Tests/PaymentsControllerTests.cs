using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly BankStubClient _bankClient = new();
    private readonly PaymentsRepository _paymentsRepository = new();
    private readonly PaymentValidator _paymentValidator = new(() => new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc));
    private readonly PaymentsController _sut;

    public PaymentsControllerTests()
    {
        var paymentService = new PaymentService(
            NullLogger<PaymentService>.Instance,
            _paymentsRepository,
            _bankClient);

        _sut = new PaymentsController(
            _paymentsRepository,
            paymentService,
            _paymentValidator,
            NullLogger<PaymentsController>.Instance);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenPaymentExists_ReturnsOk()
    {
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 8877,
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100
        };
        _paymentsRepository.Add(payment);

        var result = await _sut.GetPaymentAsync(payment.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(payment, ok.Value);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenPaymentMissing_ReturnsNotFound()
    {
        var result = await _sut.GetPaymentAsync(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenRequestInvalid_ReturnsBadRequestRejectedAndDoesNotCallBank()
    {
        var request = ValidRequest();
        request.CardNumber = "not-a-card";

        var result = await _sut.PostPaymentAsync(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = Assert.IsType<PostPaymentResponse>(badRequest.Value);
        Assert.Equal(PaymentStatus.Rejected, body.Status);
        Assert.Equal(0, _bankClient.CallCount);
        Assert.Empty(_paymentsRepository.Payments);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankAuthorizes_ReturnsOkAuthorized()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH-123"
        });

        var result = await _sut.PostPaymentAsync(ValidRequest());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<PostPaymentResponse>(ok.Value);
        Assert.Equal(PaymentStatus.Authorized, body.Status);
        Assert.Equal(8877, body.CardNumberLastFour);
        Assert.Equal(100, body.Amount);
        Assert.Equal(1, _bankClient.CallCount);
        Assert.Single(_paymentsRepository.Payments);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankDeclines_ReturnsOkDeclined()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Success(new BankPaymentResponse
        {
            Authorized = false
        });

        var result = await _sut.PostPaymentAsync(ValidRequest());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<PostPaymentResponse>(ok.Value);
        Assert.Equal(PaymentStatus.Declined, body.Status);
        Assert.Single(_paymentsRepository.Payments);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankFails_ReturnsBadRequestRejected()
    {
        _bankClient.Result = ServiceCall<BankPaymentResponse>.Failure();

        var result = await _sut.PostPaymentAsync(ValidRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var body = Assert.IsType<PostPaymentResponse>(badRequest.Value);
        Assert.Equal(PaymentStatus.Rejected, body.Status);
        Assert.Equal(1, _bankClient.CallCount);
        Assert.Empty(_paymentsRepository.Payments);
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
