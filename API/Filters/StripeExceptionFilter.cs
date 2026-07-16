using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Stripe;

namespace API.Filters;

public class StripeExceptionFilter : IExceptionFilter
{
    private readonly ILogger<StripeExceptionFilter> _logger;

    public StripeExceptionFilter(ILogger<StripeExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is StripeException stripeEx)
        {
            _logger.LogError(stripeEx, "Stripe Webhook error: {Message}", stripeEx.Message);

            context.Result = new BadRequestObjectResult(new { message = "Stripe Webhook error" });
            context.ExceptionHandled = true;
        }
    }
}
