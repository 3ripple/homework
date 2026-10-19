using UrlShort.Models;

namespace UrlShort.Services;

public interface ILinkShortenerService
{
    Task<CreateShortLinkResponse> CreateShortLinkAsync(string originalUrl, string? idempotencyKey, CancellationToken ct);
    Task<string?> GetOriginalUrlAsync(string code, CancellationToken ct);
    Task<LinkStatisticsResponse?> GetStatisticsAsync(string code, CancellationToken ct);
    Task<bool> DeleteLinkAsync(string code, CancellationToken ct);
}