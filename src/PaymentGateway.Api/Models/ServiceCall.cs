namespace PaymentGateway.Api.Models;

public class ServiceCall<T>
{
    
    public bool IsSuccess { get; set; }
    public T? Response { get; set; }
    
    public Exception? Exception { get; set; }
    
    public static ServiceCall<T> Success(T response) => new()
    {
        IsSuccess = true,
        Response = response
    };
    
    public static ServiceCall<T> Failure() => new()
    {
        IsSuccess = false
    };
}