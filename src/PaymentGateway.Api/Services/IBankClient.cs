using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IBankClient
{
    Task<ServiceCall<BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request);
}