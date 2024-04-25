using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NATS.Client;

namespace EventLogger
{
    internal class Program
    {
        private static IConnection _connection = ConnectToNats();
        
        private static readonly Dictionary<string, Func<byte[], object>> DataConverters = new Dictionary<string, Func<byte[], object>>
        {
            { "Rank", data => BitConverter.ToDouble(data, 0) },
            { "Similarity", data => BitConverter.ToInt32(data, 0) }
        };
        
        private static void Main(string[] args)
        {
            var name = args[0];
            var context = args[1];
            Console.WriteLine($"{name}: {context}");
            
            var queue = name.ToLower() + "_logger";
            var s = _connection.SubscribeAsync(context, queue, (sender, args) =>
            {
                if (DataConverters.TryGetValue(name, out var converter))
                {
                    var result = converter(args.Message.Data);
                    Console.WriteLine($"{name}: {context} - {result}");
                }
                else
                {
                    Console.WriteLine($"No converter available for {name}");
                }
            });
            s.Start();
            
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
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