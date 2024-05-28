using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
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
        Socket? previousMember;
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
            
            previousMember = ConnectToPreviousMember(listener);
            if (previousMember == null)
            {
                Console.WriteLine("Can't connect to previous member");
            }
            
            localX = int.Parse(ReceiveMessage(previousMember));
            SendMessage(sender, localX.ToString());
            localX = int.Parse(ReceiveMessage(previousMember));
            Console.WriteLine("Answer: {0}", localX);
        }
        else
        {
            previousMember = ConnectToPreviousMember(listener);
            if (previousMember == null)
            {
                Console.WriteLine("Can't connect to previous member");
            }

            int receivedY = int.Parse(ReceiveMessage(previousMember));
            int maxValue = Math.Max(localX, receivedY);
            SendMessage(sender, maxValue.ToString());
            localX = int.Parse(ReceiveMessage(previousMember));
            SendMessage(sender, localX.ToString());
            Console.WriteLine("Answer: {0}", localX);
        }

        sender.Shutdown(SocketShutdown.Both);
        sender.Close();

        previousMember.Close();
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
        try
        {
            Console.WriteLine("Receiving data...");
            byte[] buf = new byte[1024];
            string data = null;
            while (true)
            {
                // RECEIVE
                int bytesRec = socket.Receive(buf);

                data += Encoding.UTF8.GetString(buf, 0, bytesRec);
                if (data.IndexOf("<EOF>") > -1)
                {
                    break;
                }
            }

            data = data.Substring(0, data.Length - 5);
            Console.WriteLine("Received message: {0}", data);

            return data;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        return "";
    }
    
    static Socket? ConnectToPreviousMember(Socket socket)
    {
        try
        {
            while (true)
            {
                Console.WriteLine("Waiting for a client to connect...");
                // ACCEPT
                Socket? handler = socket.Accept();
                return handler;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        return null;
    }
}
