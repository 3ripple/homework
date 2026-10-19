namespace UrlShort.Models;

public record LinkStatisticsResponse(string OriginalUrl, string Code, int Clicks, DateTime CreatedAt);