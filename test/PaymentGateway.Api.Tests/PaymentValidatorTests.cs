using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Helpers;

namespace PaymentGateway.Api.Tests;

public class PaymentValidatorTests
{
    private readonly DateTime _fixedUtcNow = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
    private readonly PaymentValidator _validator;

    public PaymentValidatorTests()
    {
        _validator = new PaymentValidator(() => _fixedUtcNow);
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingCardNumber_ReturnsError(string? cardNumber)
    {
        var request = ValidRequest();
        request.CardNumber = cardNumber!;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Card number", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("1234567890123")] // 13 digits
    [InlineData("12345678901234567890")] // 20 digits
    public void Validate_CardNumberWrongLength_ReturnsError(string cardNumber)
    {
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("between 14 and 19", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_CardNumberNonNumeric_ReturnsError()
    {
        var request = ValidRequest();
        request.CardNumber = "22224053432488ab";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("numeric", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_ExpiryMonthOutOfRange_ReturnsError(int month)
    {
        var request = ValidRequest();
        request.ExpiryMonth = month;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Expiry month", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExpiredCard_ReturnsError()
    {
        var request = ValidRequest();
        request.ExpiryMonth = 10;
        request.ExpiryYear = 2026;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("future", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExpiryInCurrentMonth_ReturnsSuccess()
    {
        var request = ValidRequest();
        request.ExpiryMonth = 11;
        request.ExpiryYear = 2026;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("GB")]
    [InlineData("GBPP")]
    [InlineData("JPY")]
    public void Validate_InvalidCurrency_ReturnsError(string? currency)
    {
        var request = ValidRequest();
        request.Currency = currency!;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Currency", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAmount_ReturnsError(int amount)
    {
        var request = ValidRequest();
        request.Amount = amount;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Amount", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12a")]
    public void Validate_InvalidCvv_ReturnsError(string? cvv)
    {
        var request = ValidRequest();
        request.Cvv = cvv!;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CVV", StringComparison.OrdinalIgnoreCase));
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
