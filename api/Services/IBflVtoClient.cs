using BflVirtualTryOn.Models.Bfl;

namespace BflVirtualTryOn.Services;

/// <summary>Client for the BFL FLUX Virtual Try-On asynchronous workflow (pinned to the EU region).</summary>
public interface IBflVtoClient
{
    /// <summary>Submits a try-on job and returns the task id + polling URL.</summary>
    Task<BflSubmitResponse> SubmitAsync(BflSubmitPayload payload, CancellationToken ct);

    /// <summary>Polls a result URL once and returns the current status.</summary>
    Task<BflPollResponse> GetResultAsync(string pollingUrl, CancellationToken ct);

    /// <summary>
    /// Polls until the job reaches <c>Ready</c> (returns it) or a terminal failure (throws
    /// <see cref="BflVtoException"/>), respecting the configured poll interval and timeout.
    /// </summary>
    Task<BflPollResponse> WaitForResultAsync(string pollingUrl, CancellationToken ct);

    /// <summary>Builds the EU <c>get_result</c> URL for a task id.</summary>
    string BuildResultUrl(string taskId);
}
