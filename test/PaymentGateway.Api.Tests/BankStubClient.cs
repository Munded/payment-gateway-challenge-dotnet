using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class BankStubClient : IBankClient
{
    public ServiceCall<BankPaymentResponse> Result { get; set; } = ServiceCall<BankPaymentResponse>.Failure();
    public BankPaymentRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }
    

    public Task<ServiceCall<BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request)
    {
        CallCount++;
        LastRequest = request;
        return Task.FromResult(Result);
    }
}