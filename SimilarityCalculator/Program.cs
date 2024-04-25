using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;
using StackExchange.Redis;

namespace SimilarityCalculator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using (var c = ConnectToNats())
            {
                var s = c.SubscribeAsync("similarity.calculate", "similarity_calculator", (sender, args) =>
                {
                    var id = Encoding.UTF8.GetString(args.Message.Data);
                    
                    string text = GetText(id);
                    var similarity = CalculateSimilarity(text);
                    SetSimilarity(id, similarity);

                    c.Publish("similarity.calculated", BitConverter.GetBytes(similarity));
                });
                
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
        
        private static string GetText(string id) => RedisDatabase.StringGet("TEXT-" + id);
        private static bool SetSimilarity(string id, int similarity) => RedisDatabase.StringSet("SIMILARITY-" + id, similarity);

        private static int CalculateSimilarity(string text)
        {
            var keys = RedisServer.Keys().ToList();
            var counter = 0;

            for (int i = 0; i < keys.Count() && counter < 2; i++)
            {
                var redisKey = keys[i];
                if (redisKey.ToString().Substring(0, 5) == "TEXT-" 
                    && RedisDatabase.StringGet(redisKey) == text)
                {
                    counter++;
                }
            }

            return counter >= 2 ? 1 : 0;
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