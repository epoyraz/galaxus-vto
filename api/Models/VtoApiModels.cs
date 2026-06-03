using System.ComponentModel.DataAnnotations;

namespace BflVirtualTryOn.Models;

/// <summary>
/// Request body for submitting a virtual try-on job with image inputs supplied
/// as URLs or base64 strings.
/// </summary>
public sealed class VtoSubmitRequest
{
    /// <summary>
    /// Natural-language styling instruction. If omitted, a sensible default is used:
    /// "<see cref="VtoDefaults.Prompt"/>".
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>Person image as a public URL or a base64-encoded string (no data-URI prefix needed).</summary>
    [Required(AllowEmptyStrings = false)]
    public string Person { get; set; } = string.Empty;

    /// <summary>Garment reference as a public URL or a base64-encoded string.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Garment { get; set; } = string.Empty;

    /// <summary>Optional seed for reproducibility.</summary>
    public int? Seed { get; set; }

    /// <summary>Moderation strictness for input and output, 0–5 (BFL default is 2).</summary>
    [Range(0, 5)]
    public int? SafetyTolerance { get; set; }

    /// <summary>Output image format: <c>jpeg</c> (default), <c>png</c>, or <c>webp</c>.</summary>
    public string? OutputFormat { get; set; }

    /// <summary>Optional async webhook callback URL.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Optional webhook signature secret.</summary>
    public string? WebhookSecret { get; set; }
}

/// <summary>Returned after a job is accepted by BFL (async pattern).</summary>
/// <param name="Id">BFL task id.</param>
/// <param name="PollingUrl">Exact region-specific URL to poll for the result.</param>
/// <param name="StatusUrl">Convenience URL on this API to poll the job by id.</param>
public sealed record VtoSubmitResult(string Id, string PollingUrl, string StatusUrl);

/// <summary>Result of a poll or a synchronous try-on.</summary>
/// <param name="Id">BFL task id.</param>
/// <param name="Status">Current status (e.g. <c>Pending</c>, <c>Ready</c>, <c>Error</c>).</param>
/// <param name="ImageUrl">Signed delivery URL of the generated image when <c>Ready</c> (valid ~10 min).</param>
/// <param name="Seed">Seed used, when reported by BFL.</param>
public sealed record VtoResult(string? Id, string Status, string? ImageUrl, long? Seed);
