// Program.cs
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: <listening-port> <next-host> <next-port> [true]");
            return;
        }

        int listeningPort = int.Parse(args[0]);
        string nextHost = args[1];
        int nextPort = int.Parse(args[2]);
        bool isInitiator = args.Length == 4 && args[3] == "true";

        int X = int.Parse(Console.ReadLine());

        using (UdpClient listener = new UdpClient(listeningPort))
        using (UdpClient sender = new UdpClient())
        {
            IPEndPoint nextEndPoint = new IPEndPoint(IPAddress.Parse(nextHost), nextPort);

            if (isInitiator)
            {
                await SendValue(sender, nextEndPoint, X);
                X = await ReceiveValue(listener);
                await SendValue(sender, nextEndPoint, X);
                X = await ReceiveValue(listener);
            }
            else
            {
                X = Math.Max(X, await ReceiveValue(listener));
                await SendValue(sender, nextEndPoint, X);
                X = await ReceiveValue(listener);
            }

            Console.WriteLine(X);
        }
    }

    static async Task<int> ReceiveValue(UdpClient client)
    {
        var result = await client.ReceiveAsync();
        return int.Parse(Encoding.UTF8.GetString(result.Buffer));
    }

    static async Task SendValue(UdpClient client, IPEndPoint endPoint, int value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value.ToString());
        await client.SendAsync(data, data.Length, endPoint);
    }
}