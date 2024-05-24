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

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnection _connection = ConnectToNats();

    private ConnectionMultiplexer _redis;
    private IDatabase RedisDatabase => _redis.GetDatabase();
    private IServer RedisServer => _redis.GetServer(_redis.GetEndPoints()[0]);
    
    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text, string region)
    {
        if (String.IsNullOrEmpty(text) || String.IsNullOrEmpty(region))
        {
            return Redirect("about");
        }
        
        string id = Guid.NewGuid().ToString();
        
        _logger.LogInformation("LOOKUP: {id}, {region}", id, region);
        _redis = ConnectToRedis(region);
        
        string similarityKey = "SIMILARITY-" + id;
        RedisDatabase.StringSet(similarityKey, GetSimilarity(text, id));
    
        string textKey = "TEXT-" + id;
        RedisDatabase.StringSet(textKey, text);

        var rankTask = GetRankAsync(text);
        rankTask.Wait();
        
        string rankKey = "RANK-" + id;
        RedisDatabase.StringSet(rankKey, rankTask.Result);
        
        return Redirect($"summary?id={id}&region={region}");
    }
    
    private static IConnection ConnectToNats()
    {
        var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");
            
        ConnectionFactory factory = new ConnectionFactory();

        var options = ConnectionFactory.GetDefaultOptions();
        options.Url = natsUrl;
            
        return factory.CreateConnection(options);
    }
    
    private static ConnectionMultiplexer ConnectToRedis(string region)
    {
        var hostAndPort = Environment.GetEnvironmentVariable("DB_" + region.ToUpper()) ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(hostAndPort);
    }
    
    private Task<double> GetRankAsync(string text)
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

        _connection.Publish("rank.calculate", Encoding.UTF8.GetBytes(text));
    
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
