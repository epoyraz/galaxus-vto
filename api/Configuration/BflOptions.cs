namespace BflVirtualTryOn.Configuration;

/// <summary>
/// Configuration for the Black Forest Labs (BFL) FLUX Virtual Try-On API.
/// Bound from the "Bfl" configuration section.
/// </summary>
public sealed class BflOptions
{
    public const string SectionName = "Bfl";

    /// <summary>
    /// BFL API key, sent as the <c>x-key</c> header.
    /// Prefer setting this via user-secrets (<c>dotnet user-secrets set "Bfl:ApiKey" ...</c>)
    /// or the <c>BFL_API_KEY</c> environment variable rather than committing it.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API host. Pinned to the EU region for lower latency. The submit response's
    /// <c>polling_url</c> is always honored as-is for the result, so it may point at a
    /// region sub-host (e.g. api.eu2.bfl.ai).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.eu.bfl.ai";

    /// <summary>Path of the VTO submit endpoint, appended to the base URL.</summary>
    public string SubmitPath { get; set; } = "/v1/flux-tools/vto-v1";

    /// <summary>Path of the result polling endpoint (the <c>id</c> query param is appended).</summary>
    public string GetResultPath { get; set; } = "/v1/get_result";

    /// <summary>Delay between poll attempts in the synchronous "submit-and-wait" flow.</summary>
    public int PollIntervalMs { get; set; } = 1000;

    /// <summary>Maximum time to wait for a result in the synchronous flow before giving up.</summary>
    public int PollTimeoutSeconds { get; set; } = 120;

    /// <summary>HTTP request timeout for individual calls to the BFL API.</summary>
    public int RequestTimeoutSeconds { get; set; } = 100;
}
