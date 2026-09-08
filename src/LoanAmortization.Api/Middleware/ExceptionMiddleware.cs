using System.Text.Json;
using FluentValidation;
using LoanAmortization.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace LoanAmortization.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, 400, "Validation Failed",
                string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (EmailAlreadyExistsException ex)
        {
            await WriteProblemAsync(context, 409, "Conflict", ex.Message);
        }
        catch (InvalidCredentialsException ex)
        {
            await WriteProblemAsync(context, 401, "Unauthorized", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, 500, "Internal Server Error", "Ocurrió un error inesperado.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem, JsonOpts);
    }
}
