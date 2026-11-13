using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class BankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<ServiceCall<BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/payments", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"TODO better message - An error occured while processing {request}");

                return new ServiceCall<BankPaymentResponse>() { IsSuccess = false };
            }
            
            var processedResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();

            if (processedResponse is null)
            {
                _logger.LogError($"TODO better messageAn error occured while processing {request}");

                return new ServiceCall<BankPaymentResponse>() { IsSuccess = false };
            }
            
            return ServiceCall<BankPaymentResponse>.Success(processedResponse);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}