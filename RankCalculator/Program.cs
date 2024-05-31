using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using NATS.Client;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RankCalculator
{
    public class RankCalculatorData
    {
        public string Id { get; set; }
        public string Region { get; set; }
    }
    
    internal class Program
    {
        private ConnectionMultiplexer _redis;
        private IDatabase RedisDatabase => _redis.GetDatabase();
        private IServer RedisServer => _redis.GetServer(_redis.GetEndPoints()[0]);
        
        private static void Main(string[] args)
        {
            var program = new Program();
            program.StartCalculation();
        }

        public void StartCalculation()
        {
            using (var c = ConnectToNats())
            {
                var s = c.SubscribeAsync("rank.calculate", "rank_calculator", (sender, args) =>
                {
                    var receivedData = Encoding.UTF8.GetString(args.Message.Data);
                    var deserializedData = DeserializeData<RankCalculatorData>(receivedData);
                    
                    _redis = ConnectToRedis(deserializedData.Region);

                    string text = GetText(deserializedData.Id);
                    var rank = CalculateRank(text);
                    SetRank(deserializedData.Id, rank);

                    c.Publish("rank.calculated", BitConverter.GetBytes(rank));
                });

                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private static double CalculateRank(string text)
        {
            var notLetterCount = text.Count(ch => !char.IsLetter(ch));
            return (double)notLetterCount / text.Length;
        }
        
        private string GetText(string id) => RedisDatabase.StringGet("TEXT-" + id);
        private bool SetRank(string id, double rank) => RedisDatabase.StringSet("RANK-" + id, rank);

        private static IConnection ConnectToNats()
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");

            ConnectionFactory factory = new ConnectionFactory();

            var options = ConnectionFactory.GetDefaultOptions();
            options.Url = natsUrl;
            
            return factory.CreateConnection(options);
        }
        
        private static ConnectionMultiplexer ConnectToRedis(string region)
        {
            var hostAndPort = Environment.GetEnvironmentVariable("DB_" + region.ToUpper()) ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(hostAndPort);
        }
        
        public T DeserializeData<T>(string jsonData)
        {
            return JsonConvert.DeserializeObject<T>(jsonData);
        }
    }
}