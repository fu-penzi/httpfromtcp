using System.Net;
using System.Net.Sockets;
using System.Text;

namespace httpfromtcp.Server;

public class Server
{
    private IPEndPoint  IpEndPoint { get; }
    private TcpListener Listener   { get; }

    public Server(int port)
    {
        IpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        Listener = new TcpListener(IpEndPoint);
    }

    public void Serve()
    {
        try
        {
            Listener.Start();
        }
        catch (SocketException e)
        {
            Console.WriteLine("Error when establishing connection:\n");
            Console.WriteLine(e);
            throw;
        }
        Console.WriteLine($"Listening on: {IpEndPoint}");

        _ = Listen();
    }

    public void Close()
    {
        Listener.Stop();
    }

    private async Task Listen()
    {
        while (true)
        {
            TcpClient client = await Listener.AcceptTcpClientAsync();
            ThreadPool.QueueUserWorkItem(Handle, client, false);
        }
    }

    private void Handle(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        try
        {
            Request request = Request.FromStream(stream);
            Console.WriteLine(
                $"""

                 Request line:
                 - Method: {request.RequestLine.Method}
                 - Target: {request.RequestLine.RequestTarget}
                 - Version: {request.RequestLine.HttpVersion}
                 """
            );
            Console.WriteLine(
                $"Headers:\n{request.Headers}" +
                $"Body:\n{Encoding.UTF8.GetString(request.Body.ToArray())}\n" +
                $"Error:\n{request.Error}");
        }
        catch (IOException e)
        {
            Console.WriteLine($"(Connection terminated): {e.Message}");
        }
    }
}