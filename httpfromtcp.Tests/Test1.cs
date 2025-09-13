using Microsoft.VisualStudio.TestPlatform.TestHost;
using Newtonsoft.Json.Linq;
using System.Text;
namespace httpfromtcp.Tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        MemoryStream stream = new(Encoding.UTF8.GetBytes("GET / HTTP/1.1\r\nHost: localhost:42069\r\nUser-Agent: curl/7.81.0\r\nAccept: */*\r\n\r\n"));
        var enumerator = HttpServer.GetLines(stream).GetEnumerator();

        enumerator.MoveNext();
        Assert.AreEqual("GET / HTTP/1.1", enumerator.Current);

        enumerator.MoveNext();
        Assert.AreEqual("Host: localhost:42069", enumerator.Current);

        enumerator.MoveNext();
        Assert.AreEqual("User-Agent: curl/7.81.0", enumerator.Current);

        enumerator.MoveNext();
        Assert.AreEqual("Accept: */*", enumerator.Current);
    }
}
