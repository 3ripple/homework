using Microsoft.EntityFrameworkCore;
using UrlShort.Caching;
using UrlShort.Data;
using UrlShort.Endpoints;
using UrlShort.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DdConnection")));
builder.Services.AddSingleton(new LruCache<string, string>(capacity: 100, ttl: TimeSpan.FromMinutes(5)));
builder.Services.AddScoped<ILinkShortenerService, LinkShortenerService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseLinksEndpoints();
app.Run();