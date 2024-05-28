﻿using System;
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
        bool isInitiator = args is [_, _, _, "true"];

        int localX = int.Parse(Console.ReadLine());

        Socket listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
        listener.Listen(10);
        Console.WriteLine("Listening port: {0}", listeningPort);

        var senderIpAddress = Dns.GetHostAddresses(nextHost)[0];
        Socket sender = new Socket(senderIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint nextEndPoint = new IPEndPoint(senderIpAddress, nextPort);
        sender.Connect(nextEndPoint);
        Console.WriteLine("Next host: {0}", nextHost);
        Console.WriteLine("Next port: {0}", nextPort);
        Console.WriteLine("Is initiator: {0}", isInitiator);

        if (isInitiator)
        {
            SendMessage(sender, localX.ToString());
            localX = int.Parse(ReceiveMessage(listener));
            SendMessage(sender, localX.ToString());
            localX = int.Parse(ReceiveMessage(listener));
            Console.WriteLine("Answer: {0}", localX);
        }
        else
        {
            int receivedY = int.Parse(ReceiveMessage(listener));
            int maxValue = Math.Max(localX, receivedY);
            SendMessage(sender, maxValue.ToString());
            localX = int.Parse(ReceiveMessage(listener));
            Console.WriteLine("Answer: {0}", localX);
        }
        
        sender.Shutdown(SocketShutdown.Both);
        sender.Close();
        listener.Shutdown(SocketShutdown.Both);
        listener.Close();
    }

    static void SendMessage(Socket socket, string message)
    {
        try
        {
            Console.WriteLine("Remote address of socket connection: {0}",
                socket.RemoteEndPoint.ToString());
            Console.WriteLine("Sending data: {0}", message);

            // Подготовка данных к отправке
            byte[] msg = Encoding.UTF8.GetBytes(message + "<EOF>");

            // SEND
            int bytesSent = socket.Send(msg);

            // RELEASE
        }
        catch (ArgumentNullException ane)
        {
            Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
        }
        catch (SocketException se)
        {
            Console.WriteLine("SocketException : {0}", se.ToString());
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception : {0}", e.ToString());
        }

        // byte[] data = Encoding.UTF8.GetBytes(message);
        // await socket.SendToAsync(new ArraySegment<byte>(data), SocketFlags.None, endPoint);
    }

    static string ReceiveMessage(Socket socket)
    {
        // byte[] buffer = new byte[1024];
        // var result = await socket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        // return Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);

        try
        {
            while (true)
            {
                Console.WriteLine("Waiting for a client ot connect...");
                // ACCEPT
                Socket handler = socket.Accept();

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
                        break;
                    }
                }

                data = data.Substring(0, data.Length - 5);
                Console.WriteLine("Received message: {0}", data);

                // RELEASE
                handler.Close();

                return data;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        return "";
    }
}
