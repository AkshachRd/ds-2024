using System;
using System.Linq;
using System.Text;
using NATS.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : RedisPageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger, ConnectionMultiplexer redis) : base(redis)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return Redirect("about");
        }
        
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        
        string similarityKey = "SIMILARITY-" + id;
        RedisDatabase.StringSet(similarityKey, GetSimilarity(text, id));
        
        string textKey = "TEXT-" + id;
        RedisDatabase.StringSet(textKey, text);

        CalculateRank(id).Wait();

        return Redirect($"summary?id={id}");
    }
    
    private int GetSimilarity(string text, string id)
    {
        var keys = RedisServer.Keys();
        
        return keys.Any(key => 
            key.ToString().Substring(0, 5) == "TEXT-" && RedisDatabase.StringGet(key) == text) ? 1 : 0;
    }
    
    private static IConnection ConnectToNats()
    {
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");
            
        ConnectionFactory factory = new ConnectionFactory();

        var options = ConnectionFactory.GetDefaultOptions();
        options.Url = natsUrl;
            
        return factory.CreateConnection(options);
    }

    private static Task CalculateRank(string id)
    {
        using (var c = ConnectToNats())
        {
            var data = Encoding.UTF8.GetBytes(id);
            var task = c.RequestAsync("valuator.processing.rank", data);

            task.Wait();
            
            c.Drain();
            
            return task;
        }
    }
}
