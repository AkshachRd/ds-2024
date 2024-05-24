using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    private readonly IConnection _connection = ConnectToNats();

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


        GetRankAsync(id).Wait();

        return Redirect($"summary?id={id}");
    }
    
    private static IConnection ConnectToNats()
    {
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");
            
        ConnectionFactory factory = new ConnectionFactory();

        var options = ConnectionFactory.GetDefaultOptions();
        options.Url = natsUrl;
            
        return factory.CreateConnection(options);
    }

    private Task<double> GetRankAsync(string id)
    {
        var tcs = new TaskCompletionSource<double>();

        var subscription = _connection.SubscribeAsync("rank.calculated");
        subscription.MessageHandler += (sender, args) =>
        {
            try
            {
                var result = BitConverter.ToDouble(args.Message.Data);
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(new InvalidOperationException("Невозможно преобразовать ответ в число."));
            }
            
            subscription.Unsubscribe();
        };
        subscription.Start();
    
        _connection.Publish("rank.calculate", Encoding.UTF8.GetBytes(id));
    
        return tcs.Task;
    }
    
    private int GetSimilarity(string text, string id)
    {
        var keys = RedisServer.Keys();
        var similarity = keys.Any(key => 
            key.ToString().Substring(0, 5) == "TEXT-" && RedisDatabase.StringGet(key) == text) ? 1 : 0;
        
        _connection.Publish("similarity.calculated", BitConverter.GetBytes(similarity));

        return similarity;
    }
}
