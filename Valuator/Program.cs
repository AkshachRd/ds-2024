using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        
        var redisHostAndPort = Environment.GetEnvironmentVariable("DB_RUS") ?? "localhost:6379";
        var redis = ConnectionMultiplexer.Connect(redisHostAndPort);
        
        builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
