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
        
        string textKey = "TEXT-" + id;
        RedisDatabase.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        RedisDatabase.StringSet(rankKey, "1");

        string similarityKey = "SIMILARITY-" + id;
        RedisDatabase.StringSet(similarityKey, "2");

        return Redirect($"summary?id={id}");
    }
}
