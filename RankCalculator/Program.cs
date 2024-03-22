using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;
using StackExchange.Redis;

namespace RankCalculator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Producer started");

            CalculateRankAsync();
        }
        
        private static IConnection ConnectToNats()
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");
            
            ConnectionFactory factory = new ConnectionFactory();

            var options = ConnectionFactory.GetDefaultOptions();
            options.Url = natsUrl;
            
            return factory.CreateConnection(options);
        }
        
        private static IDatabase ConnectToRedis()
        {
            var hostAndPort = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
            var redis = ConnectionMultiplexer.Connect(hostAndPort);
            return redis.GetDatabase();
        }

        private static void CalculateRankAsync()
        {
            var redisDb = ConnectToRedis();
            
            using (var c = ConnectToNats())
            {
                var s = c.SubscribeAsync("valuator.processing.rank", "rank_calculator", (sender, args) =>
                {
                    var id = Encoding.UTF8.GetString(args.Message.Data);
                    var textKey = "TEXT-" + id;
                    string text = redisDb.StringGet(textKey);
                    
                    var notLetterCount = text.Count(ch => !char.IsLetter(ch));
                    var rank = (double)notLetterCount / text.Length;
            
                    var rankKey = "RANK-" + id;
                    redisDb.StringSet(rankKey, rank);

                    c.Publish(args.Message.Reply, null);
                });
                
                while (true)
                {
                    Console.WriteLine("Worker listening...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
    }
}