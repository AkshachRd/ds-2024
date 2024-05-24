using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class RedisPageModel(ConnectionMultiplexer redis) : PageModel
{
    public IDatabase RedisDatabase => redis.GetDatabase();
    public IServer RedisServer => redis.GetServer(redis.GetEndPoints()[0]);
}