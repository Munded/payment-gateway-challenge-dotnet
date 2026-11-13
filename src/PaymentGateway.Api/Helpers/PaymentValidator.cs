using System.Text.RegularExpressions;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public class PaymentValidator
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "GBP",
        "USD",
        "EUR"
    };

    private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

    private readonly Func<DateTime> _utcNow;

    public PaymentValidator() : this(() => DateTime.UtcNow)
    {
    }

    public PaymentValidator(Func<DateTime> utcNow)
    {
        _utcNow = utcNow;
    }

    public PaymentValidationResult Validate(PostPaymentRequest request)
    {
        var errors = new List<string>();

        ValidateCardNumber(request.CardNumber, errors);
        ValidateExpiry(request.ExpiryMonth, request.ExpiryYear, errors);
        ValidateCurrency(request.Currency, errors);
        ValidateAmount(request.Amount, errors);
        ValidateCvv(request.Cvv, errors);

        if (errors.Count == 0)
        {
            return PaymentValidationResult.Success();
        }

        return PaymentValidationResult.Failure(errors.ToArray());
    }

    private static void ValidateCardNumber(string? cardNumber, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            errors.Add("Card number is required.");
            return;
        }

        if (cardNumber.Length is < 14 or > 19)
        {
            errors.Add("Card number must be between 14 and 19 characters.");
        }

        if (!DigitsOnly.IsMatch(cardNumber))
        {
            errors.Add("Card number must contain only numeric characters.");
        }
    }

    private void ValidateExpiry(int expiryMonth, int expiryYear, List<string> errors)
    {
        if (expiryMonth is < 1 or > 12)
        {
            errors.Add("Expiry month must be between 1 and 12.");
            return;
        }

        if (expiryYear < 1)
        {
            errors.Add("Expiry year must be a valid year.");
            return;
        }

        var now = _utcNow();
        var endOfExpiryMonth = new DateTime(expiryYear, expiryMonth, DateTime.DaysInMonth(expiryYear, expiryMonth),
            23, 59, 59, DateTimeKind.Utc);

        if (endOfExpiryMonth < now)
        {
            errors.Add("Card expiry must be in the future.");
        }
    }

    private static void ValidateCurrency(string? currency, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add("Currency is required.");
            return;
        }

        if (currency.Length != 3)
        {
            errors.Add("Currency must be 3 characters.");
        }

        if (!AllowedCurrencies.Contains(currency))
        {
            errors.Add($"Currency must be one of: {string.Join(", ", AllowedCurrencies.OrderBy(c => c, StringComparer.Ordinal))}.");
        }
    }

    private static void ValidateAmount(int amount, List<string> errors)
    {
        if (amount <= 0)
        {
            errors.Add("Amount must be a positive integer.");
        }
    }

    private static void ValidateCvv(string? cvv, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(cvv))
        {
            errors.Add("CVV is required.");
            return;
        }

        if (cvv.Length is < 3 or > 4)
        {
            errors.Add("CVV must be 3 or 4 characters.");
        }

        if (!DigitsOnly.IsMatch(cvv))
        {
            errors.Add("CVV must contain only numeric characters.");
        }
    }
}
