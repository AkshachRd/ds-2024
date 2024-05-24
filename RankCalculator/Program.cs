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
                    var text = Encoding.UTF8.GetString(args.Message.Data);

                    var rank = CalculateRank(text);

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

        private static IConnection ConnectToNats()
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL");

            ConnectionFactory factory = new ConnectionFactory();

            var options = ConnectionFactory.GetDefaultOptions();
            options.Url = natsUrl;

            return factory.CreateConnection(options);
        }
    }
}