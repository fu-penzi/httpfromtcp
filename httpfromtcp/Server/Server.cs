using System.Net;
using System.Net.Sockets;

namespace httpfromtcp.Server;

public delegate Response RequestHandler(Request request);

public class Server(int port)
{
    private static IPAddress LocalhostAddress { get; } = IPAddress.Parse("127.0.0.1");

    private TcpListener    Listener { get; } = new(new IPEndPoint(LocalhostAddress, port));
    private RequestHandler _handler;

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
        Console.WriteLine($"Listening on: {Listener.LocalEndpoint}");

        _ = Listen();
    }

    public void Close()
    {
        Listener.Stop();
    }

    public void Handle(RequestHandler handler)
    {
        _handler = handler;
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
            Response response = _handler(request);
            response.AddDefaultHeaders(response.Body.Length);

            response.WriteStatusLine(stream);
            response.WriteHeaders(stream);
            response.WriteBody(stream);
        }
        catch (IOException e)
        {
            Console.WriteLine($"(Connection terminated): {e.Message}");
        }
    }
}