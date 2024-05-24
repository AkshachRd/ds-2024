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
            using (var c = ConnectToNats())
            {
                var s = c.SubscribeAsync("rank.calculate", "rank_calculator", (sender, args) =>
                {
                    var id = Encoding.UTF8.GetString(args.Message.Data);
                    
                    string text = GetText(id);
                    var rank = CalculateRank(text);
                    SetRank(id, rank);

                    c.Publish("rank.calculated", BitConverter.GetBytes(rank));
                });
                
                while (true)
                {
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
        
        private static ConnectionMultiplexer ConnectToRedis()
        {
            var hostAndPort = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(hostAndPort);
        }

        private static ConnectionMultiplexer Redis => ConnectToRedis();
    
        private static IDatabase RedisDatabase => Redis.GetDatabase();
        private static IServer RedisServer => Redis.GetServer(Redis.GetEndPoints()[0]);
    }
}