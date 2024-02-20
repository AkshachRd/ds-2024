using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        
        string similarityKey = "SIMILARITY-" + id;
        RedisDatabase.StringSet(similarityKey, GetSimilarity(text, id));
        
        string textKey = "TEXT-" + id;
        RedisDatabase.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        RedisDatabase.StringSet(rankKey, GetRank(text));

        return Redirect($"summary?id={id}");
    }
    
    private int GetSimilarity(string text, string id)
    {
        var keys = RedisServer.Keys();
        
        return keys.Any(key => 
            key.ToString().Substring(0, 5) == "TEXT-" && RedisDatabase.StringGet(key) == text) ? 1 : 0;
    }

    private static double GetRank(string text)
    {
        var notLetterCount = text.Count(ch => !char.IsLetter(ch));

        return 1.0 - (double) notLetterCount / text.Length;
    }
}
