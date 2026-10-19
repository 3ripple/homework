using Microsoft.AspNetCore.Mvc;
using UrlShort.Models;
using UrlShort.Services;

namespace UrlShort.Endpoints;

public static class LinksEndpoints
{
    public static IEndpointRouteBuilder UseLinksEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/links", async (
                CreateShortLinkRequest request,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                ILinkShortenerService service,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.OriginalUrl))
                    return Results.BadRequest("OriginalUrl is required");

                var result = await service.CreateShortLinkAsync(
                    request.OriginalUrl, idempotencyKey, ct);

                return Results.Created(result.ShortUrl, result);
            })
            .WithName("CreateShortLink");
        app.MapGet("/{code}", async (
                string code,
                ILinkShortenerService service,
                CancellationToken ct) =>
            {
                var url = await service.GetOriginalUrlAsync(code, ct);

                return url is null ? Results.NotFound() : Results.Redirect(url);
            })
            .WithName("GetOriginalUrl");
        app.MapGet("/api/links/{code}", async (
                string code,
                ILinkShortenerService service,
                CancellationToken ct) =>
            {
                var statistics = await service.GetStatisticsAsync(code, ct);

                return statistics is null ? Results.NotFound() : Results.Ok(statistics);
            })
            .WithName("GetLinkStatistics");
        app.MapDelete("/api/links/{code}", async (
                string code,
                ILinkShortenerService service,
                CancellationToken ct) =>
            {
                var deleted = await service.DeleteLinkAsync(code, ct);

                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteLink");
        return app;
    }
}