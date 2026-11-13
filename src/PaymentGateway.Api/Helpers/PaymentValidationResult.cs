namespace PaymentGateway.Api.Helpers;

public class PaymentValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();

    public static PaymentValidationResult Success() => new();

    public static PaymentValidationResult Failure(params string[] errors)
    {
        var result = new PaymentValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }
}
