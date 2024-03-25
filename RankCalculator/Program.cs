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

            using (var c = ConnectToNats())
            {
                var s = c.SubscribeAsync("valuator.processing.rank", "rank_calculator", (sender, args) =>
                {
                    var id = Encoding.UTF8.GetString(args.Message.Data);
                    
                    string text = GetText(id);
                    var rank = CalculateRank(text);
                    SetRank(id, rank);

                    c.Publish(args.Message.Reply, null);
                });
                
                while (true)
                {
                    Console.WriteLine("Worker listening...");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
        
        private static string GetText(string id) => RedisDatabase.StringGet("TEXT-" + id);
        private static bool SetRank(string id, double rank) => RedisDatabase.StringSet("RANK-" + id, rank);

        private static double CalculateRank(string text)
        {
            var notLetterCount = text.Count(ch => !char.IsLetter(ch));
            return (double)notLetterCount / text.Length;
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

        private static IDatabase RedisDatabase => ConnectToRedis();
    }
}