using System.Net;
using System.Net.Sockets;
using System.Text;
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
        Request r = Request.FromStream(new Reader(stream));
        Console.WriteLine(
            $"""

             Request line:
             - Method: {r.RequestLine.Method}
             - Target: {r.RequestLine.RequestTarget}
             - Version: {r.RequestLine.HttpVersion}
             """
        );
        Console.WriteLine(
            $"Headers:\n{r.Headers}" +
            $"Body:\n{Encoding.UTF8.GetString(r.Body.ToArray())}\n" +
            $"Error:\n{r.Error}");
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