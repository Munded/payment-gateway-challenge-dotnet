namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    // if this wasnt the case, i'm sorry but this should be Card number in the request?
    // public int CardNumberLastFour { get; set; }
    public int CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; }
    public int Amount { get; set; }
    public int Cvv { get; set; }
}