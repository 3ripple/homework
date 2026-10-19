using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShort.Caching;
using UrlShort.Data;
using UrlShort.Exceptions;
using UrlShort.Models;

namespace UrlShort.Services;

public class LinkShortenerService(AppDbContext dbContext, LruCache<string, string> cache) : ILinkShortenerService
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CodeLength = 7;
    private const int MaxAttempts = 10;

    public async Task<CreateShortLinkResponse> CreateShortLinkAsync(string originalUrl, string? idempotencyKey, CancellationToken ct)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid url: {originalUrl}");
        }
        var baseUrl = "https://www.example.com"; //TODO вынести
        if (idempotencyKey is not null)
        {
            var link = await dbContext.Links
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdempotencyKey == idempotencyKey, ct);
            if (link is not null)
            {
                if (link.OriginalUrl != originalUrl)
                    throw new IdempotencyConflictException();
                var shortUrl = new Uri(new Uri(baseUrl), link.Code);
                return new CreateShortLinkResponse(
                    link.Code,
                    shortUrl.ToString());
            }
        }
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = GenerateCode();
            var link = new Link
            {
                Id = Guid.CreateVersion7(),
                Code = code,
                OriginalUrl = originalUrl,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow,
                Clicks = 0
            };
            await dbContext.Links.AddAsync(link, ct);
            try
            {
                await dbContext.SaveChangesAsync(ct);
                var shortUrl = new Uri(new Uri(baseUrl), code);
                return new CreateShortLinkResponse(
                    code,
                    shortUrl.ToString());
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException
                                               {
                                                   SqliteExtendedErrorCode: 2067
                                               })
            {
                dbContext.Entry(link).State = EntityState.Detached;
            }
        }
        throw new InvalidOperationException("Unable to create short link");
    }

    public async Task<string?> GetOriginalUrlAsync(string code, CancellationToken ct)
    {
        if (!cache.TryGet(code, out var originalUrl))
        {
            originalUrl = await dbContext.Links
                .AsNoTracking()
                .Where(l => l.Code == code)
                .Select(l => l.OriginalUrl)
                .FirstOrDefaultAsync(ct);
            if (originalUrl is null)
                return null;
            cache.Set(code, originalUrl);
        }
        await dbContext.Links
            .Where(l => l.Code == code)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.Clicks, x => x.Clicks + 1), ct);
        return originalUrl;
    }
    
    public async Task<LinkStatisticsResponse?> GetStatisticsAsync(string code, CancellationToken ct)
    {
        return await dbContext.Links
            .AsNoTracking()
            .Where(l => l.Code == code)
            .Select(l => new LinkStatisticsResponse(l.OriginalUrl, l.Code, l.Clicks, l.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
    
    public async Task<bool> DeleteLinkAsync(string code, CancellationToken ct)
    {
        var deletedCount = await dbContext.Links
            .Where(l => l.Code == code)
            .ExecuteDeleteAsync(ct);
        if (deletedCount == 0)
            return false;
        cache.Remove(code);
        return true;
    }
    private static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            var idx = RandomNumberGenerator.GetInt32(Alphabet.Length);
            chars[i] = Alphabet[idx];
        }
        return new string(chars);
    }
}