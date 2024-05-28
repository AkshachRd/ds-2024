using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client;

class Program
{
    static List<string> GetStringListFromBytes(byte[] bytes, int byteCount, char delimiter)
    {
        // Decode the specified number of bytes into a single string
        string decodedString = Encoding.UTF8.GetString(bytes, 0, byteCount);

        // Split the string into a list of strings based on the delimiter
        List<string> stringList = new List<string>(decodedString.Split(delimiter));

        return stringList;
    }
    
    public static void StartClient(IPAddress ipAddress, int port, string message)
    {
        try
        {
            // Разрешение сетевых имён
            //IPAddress ipAddress = IPAddress.Loopback;
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];

            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // CREATE
            Socket sender = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                // CONNECT
                sender.Connect(remoteEP);

                Console.WriteLine("Remote address of socket connection: {0}",
                    sender.RemoteEndPoint.ToString());

                // Подготовка данных к отправке
                byte[] msg = Encoding.UTF8.GetBytes(message + "<EOF>");

                // SEND
                int bytesSent = sender.Send(msg);

                // RECEIVE
                byte[] buf = new byte[1024];
                int bytesRec = sender.Receive(buf);
                var answer = GetStringListFromBytes(buf, bytesRec, ',');

                Console.WriteLine("Previous messages: [{0}]", string.Join(", ", answer));

                // RELEASE
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
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
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Please enter arguments in format: prog.exe <host address> <port> <message>");
        }

        var ipAddress = IPAddress.None;
        if (!IPAddress.TryParse(args[0], out ipAddress))
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(args[0]);
                ipAddress = hostEntry.AddressList.Last();
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid host name or IP address: {0}", e.Message);
                return;
            }
        }
        
        var port = 0;
        if (!Int32.TryParse(args[1], out port))
        {
            Console.WriteLine("Invalid port");
        }
        
        var message = args[2];

        StartClient(ipAddress, port, message);
    }
}