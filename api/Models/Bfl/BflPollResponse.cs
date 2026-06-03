using System.Text.Json;
using System.Text.Json.Serialization;

namespace BflVirtualTryOn.Models.Bfl;

/// <summary>Response returned when polling the BFL <c>get_result</c> endpoint.</summary>
public sealed class BflPollResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Job status, e.g. <c>Pending</c>, <c>Ready</c>, <c>Error</c>, <c>Request Moderated</c>,
    /// <c>Content Moderated</c>, <c>Task not found</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("result")]
    public BflResult? Result { get; set; }

    /// <summary>Free-form details bag BFL may include (e.g. moderation reasons). Kept raw.</summary>
    [JsonPropertyName("details")]
    public JsonElement? Details { get; set; }
}

/// <summary>The <c>result</c> object present once a job reaches <c>Ready</c>.</summary>
public sealed class BflResult
{
    /// <summary>Signed delivery URL of the generated image. Valid for ~10 minutes.</summary>
    [JsonPropertyName("sample")]
    public string? Sample { get; set; }

    [JsonPropertyName("seed")]
    public long? Seed { get; set; }
}
