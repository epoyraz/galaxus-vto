using System.Net.Http.Json;
using System.Text.Json;
using BflVirtualTryOn.Configuration;
using BflVirtualTryOn.Models;
using BflVirtualTryOn.Models.Bfl;
using Microsoft.Extensions.Options;

namespace BflVirtualTryOn.Services;

/// <summary>Typed <see cref="HttpClient"/> implementation of <see cref="IBflVtoClient"/>.</summary>
public sealed class BflVtoClient : IBflVtoClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly BflOptions _options;
    private readonly ILogger<BflVtoClient> _logger;

    public BflVtoClient(HttpClient http, IOptions<BflOptions> options, ILogger<BflVtoClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BflSubmitResponse> SubmitAsync(BflSubmitPayload payload, CancellationToken ct)
    {
        var url = $"{BaseUrl}{_options.SubmitPath}";

        _logger.LogInformation("Submitting VTO job to {Url}", url);

        using var response = await _http.PostAsJsonAsync(url, payload, JsonOptions, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new BflVtoException(
                $"BFL submit failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.",
                httpStatusCode: response.StatusCode,
                responseBody: body);
        }

        var result = Deserialize<BflSubmitResponse>(body)
            ?? throw new BflVtoException("BFL submit returned an empty body.", responseBody: body);

        if (string.IsNullOrWhiteSpace(result.Id))
        {
            throw new BflVtoException("BFL submit response did not include a task id.", responseBody: body);
        }

        // BFL normally returns a region-matched polling_url; fall back to constructing one.
        if (string.IsNullOrWhiteSpace(result.PollingUrl))
        {
            result.PollingUrl = BuildResultUrl(result.Id);
        }

        return result;
    }

    public async Task<BflPollResponse> GetResultAsync(string pollingUrl, CancellationToken ct)
    {
        using var response = await _http.GetAsync(pollingUrl, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new BflVtoException(
                $"BFL poll failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.",
                httpStatusCode: response.StatusCode,
                responseBody: body);
        }

        return Deserialize<BflPollResponse>(body)
            ?? throw new BflVtoException("BFL poll returned an empty body.", responseBody: body);
    }

    public async Task<BflPollResponse> WaitForResultAsync(string pollingUrl, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.PollTimeoutSeconds));
        var pollToken = timeoutCts.Token;

        var delay = TimeSpan.FromMilliseconds(Math.Max(100, _options.PollIntervalMs));

        try
        {
            while (true)
            {
                var poll = await GetResultAsync(pollingUrl, pollToken);

                if (VtoStatus.IsReady(poll.Status))
                {
                    return poll;
                }

                if (VtoStatus.IsFailure(poll.Status))
                {
                    var details = poll.Details?.ToString();
                    throw new BflVtoException(
                        $"VTO job ended with terminal status '{poll.Status}'.",
                        jobStatus: poll.Status,
                        responseBody: details);
                }

                await Task.Delay(delay, pollToken);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // The linked token fired due to our timeout, not the caller's cancellation.
            throw new BflVtoException(
                $"Timed out waiting for VTO result after {_options.PollTimeoutSeconds}s. " +
                $"The job may still complete — poll {pollingUrl} to retrieve it.");
        }
    }

    public string BuildResultUrl(string taskId) =>
        $"{BaseUrl}{_options.GetResultPath}?id={Uri.EscapeDataString(taskId)}";

    private string BaseUrl => _options.BaseUrl.TrimEnd('/');

    private static T? Deserialize<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new BflVtoException("Failed to parse the BFL API response.", responseBody: body, inner: ex);
        }
    }
}
