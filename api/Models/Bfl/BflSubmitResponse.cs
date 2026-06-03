using System.Text.Json.Serialization;

namespace BflVirtualTryOn.Models.Bfl;

/// <summary>Initial response returned by the BFL submit endpoint.</summary>
public sealed class BflSubmitResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Region-specific URL to poll for the result. Always poll this exact URL rather than
    /// rewriting the host, per BFL guidance.
    /// </summary>
    [JsonPropertyName("polling_url")]
    public string? PollingUrl { get; set; }
}
