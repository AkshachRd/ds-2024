using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

struct Args
{
    public int ListeningPort;
    public IPAddress NextHost;
    public int NextPort;
    public bool IsInitiator;
}

class Program
{
    static void Main(string[] args)
    {
        var myArgs = ParseArgs(args);
        
        Console.WriteLine("Listening port: {0}", myArgs.ListeningPort);
        Console.WriteLine("Next host: {0}", myArgs.NextHost.ToString());
        Console.WriteLine("Next port: {0}", myArgs.NextPort);
        Console.WriteLine("Is initiator: {0}", myArgs.IsInitiator);

        int localX = int.Parse(Console.ReadLine());

        Socket listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, myArgs.ListeningPort));
        listener.Listen(10);
        Socket? previousMember;

        Socket sender = new Socket(myArgs.NextHost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint nextEndPoint = new IPEndPoint(myArgs.NextHost, myArgs.NextPort);
        sender.Connect(nextEndPoint);
        
        previousMember = ConnectToPreviousMember(listener);
        if (previousMember == null)
        {
            Console.WriteLine("Can't connect to previous member");
        }

        if (myArgs.IsInitiator)
        {
            SendMessage(sender, localX.ToString());
            localX = int.Parse(ReceiveMessage(previousMember));
            SendMessage(sender, localX.ToString());
            localX = int.Parse(ReceiveMessage(previousMember));
            Console.WriteLine("Answer: {0}", localX);
        }
        else
        {
            int receivedY = int.Parse(ReceiveMessage(previousMember));
            int maxValue = Math.Max(localX, receivedY);
            SendMessage(sender, maxValue.ToString());
            localX = int.Parse(ReceiveMessage(previousMember));
            SendMessage(sender, localX.ToString());
            Console.WriteLine("Answer: {0}", localX);
        }

        // RELEASE
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

            byte[] msg = Encoding.UTF8.GetBytes(message + "<EOF>");
            socket.Send(msg);
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

    static Args ParseArgs(string[] args)
    {
        if (args.Length < 3)
        {
            throw new Exception("Usage: <listening-port> <next-host> <next-port> [true]");
        }
        
        var myArgs = new Args();
        
        myArgs.ListeningPort = 0;
        if (!Int32.TryParse(args[0], out myArgs.ListeningPort))
        {
            throw new Exception("Invalid listening port");
        }
        
        myArgs.NextHost = IPAddress.None;
        if (!IPAddress.TryParse(args[1], out myArgs.NextHost))
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(args[1]);
                myArgs.NextHost = hostEntry.AddressList.Last();
            }
            catch (Exception e)
            {
                throw new Exception($"Invalid next host name or IP address: {e.Message}");
            }
        }
        
        myArgs.NextPort = 0;
        if (!Int32.TryParse(args[2], out myArgs.NextPort))
        {
            throw new Exception("Invalid next port");
        }
        
        myArgs.IsInitiator = args is [_, _, _, "true"];
        
        return myArgs;
    }
}
