using System.Text;
using httpfromtcp.Parsing;

namespace httpfromtcp.Tests;

class ChunkReader(MemoryStream stream, int numBytesPerRead) : MemoryStream
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, numBytesPerRead);
    }
}

[TestClass]
public sealed class RequestTest
{
    private const string DefaultRequestLine = "GET /coffee HTTP/1.1\r\n";

    private const string DefaultHeaders = "Host: localhost:42069\r\n" +
                                          "User-Agent: curl/7.81.0\r\n" +
                                          "Accept: */*\r\n" +
                                          "\r\n";

    [TestClass]
    public class RequestLine
    {
        // Good: GET Request line
        [DataRow("GET", "/", "1.1", "GET / HTTP/1.1\r\n")]
        // Good: POST Request line
        [DataRow("POST", "/", "1.1", "POST / HTTP/1.1\r\n")]
        // Good: GET Request line with path
        [DataRow("GET", "/coffee", "1.1", "GET /coffee HTTP/1.1\r\n")]
        // Good: POST Request line with path
        [DataRow("POST", "/coffee", "1.1", "POST /coffee HTTP/1.1\r\n")]
        [TestMethod]
        public void Good(string method, string target, string httpVersion, string request)
        {
            Request r = Request.FromStream(
                GetStream($"{request}Host: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n"));
            Assert.AreEqual(method, r.RequestLine.Method);
            Assert.AreEqual(target, r.RequestLine.RequestTarget);
            Assert.AreEqual(httpVersion, r.RequestLine.HttpVersion);

            Assert.AreEqual("localhost:42069", r.Headers.Get("Host"));
            Assert.AreEqual("curl/7.81.0", r.Headers.Get("User-Agent"));
            Assert.AreEqual("*/*", r.Headers.Get("Accept"));
        }

        //BAD: Invalid request line parts
        [DataRow($"/coffee HTTP/1.1\r\n")]
        //Bad: Invalid spacing
        [DataRow($"GET   /   HTTP/1.1\r\n")]
        //Bad: Invalid HTTP version
        [DataRow($"GET / HTTP/2.1\r\n")]
        [TestMethod]
        public void Bad_Unsupported_Format(string request)
        {
            TestForException(() => { Request.FromStream(GetStream($"{request}{DefaultHeaders}")); });
        }

        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(100)]
        [TestMethod]
        public void VariableChunkSize(int numBytesPerRead)
        {
            const string request = $"GET /test HTTP/1.1\r\n" +
                                   $"\r\n";
            Stream stream = new ChunkReader(new MemoryStream(Encoding.UTF8.GetBytes(request)), numBytesPerRead);
            Request r = Request.FromStream(stream);

            Assert.AreEqual("GET", r.RequestLine.Method);
            Assert.AreEqual("/test", r.RequestLine.RequestTarget);
            Assert.AreEqual("1.1", r.RequestLine.HttpVersion);
        }
    }


    [TestClass]
    public class Headers
    {
        // Good: Standard headers
        [DataRow("Host: localhost:42069\r\n" +
                 "User-Agent: curl/7.81.0\r\n" +
                 "Accept: */*\r\n" +
                 "\r\n")]
        [TestMethod]
        public void StandardHeaders(string headers)
        {
            Request r = Request.FromStream(GetStream($"{DefaultRequestLine}{headers}"));
            Assert.AreEqual("localhost:42069", r.Headers.Get("Host"));
            Assert.AreEqual("curl/7.81.0", r.Headers.Get("User-Agent"));
            Assert.AreEqual("*/*", r.Headers.Get("Accept"));

            Assert.AreEqual(3, r.Headers.Data().Count);
        }


        // Good: Long headers block
        [DataRow("Host: localhost:42069\r\n" +
                 "User-Agent: curl/7.81.0\r\n" +
                 "Accept: */*\r\n")]
        [TestMethod]
        public void LongHeaders(string headers)
        {
            Request r = Request.FromStream(
                GetStream($"{DefaultRequestLine}{string.Concat(Enumerable.Repeat(headers, 1000))}\r\n"));

            Assert.AreEqual(16998, r.Headers.Get("Host").Length);
            Assert.AreEqual(12998, r.Headers.Get("User-Agent").Length);
            Assert.AreEqual(4998, r.Headers.Get("Accept").Length);

            Assert.AreEqual(3, r.Headers.Data().Count);
        }

        // Good: Single long header
        [TestMethod]
        public void SingleLongHeader()
        {
            string longHeader = $"Host: {string.Concat(Enumerable.Repeat("Test, ", 1000))}";
            Request r = Request.FromStream(GetStream($"{DefaultRequestLine}{longHeader}\r\n\r\n"));

            Assert.AreEqual(1, r.Headers.Data().Count);
        }

        // Good: Empty headers
        [DataRow("\r\n")]
        [TestMethod]
        public void EmptyHeaders(string headers)
        {
            Request r = Request.FromStream(GetStream($"{DefaultRequestLine}{headers}"));

            Assert.AreEqual(0, r.Headers.Data().Count);
        }

        // Good: Duplicate headers
        [DataRow("Host: localhost:42069\r\n" +
                 "Host: www.test.pl\r\n" +
                 "\r\n")]
        [TestMethod]
        public void Duplicate(string headers)
        {
            Request r = Request.FromStream(GetStream($"{DefaultRequestLine}{headers}"));
            Assert.AreEqual("localhost:42069, www.test.pl", r.Headers.Get("Host"));
            Assert.AreEqual(1, r.Headers.Data().Count);
        }


        // Bad: Malformed header
        [DataRow("Host : localhost:42069\r\n" +
                 "Host: www.test.pl\r\n" +
                 "\r\n")]
        [TestMethod]
        public void MalformedHeader(string headers)
        {
            TestForException((() => { Request.FromStream(GetStream($"{DefaultRequestLine}{headers}")); }));
        }
    }


    [TestClass]
    public class Body
    {
        // Good: Standard headers
        [DataRow(
            "GET /coffee HTTP/1.1\r\n" +
            "Host: localhost:42069\r\n" +
            "Content-Length: 13\r\n" +
            "\r\n" +
            "hello world!\n"
        )]
        [TestMethod]
        public void StandardBody(string request)
        {
            Request r = Request.FromStream(GetStream(request));

            Assert.AreEqual("hello world!\n", Encoding.UTF8.GetString(r.Body));

            // Assert.AreEqual("localhost:42069", r.Headers.Get("Host"));
            // Assert.AreEqual("curl/7.81.0", r.Headers.Get("User-Agent"));
            // Assert.AreEqual("*/*", r.Headers.Get("Accept"));

            // Assert.AreEqual(3, r.Headers.Data().Count);
        }
    }

    private static Stream GetStream(string data)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(data));
    }

    private static void TestForException(Action test)
    {
        Exception? ex = null;
        try
        {
            test();
        }
        catch (IncorrectFormatException e)
        {
            ex = e;
        }

        Assert.IsNotNull(ex);
    }
}