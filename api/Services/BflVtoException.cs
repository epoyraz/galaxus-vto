using System.Net;

namespace BflVirtualTryOn.Services;

/// <summary>
/// Raised when the BFL API returns an error, a request times out, or a job reaches a
/// terminal failure status (Error / moderation / not found).
/// </summary>
public sealed class BflVtoException : Exception
{
    /// <summary>HTTP status from the BFL call, if the failure was an HTTP error.</summary>
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>Job status from a poll response, if the failure was a terminal job status.</summary>
    public string? JobStatus { get; }

    /// <summary>Raw response body, when available, for diagnostics.</summary>
    public string? ResponseBody { get; }

    public BflVtoException(
        string message,
        HttpStatusCode? httpStatusCode = null,
        string? jobStatus = null,
        string? responseBody = null,
        Exception? inner = null)
        : base(message, inner)
    {
        HttpStatusCode = httpStatusCode;
        JobStatus = jobStatus;
        ResponseBody = responseBody;
    }
}
