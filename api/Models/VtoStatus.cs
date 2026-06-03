namespace BflVirtualTryOn.Models;

/// <summary>Helpers for interpreting the status strings returned by the BFL API.</summary>
public static class VtoStatus
{
    public const string Ready = "Ready";

    /// <summary>Terminal failure statuses returned by BFL.</summary>
    private static readonly HashSet<string> FailureStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Error",
        "Request Moderated",
        "Content Moderated",
        "Task not found",
    };

    public static bool IsReady(string? status) =>
        string.Equals(status, Ready, StringComparison.OrdinalIgnoreCase);

    public static bool IsFailure(string? status) =>
        status is not null && FailureStatuses.Contains(status);

    /// <summary>True while the job is still queued/processing (not yet terminal).</summary>
    public static bool IsPending(string? status) =>
        !IsReady(status) && !IsFailure(status);
}

/// <summary>Default prompt used when a request omits one.</summary>
public static class VtoDefaults
{
    public const string Prompt =
        "The person of image 1, maintaining exactly their face and pose, wearing the garments of image 2.";
}
