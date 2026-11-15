using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class BankClient : IBankClient
{
    private const int MaxAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly ILogger<BankClient> _logger;

    public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ServiceCall<BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/payments", request);

                if (!response.IsSuccessStatusCode)
                {
                    LogError(request, attempt);
                    if (attempt == MaxAttempts)
                    {
                        return ServiceCall<BankPaymentResponse>.Failure();
                    }

                    continue;
                }

                var processedResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();

                if (processedResponse is null)
                {
                    LogError(request, attempt);
                    if (attempt == MaxAttempts)
                    {
                        return ServiceCall<BankPaymentResponse>.Failure();
                    }

                    continue;
                }

                return ServiceCall<BankPaymentResponse>.Success(processedResponse);
            }
            catch (Exception)
            {
                LogError(request, attempt);
                if (attempt == MaxAttempts)
                {
                    throw;
                }
            }
        }

        return ServiceCall<BankPaymentResponse>.Failure();
    }

    private void LogError(BankPaymentRequest request, int attempt)
    {
        _logger.LogError(
            "An error occured while processing payment for card {CardLastFour} (attempt {Attempt}/{MaxAttempts})",
            PaymentResponseHelper.GetLastFourDigits(request.CardNumber),
            attempt,
            MaxAttempts);
    }
}
