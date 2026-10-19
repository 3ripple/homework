namespace UrlShort.Models;

public class Link
{
    public Guid Id { get; set; }
    
    public string Code {get; set;}
    
    public string OriginalUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public int Clicks { get; set; }
    
    public string? IdempotencyKey { get; set; }
}