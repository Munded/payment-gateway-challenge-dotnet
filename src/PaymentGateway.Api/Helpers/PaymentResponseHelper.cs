using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Helpers;

public static class PaymentResponseHelper
{
    public static PostPaymentResponse Build(
        PostPaymentRequest request,
        PaymentStatus status) => new()
    {
        // usually the id would be created in the db, but we'll generate it in the mapper for this
        Id = Guid.Empty,
        Status = status,
        CardNumberLastFour = GetLastFourDigits(request.CardNumber),
        ExpiryMonth = request.ExpiryMonth,
        ExpiryYear = request.ExpiryYear,
        Currency = request.Currency?.ToUpperInvariant() ?? string.Empty,
        Amount = request.Amount
    };

    public static PostPaymentResponse BuildRejected(PostPaymentRequest request) =>
        Build(request, PaymentStatus.Rejected);

    public static int GetLastFourDigits(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
        {
            return 0;
        }

        return int.TryParse(cardNumber[^4..], out var lastFour) ? lastFour : 0;
    }
}