using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PaymentValidator _paymentValidator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentsRepository paymentsRepository,
        PaymentValidator paymentValidator,
        ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _paymentValidator = paymentValidator;
        _logger = logger;
    }
    
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        return new OkObjectResult(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse?>> PostPaymentAsync([FromBody] PostPaymentRequest request)
    {
        var validation = _paymentValidator.Validate(request);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "Payment rejected. CardLastFour: {CardLastFour}, Errors: {Errors}",
                PaymentResponseHelper.GetLastFourDigits(request.CardNumber),
                validation.Errors);

            return BadRequest(PaymentResponseHelper.BuildRejected(request));
        }
        
        // payment service
        return Ok(new PostPaymentResponse());
    }
}