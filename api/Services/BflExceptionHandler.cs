using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BflVirtualTryOn.Services;

/// <summary>
/// Translates client and upstream-BFL exceptions into RFC 7807 problem responses.
/// </summary>
public sealed class BflExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<BflExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ArgumentException argEx:
                problem = new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = argEx.Message,
                    Status = StatusCodes.Status400BadRequest,
                };
                break;

            case BflVtoException bflEx:
                logger.LogWarning(bflEx,
                    "BFL VTO error. HttpStatus={HttpStatus} JobStatus={JobStatus} Body={Body}",
                    bflEx.HttpStatusCode, bflEx.JobStatus, bflEx.ResponseBody);

                problem = BuildBflProblem(bflEx);
                break;

            default:
                // Not ours to handle — let the default pipeline deal with it.
                return false;
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails BuildBflProblem(BflVtoException ex)
    {
        // Map terminal job statuses and upstream HTTP failures to meaningful codes.
        var (status, title) = ex switch
        {
            { JobStatus: "Task not found" } => (StatusCodes.Status404NotFound, "Task not found"),
            { JobStatus: "Request Moderated" or "Content Moderated" }
                => (StatusCodes.Status422UnprocessableEntity, "Content moderated"),
            { JobStatus: not null } => (StatusCodes.Status502BadGateway, "Try-on job failed"),
            { HttpStatusCode: System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden }
                => (StatusCodes.Status502BadGateway, "BFL authentication failed — check the API key"),
            { HttpStatusCode: not null } when ex.HttpStatusCode is var http
                && (int)http! == 408 => (StatusCodes.Status504GatewayTimeout, "Upstream timeout"),
            { HttpStatusCode: null, JobStatus: null } => (StatusCodes.Status504GatewayTimeout, "Try-on timed out"),
            _ => (StatusCodes.Status502BadGateway, "BFL request failed"),
        };

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = ex.Message,
            Status = status,
        };

        if (ex.JobStatus is not null)
        {
            problem.Extensions["jobStatus"] = ex.JobStatus;
        }
        if (ex.HttpStatusCode is not null)
        {
            problem.Extensions["upstreamStatus"] = (int)ex.HttpStatusCode.Value;
        }

        return problem;
    }
}
