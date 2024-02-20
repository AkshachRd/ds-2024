using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class RedisPageModel(ConnectionMultiplexer redis) : PageModel
{
    private string _hostName = "redis";
    private int _port = 6379;
    public IDatabase RedisDatabase => redis.GetDatabase();
    public IServer RedisServer => redis.GetServer(_hostName, _port);
}