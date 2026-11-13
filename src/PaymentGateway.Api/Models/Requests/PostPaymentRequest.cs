namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    // if this wasnt the case, i'm sorry but this should be
    // i'd also suggest it be a string
    // public int CardNumberLastFour { get; set; }
    public string CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
    public string Cvv { get; set; }
}