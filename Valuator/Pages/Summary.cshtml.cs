using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    
    private ConnectionMultiplexer _redis;
    private IDatabase RedisDatabase => _redis.GetDatabase();
    private IServer RedisServer => _redis.GetServer(_redis.GetEndPoints()[0]);

    public SummaryModel(ILogger<SummaryModel> logger)
    {
        _logger = logger;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id, string region)
    {
        _logger.LogDebug(id);
        _redis = ConnectToRedis(region);

        string rankKey = "RANK-" + id;
        Rank = ((IConvertible)RedisDatabase.StringGet(rankKey)).ToDouble(null);

        string similarityKey = "SIMILARITY-" + id;
        Similarity = ((IConvertible)RedisDatabase.StringGet(similarityKey)).ToDouble(null);
    }
    
    private static ConnectionMultiplexer ConnectToRedis(string region)
    {
        var hostAndPort = Environment.GetEnvironmentVariable("DB_" + region.ToUpper()) ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(hostAndPort);
    }
}
