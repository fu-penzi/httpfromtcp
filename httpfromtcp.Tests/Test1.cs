using System.Net;
using System.Text;
namespace httpfromtcp.Tests;

class ChunkReader(MemoryStream stream, int NumBytesPerRead) : MemoryStream
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, NumBytesPerRead);
    }
}


[TestClass]
public sealed class RequestFromReader
{
    // Good: GET Request line
    [DataRow("GET", "/", "1.1",
        "GET / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]

    // Good: POST Request line
    [DataRow("POST", "/", "1.1",
        "POST / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]

    // Good: GET Request line with path
    [DataRow("GET", "/coffee", "1.1",
        "GET /coffee HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]

    // Good: POST Request line with path
    [DataRow("POST", "/coffee", "1.1",
        "POST /coffee HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n"
     )]
    [TestMethod]
    public void Good(string method, string target, string httpVersion, string request)
    {
        HttpServer httpServer = new();
        Request r = httpServer.RequestFromReader(new MemoryStream(Encoding.UTF8.GetBytes(request)));
        Assert.AreEqual(method, r.RequestLine.Method);
        Assert.AreEqual(target, r.RequestLine.RequestTarget);
        Assert.AreEqual(httpVersion, r.RequestLine.HttpVersion);
    }


    //BAD: Invalid request line parts
    [DataRow("/coffee HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]
    //Bad: Invalid HTTP version
    [DataRow("GET / HTTP/2.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]
    [TestMethod]
    public void Bad_Unsupported_Format(string request)
    {
        HttpServer httpServer = new();
        Exception? ex = null;
        try
        {
            Request r = httpServer.RequestFromReader(new MemoryStream(Encoding.UTF8.GetBytes(request)));
        }
        catch (IncorrectFormatException e)
        {
            ex = e;
        }
        Assert.IsNotNull(ex);
    }

    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(6)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(1024)]
    [TestMethod]
    public void VariableChunkSize(int NumBytesPerRead)
    {
        string request = "GET /test HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n";
        HttpServer httpServer = new();
        Stream stream = new ChunkReader(new MemoryStream(Encoding.UTF8.GetBytes(request)), NumBytesPerRead);


        Request r = httpServer.RequestFromReader(stream);

        Assert.AreEqual("GET", r.RequestLine.Method);
        Assert.AreEqual("/test", r.RequestLine.RequestTarget);
        Assert.AreEqual("1.1", r.RequestLine.HttpVersion);
    }

    [DataRow("GET / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n")]
    [TestMethod]
    public void Test(string request)
    {
        HttpServer httpServer = new();
        Request r = httpServer.RequestFromReader(new MemoryStream(Encoding.UTF8.GetBytes(request)));
    }
}
