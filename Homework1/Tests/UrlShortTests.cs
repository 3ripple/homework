using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShort.Caching;
using UrlShort.Data;
using UrlShort.Services;

namespace Tests;

public class UrlShortTests
{
    private static async Task<AppDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbContext = new AppDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }
    
    private static LruCache<string, string> CreateCache() => new(100, TimeSpan.FromMinutes(5), TimeProvider.System);
    

    [Fact]
    public async Task CreateShortLink_GeneratesValidCode()
    {
        await using var db = await CreateDbContextAsync();
        var service = new LinkShortenerService(db, CreateCache());
        var result = await service
            .CreateShortLinkAsync("https://www.yandex.com", null, CancellationToken.None);
        Assert.Matches("^[0-9a-zA-Z]{7}$", result.Code);
        var exist = await db.Links.AnyAsync(x => x.Code == result.Code, CancellationToken.None);
        Assert.True(exist);
    }
    
    [Fact]
    public async Task CreateShortLink_SameIdempotencyKey_ReturnsSameResult()
    {
        await using var db = await CreateDbContextAsync();
        var service = new LinkShortenerService(db, CreateCache());
        var resultFirst = await service
            .CreateShortLinkAsync("https://www.yandex.com","idempotency-key", CancellationToken.None);
        var resultSecond = await service
            .CreateShortLinkAsync("https://www.yandex.com","idempotency-key", CancellationToken.None);
        Assert.Equal(resultFirst.Code, resultSecond.Code);
    }
    
    [Fact]
    public async Task GetOriginalUrl_UnknownCode_ReturnsNull()
    {
        await using var db = await CreateDbContextAsync();
        var service = new LinkShortenerService(db, CreateCache());
        var result = await service
            .GetOriginalUrlAsync("unknown", CancellationToken.None);
        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetOriginalUrl_IncrementsClicks()
    {
        await using var db = await CreateDbContextAsync();
        var service = new LinkShortenerService(db, CreateCache());
        var result = await service
            .CreateShortLinkAsync("https://www.yandex.com", null, CancellationToken.None);
        await service.GetOriginalUrlAsync(result.Code, CancellationToken.None);
        await service.GetOriginalUrlAsync(result.Code, CancellationToken.None);
        var clicks = await db.Links.Where(x => x.Code == result.Code).Select(x => x.Clicks).FirstOrDefaultAsync(CancellationToken.None);
        Assert.Equal(2, clicks);
    }
    
    [Fact]
    public async Task GetOriginalUrl_CacheHit_ReturnsCachedUrl()
    {
        await using var db = await CreateDbContextAsync();
        var cache = CreateCache();
        const string cacheUrl = "https://www.cache.com";
        var service = new LinkShortenerService(db, cache);
        var created = await service
            .CreateShortLinkAsync("https://www.yandex.com", null, CancellationToken.None);
        cache.Set(created.Code, cacheUrl);
        var url = await service.GetOriginalUrlAsync(created.Code, CancellationToken.None);
        Assert.Equal(cacheUrl, url);
    }
}