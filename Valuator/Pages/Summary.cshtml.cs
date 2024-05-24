using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : RedisPageModel
{
    private readonly ILogger<SummaryModel> _logger;

    public SummaryModel(ILogger<SummaryModel> logger, ConnectionMultiplexer redis) : base(redis)
    {
        _logger = logger;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        string rankKey = "RANK-" + id;
        Rank = ((IConvertible)RedisDatabase.StringGet(rankKey)).ToDouble(null);

        string similarityKey = "SIMILARITY-" + id;
        Similarity = ((IConvertible)RedisDatabase.StringGet(similarityKey)).ToDouble(null);
    }
}
