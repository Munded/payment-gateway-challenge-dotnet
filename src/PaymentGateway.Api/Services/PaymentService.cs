using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IBankClient _bankClient;

    public PaymentService(
        ILogger<PaymentService> logger,
        PaymentsRepository paymentsRepository,
        IBankClient bankClient)
    {
        _logger = logger;
        _paymentsRepository = paymentsRepository;
        _bankClient = bankClient;
    }

    public async Task<ServiceCall<PostPaymentResponse>> ProcessPaymentAsync(PostPaymentRequest request)
    {

        var bankRequest = new BankPaymentRequest()
        {
            CardNumber = request.CardNumber,
            ExpiryDate = $"{request.ExpiryMonth}/{request.ExpiryYear}",
            Currency = request.Currency.ToUpperInvariant(),
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        var bankResult = await _bankClient.ProcessPaymentAsync(bankRequest);

        if (bankResult == null || !bankResult.IsSuccess || bankResult.Response == null)
        {
            _logger.LogWarning(
                "Bank Client returned an error for {CardLastFour}",
                PaymentResponseHelper.GetLastFourDigits(bankRequest.CardNumber));
            return ServiceCall<PostPaymentResponse>.Failure();
        }

        var status = bankResult.Response.Authorized
            ? PaymentStatus.Authorized
            : PaymentStatus.Declined;

        var payment = PaymentResponseHelper.Build(request, status);
        _paymentsRepository.Add(payment);

        return ServiceCall<PostPaymentResponse>.Success(payment);
    }
}
