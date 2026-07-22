using System;
using System.Net;
using System.Text.Json;
using API.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace API.Middleware;

public class ExceptionMiddleware(IHostEnvironment env, RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An unhandled exception occurred during the request.");
            await HandleExceptionAsync(context, e, env);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception e, IHostEnvironment env)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = env.IsDevelopment()
            ? new ApiErrorResponse(context.Response.StatusCode, e.Message, e.StackTrace)
            : new ApiErrorResponse(context.Response.StatusCode, "Internal Server error", "Internal Server error");

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}
