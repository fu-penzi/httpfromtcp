using httpfromtcp;
using System.Net;
using System.Net.Sockets;

IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 42069);
TcpListener listener = new(ipEndPoint);

try
{
    listener.Start();
    Console.WriteLine($"Listening on: {ipEndPoint}");


    TcpClient handler = listener.AcceptTcpClient();
    NetworkStream stream = handler.GetStream();

    while (true)
    {
        try
        {
            foreach (var line in HttpServer.GetLines(stream))
            {
                Console.WriteLine($"READ: {line}");
            }
        }
        catch (IOException e)
        {
            Console.WriteLine($"(Connection terminated): {e.Message}");
            handler = listener.AcceptTcpClient();
            stream = handler.GetStream();
        }
    }
}
finally
{
    listener.Stop();
}
