using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class BankClient : IBankClient
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
                LogError(request);

                return new ServiceCall<BankPaymentResponse>() { IsSuccess = false };
            }
            
            var processedResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();

            if (processedResponse is null)
            {
                LogError(request);

                return new ServiceCall<BankPaymentResponse>() { IsSuccess = false };
            }
            
            return ServiceCall<BankPaymentResponse>.Success(processedResponse);
        }
        catch (Exception e)
        {
            LogError(request);
            throw;
        }
    }

    private void LogError(BankPaymentRequest request)
    {
        _logger.LogError($"An error occured while processing payment for card {PaymentResponseHelper.GetLastFourDigits(request.CardNumber)}");
    }
}