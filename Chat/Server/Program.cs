using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

class Program
{
    static byte[] GetBytesFromStringList(List<string> stringList, char delimiter)
    {
        // Join the list of strings into a single string using the delimiter
        string joinedString = string.Join(delimiter, stringList);

        // Convert the joined string into a byte array
        byte[] byteArray = Encoding.UTF8.GetBytes(joinedString);

        return byteArray;
    }

    public static void StartListening(int port)
    {
        // Разрешение сетевых имён

        // Привязываем сокет ко всем интерфейсам на текущей машинe
        IPAddress ipAddress = IPAddress.Any;

        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        // CREATE
        Socket listener = new Socket(
            ipAddress.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        List<string> messages = [];

        try
        {
            // BIND
            listener.Bind(localEndPoint);

            // LISTEN
            listener.Listen(10);

            while (true)
            {
                Console.WriteLine("Waiting for a client ot connect...");
                // ACCEPT
                Socket handler = listener.Accept();

                Console.WriteLine("Receiving data...");
                byte[] buf = new byte[1024];
                string data = null;
                while (true)
                {
                    // RECEIVE
                    int bytesRec = handler.Receive(buf);

                    data += Encoding.UTF8.GetString(buf, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        messages.Add(data.Substring(0, data.Length - 5));
                        break;
                    }
                }

                Console.WriteLine("Received message: {0}", data);

                // Отправляем текст обратно клиенту
                byte[] msg = GetBytesFromStringList(messages, ',');

                // SEND
                handler.Send(msg);

                // RELEASE
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Please enter arguments in format: prog.exe <port>");
        }
        
        var port = 0;
        if (!Int32.TryParse(args[0], out port))
        {
            Console.WriteLine("Invalid port");
        }
        
        Console.WriteLine("Starting up the server...");
        
        StartListening(port);

        Console.WriteLine("\nPress ENTER to exit...");
        Console.Read();
    }
}