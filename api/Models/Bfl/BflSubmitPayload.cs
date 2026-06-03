using System.Text.Json.Serialization;

namespace BflVirtualTryOn.Models.Bfl;

/// <summary>
/// Body POSTed to the BFL VTO submit endpoint. Property names are serialized as
/// snake_case (configured globally on the serializer), matching the BFL API contract.
/// Null values are omitted so optional fields fall back to BFL defaults.
/// </summary>
public sealed class BflSubmitPayload
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    /// <summary>Person image — URL or base64. Mapped to <c>input_image</c> by BFL.</summary>
    [JsonPropertyName("person")]
    public required string Person { get; set; }

    /// <summary>Garment reference — URL or base64. Mapped to <c>input_image_2</c> by BFL.</summary>
    [JsonPropertyName("garment")]
    public required string Garment { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("safety_tolerance")]
    public int? SafetyTolerance { get; set; }

    [JsonPropertyName("output_format")]
    public string? OutputFormat { get; set; }

    [JsonPropertyName("webhook_url")]
    public string? WebhookUrl { get; set; }

    [JsonPropertyName("webhook_secret")]
    public string? WebhookSecret { get; set; }
}
