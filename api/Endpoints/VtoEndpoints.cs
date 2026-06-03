using BflVirtualTryOn.Models;
using BflVirtualTryOn.Models.Bfl;
using BflVirtualTryOn.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace BflVirtualTryOn.Endpoints;

/// <summary>Maps the virtual try-on HTTP endpoints.</summary>
public static class VtoEndpoints
{
    private const long MaxImageBytes = 20 * 1024 * 1024; // 20 MB per uploaded image.

    public static IEndpointRouteBuilder MapVtoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vto").WithTags("Virtual Try-On");

        // --- Submit (async) -------------------------------------------------
        group.MapPost("/", SubmitAsync)
            .WithName("SubmitVto")
            .WithSummary("Submit a try-on job (images as URL or base64). Returns a task id to poll.");

        // --- Submit via file upload (async, or sync with ?wait=true) --------
        group.MapPost("/upload", SubmitUploadAsync)
            .WithName("SubmitVtoUpload")
            .WithSummary("Submit a try-on job by uploading person + garment image files.")
            .DisableAntiforgery();

        // --- Synchronous submit-and-wait ------------------------------------
        group.MapPost("/try-on", TryOnAsync)
            .WithName("TryOnVto")
            .WithSummary("Submit a try-on job and wait until the result image is ready.");

        // --- Poll a job by id -----------------------------------------------
        group.MapGet("/{id}", GetResultAsync)
            .WithName("GetVtoResult")
            .WithSummary("Poll a previously submitted job by id.");

        return group;
    }

    // ------------------------------------------------------------------ //

    private static async Task<IResult> SubmitAsync(
        VtoSubmitRequest request,
        IBflVtoClient client,
        CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var payload = ToPayload(request);
        var submit = await client.SubmitAsync(payload, ct);
        return Results.Accepted(value: ToSubmitResult(submit));
    }

    private static async Task<IResult> TryOnAsync(
        VtoSubmitRequest request,
        IBflVtoClient client,
        CancellationToken ct)
    {
        if (Validate(request) is { } problem)
        {
            return problem;
        }

        var payload = ToPayload(request);
        var submit = await client.SubmitAsync(payload, ct);
        var poll = await client.WaitForResultAsync(submit.PollingUrl!, ct);
        return Results.Ok(ToResult(poll, submit.Id));
    }

    private static async Task<IResult> SubmitUploadAsync(
        HttpRequest httpRequest,
        IBflVtoClient client,
        IFormFile person,
        IFormFile garment,
        CancellationToken ct,
        [FromForm] string? prompt = null,
        [FromForm] int? seed = null,
        [FromForm] int? safetyTolerance = null,
        [FromForm] string? outputFormat = null,
        [FromForm] bool wait = false)
    {
        if (await ReadImageAsBase64(person, "person", ct) is not { } personB64Result)
        {
            return BadRequest("The 'person' image is required.");
        }
        if (personB64Result.Problem is { } p1)
        {
            return p1;
        }

        if (await ReadImageAsBase64(garment, "garment", ct) is not { } garmentB64Result)
        {
            return BadRequest("The 'garment' image is required.");
        }
        if (garmentB64Result.Problem is { } p2)
        {
            return p2;
        }

        if (safetyTolerance is < 0 or > 5)
        {
            return BadRequest("safetyTolerance must be between 0 and 5.");
        }

        var payload = new BflSubmitPayload
        {
            Prompt = string.IsNullOrWhiteSpace(prompt) ? VtoDefaults.Prompt : prompt,
            Person = personB64Result.Base64!,
            Garment = garmentB64Result.Base64!,
            Seed = seed,
            SafetyTolerance = safetyTolerance,
            OutputFormat = outputFormat,
        };

        var submit = await client.SubmitAsync(payload, ct);

        if (wait)
        {
            var poll = await client.WaitForResultAsync(submit.PollingUrl!, ct);
            return Results.Ok(ToResult(poll, submit.Id));
        }

        return Results.Accepted(value: ToSubmitResult(submit));
    }

    private static async Task<IResult> GetResultAsync(
        string id,
        IBflVtoClient client,
        CancellationToken ct)
    {
        var url = client.BuildResultUrl(id);
        var poll = await client.GetResultAsync(url, ct);
        return Results.Ok(ToResult(poll, id));
    }

    // ------------------------------------------------------------------ //
    // Helpers

    private static ProblemHttpResult? Validate(VtoSubmitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Person))
        {
            return BadRequest("The 'person' field (image URL or base64) is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Garment))
        {
            return BadRequest("The 'garment' field (image URL or base64) is required.");
        }
        if (request.SafetyTolerance is < 0 or > 5)
        {
            return BadRequest("safetyTolerance must be between 0 and 5.");
        }
        return null;
    }

    private static BflSubmitPayload ToPayload(VtoSubmitRequest request) => new()
    {
        Prompt = string.IsNullOrWhiteSpace(request.Prompt) ? VtoDefaults.Prompt : request.Prompt,
        Person = request.Person,
        Garment = request.Garment,
        Seed = request.Seed,
        SafetyTolerance = request.SafetyTolerance,
        OutputFormat = request.OutputFormat,
        WebhookUrl = request.WebhookUrl,
        WebhookSecret = request.WebhookSecret,
    };

    private static VtoSubmitResult ToSubmitResult(BflSubmitResponse submit)
    {
        var statusUrl = $"/api/vto/{Uri.EscapeDataString(submit.Id)}";
        return new VtoSubmitResult(submit.Id, submit.PollingUrl ?? string.Empty, statusUrl);
    }

    private static VtoResult ToResult(BflPollResponse poll, string id) =>
        new(poll.Id ?? id, poll.Status ?? "Unknown", poll.Result?.Sample, poll.Result?.Seed);

    private static ProblemHttpResult BadRequest(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    private readonly record struct ImageReadResult(string? Base64, ProblemHttpResult? Problem);

    private static async Task<ImageReadResult?> ReadImageAsBase64(IFormFile? file, string fieldName, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return null; // caller treats null as "missing".
        }

        if (file.Length > MaxImageBytes)
        {
            return new ImageReadResult(null,
                BadRequest($"The '{fieldName}' image exceeds the {MaxImageBytes / (1024 * 1024)} MB limit."));
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return new ImageReadResult(Convert.ToBase64String(ms.ToArray()), null);
    }
}
