using System.Net;
using System.Net.Sockets;

namespace httpfromtcp.Server;

public delegate Task<Response> RequestHandler(Request request);

internal record Handler(Http.Method HttpMethod, string Route, RequestHandler RequestHandler);

/// <summary>
/// Server listening for requests on localhost for given port.
/// </summary>
/// <param name="port">Port to listen on.</param>
public class Server(int port)
{
    private static IPAddress LocalhostAddress { get; } = IPAddress.Parse("127.0.0.1");

    private TcpListener   Listener { get; } = new(new IPEndPoint(LocalhostAddress, port));
    private List<Handler> Handlers { get; } = [];

    /// <summary>
    /// Start listening for requests.
    /// </summary>
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

    /// <summary>
    /// Close connection.
    /// </summary>
    public void Close()
    {
        Listener.Stop();
    }

    /// <summary>
    /// Register handler function for given router path.
    /// </summary>
    /// <param name="httpMethod">HTTP method</param>
    /// <param name="route">Router path (ex. /test)</param>
    /// <param name="requestHandler">Function handling requests for route.</param>
    public void Handle(Http.Method httpMethod, string route, RequestHandler requestHandler)
    {
        Handlers.Add(new Handler(httpMethod, route, requestHandler));
    }

    private async Task Listen()
    {
        while (true)
        {
            TcpClient client = await Listener.AcceptTcpClientAsync();
            _ = Handle(client);
        }
    }

    private async Task Handle(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        try
        {
            Request request = Request.FromStream(stream);

            var foundHandler = Handlers.Find(handler =>
                handler.Route == request.RequestLine.RequestTarget &&
                Http.ParseMethod(request.RequestLine.Method) == handler.HttpMethod);

            Response response = foundHandler is null
                ? Response.GetNotFoundResponse()
                : await foundHandler.RequestHandler(request);

            response.AddDefaultHeaders();
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