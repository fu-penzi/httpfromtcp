using System.Net;
using System.Text;

namespace httpfromtcp.Tests;

[TestClass]
public sealed class HeadersTest
{
    // Good: Valid single header
    [DataRow("Host: localhost:42069\r\n\r\n")]
    [TestMethod]
    public void SingleHeader(string data)
    {
        Headers headers = new();

        var (parsed, done) = headers.Parse(Encoding.UTF8.GetBytes(data));

        headers.Data.TryGetValue("Host", out var value);
        Assert.AreEqual("localhost:42069", value);
        Assert.AreEqual(23, parsed);
        Assert.IsFalse(done);
    }

    // Good: Valid two headers
    [TestMethod]
    public void TwoHeaders()
    {
        Headers headers = new();
        var (parsedH1, doneH1) = headers.Parse("Host: localhost:42069\r\nUser-Agent: curl/7.81.0\r\n\r\n"u8.ToArray());
        Assert.AreEqual("localhost:42069", headers.Data["Host"]);
        Assert.AreEqual(23, parsedH1);
        Assert.IsFalse(doneH1);

        var (parsedH2, doneH2) = headers.Parse("User-Agent: curl/7.81.0\r\n\r\n"u8.ToArray());
        Assert.AreEqual("curl/7.81.0", headers.Data["User-Agent"]);
        Assert.AreEqual(25, parsedH2);
        Assert.IsFalse(doneH2);
    }

    // Good: End of headers
    [DataRow("\r\n")]
    [TestMethod]
    public void EndOfHeaderBlock(string data)
    {
        var (_, done) = new Headers().Parse(Encoding.UTF8.GetBytes(data));
        Assert.IsTrue(done);
    }

    // Bad: Invalid spacing
    [DataRow("Host:localhost:42069\r\n\r\n")]
    [DataRow("Host : localhost:42069\r\n\r\n")]
    [DataRow(" Host: localhost:42069 \r\n\r\n")]
    [DataRow("            Host : localhost:42069           \r\n\r\n")]
    [TestMethod]
    public void BadSpacing(string data)
    {
        TestForException((() => new Headers().Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Bad: Missing header
    [DataRow("localhost:42069\r\n\r\n")]
    [DataRow(": localhost:42069\r\n\r\n")]
    [DataRow(" : localhost:42069\r\n\r\n")]
    [DataRow(" :localhost:42069\r\n\r\n")]
    [TestMethod]
    public void MissingHeader(string data)
    {
        TestForException((() => new Headers().Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Bad: Missing value
    [DataRow("Host:\r\n\r\n")]
    [DataRow("Host: \r\n\r\n")]
    [TestMethod]
    public void MissingValue(string data)
    {
        TestForException((() => new Headers().Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Bad: Missing header and value
    [DataRow(" : \r\n\r\n")]
    [DataRow(":\r\n\r\n")]
    [DataRow(" \r\n\r\n")]
    [TestMethod]
    public void MissingHeaderAndValue(string data)
    {
        TestForException((() => new Headers().Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Bad: Duplicated header
    [TestMethod]
    public void DuplicatedHeader()
    {
        TestForException(() =>
        {
            Headers headers = new();
            headers.Parse("Host: localhost:42069\r\n\r\n"u8.ToArray());
            headers.Parse("Host: www.test.com\r\n\r\n"u8.ToArray());
        });
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