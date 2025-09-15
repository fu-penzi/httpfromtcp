using System.Net;
using System.Net.Sockets;
using httpfromtcp.Parsing;

IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 42069);
TcpListener listener = new(ipEndPoint);

try
{
    listener.Start();
    Console.WriteLine($"Listening on: {ipEndPoint}");


    TcpClient handler = listener.AcceptTcpClient();
    NetworkStream stream = handler.GetStream();

    try
    {
        Request r = Request.FromStream(stream);
        Console.WriteLine(
            $"""
             Request line:

             - Method: {r.RequestLine.Method}
             - Target: {r.RequestLine.RequestTarget}
             - Version: {r.RequestLine.HttpVersion}
             """
        );
        Console.WriteLine($"Headers:\n{r.Headers}");
    }
    catch (IncorrectFormatException e)
    {
        Console.WriteLine(e.Message);
    }
    catch (IOException e)
    {
        Console.WriteLine($"(Connection terminated): {e.Message}");
        handler = listener.AcceptTcpClient();
        stream = handler.GetStream();
    }
}
finally
{
    listener.Stop();
}