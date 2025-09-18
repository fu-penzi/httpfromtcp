using System.Text;
using httpfromtcp.Server;

namespace httpfromtcp.Tests;

[TestClass]
public sealed class HeadersTest
{
    // Good: Valid single header
    [DataRow("Host: localhost:42069\r\n\r\n", 23)]
    [DataRow("          Host: localhost:42069              \r\n\r\n", 47)]
    [TestMethod]
    public void SingleHeader(string data, int expectedParsed)
    {
        Headers headers = new();

        var (parsed, done) = headers.Parse(Encoding.UTF8.GetBytes(data));

        Assert.AreEqual("localhost:42069", headers.Get("Host"));
        Assert.AreEqual(expectedParsed, parsed);
        Assert.IsFalse(done);
    }

    // Good: Valid two headers
    [TestMethod]
    public void TwoHeaders()
    {
        Headers headers = new();
        var (parsedH1, doneH1) = headers.Parse("Host: localhost:42069\r\nUser-Agent: curl/7.81.0\r\n\r\n"u8.ToArray());
        Assert.AreEqual("localhost:42069", headers.Get("Host"));
        Assert.AreEqual(23, parsedH1);
        Assert.IsFalse(doneH1);

        var (parsedH2, doneH2) = headers.Parse("User-Agent: curl/7.81.0\r\n\r\n"u8.ToArray());
        Assert.AreEqual("curl/7.81.0", headers.Get("User-Agent"));
        Assert.AreEqual(25, parsedH2);
        Assert.IsFalse(doneH2);
    }

    // Good: Multiple values
    [TestMethod]
    public void MultipleValuesForHeader()
    {
        Headers headers = new();
        headers.Parse("Host: localhost:42069\r\n\r\n"u8.ToArray());
        headers.Parse("Host: www.test.com\r\n\r\n"u8.ToArray());

        Assert.AreEqual("localhost:42069, www.test.com", headers.Get("Host"));
    }

    // Good: End of headers
    [DataRow("\r\n")]
    [TestMethod]
    public void EndOfHeaderBlock(string data)
    {
        var (_, done) = new Headers().Parse(Encoding.UTF8.GetBytes(data));
        Assert.IsTrue(done);
    }

    // Good: Case sensitivity
    [DataRow("host: localhost:42069\r\n\r\n", "Host", "localhost:42069")]
    [DataRow("Host: localhost:42069\r\n\r\n", "Host", "localhost:42069")]
    [DataRow("User-Agent: curl/7.81.0\r\n\r\n", "user-agent", "curl/7.81.0")]
    [DataRow("USER-AGENT: curl/7.81.0\r\n\r\n", "user-agent", "curl/7.81.0")]
    [TestMethod]
    public void CaseSensitivity(string data, string key, string value)
    {
        Headers headers = new();
        headers.Parse(Encoding.UTF8.GetBytes(data));
        Assert.AreEqual(value, headers.Get(key));
    }

    // Bad: Invalid characters in header name
    [DataRow("Ho=st: localhost:42069\r\n\r\n")]
    [DataRow("Host,Host2: localhost:42069\r\n\r\n")]
    [DataRow(";[]{}\\?/<>,(): localhost:42069\r\n\r\n")]
    [DataRow(";: localhost:42069\r\n\r\n")]
    [DataRow("]: localhost:42069\r\n\r\n")]
    [DataRow("{: localhost:42069\r\n\r\n")]
    [DataRow("}: localhost:42069\r\n\r\n")]
    [DataRow("\\: localhost:42069\r\n\r\n")]
    [DataRow("?: localhost:42069\r\n\r\n")]
    [DataRow("/: localhost:42069\r\n\r\n")]
    [DataRow("<: localhost:42069\r\n\r\n")]
    [DataRow(",: localhost:42069\r\n\r\n")]
    [DataRow("(: localhost:42069\r\n\r\n")]
    [DataRow("): localhost:42069\r\n\r\n")]
    [DataRow("\": localhost:42069\r\n\r\n")]
    [TestMethod]
    public void InvalidHeaderCharacters(string data)
    {
        Headers headers = new();
        TestForException((() => headers.Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Good: Special characters in header name
    [DataRow("!#$%&'*+-.^_`|~0123456789: localhost:42069\r\n\r\n")]
    [TestMethod]
    public void SpecialCharacters(string data)
    {
        Headers headers = new();
        headers.Parse(Encoding.UTF8.GetBytes(data));
    }

    // Bad: Invalid spacing
    [DataRow("Host : localhost:42069\r\n\r\n")]
    [DataRow(" Host\t: localhost:42069 \r\n\r\n")]
    [DataRow("            Host : localhost:42069           \r\n\r\n")]
    [TestMethod]
    public void BadSpacing(string data)
    {
        TestForException((() => new Headers().Parse(Encoding.UTF8.GetBytes(data))));
    }

    // Bad: Missing header name
    [DataRow(":localhost:42069\r\n\r\n")]
    [DataRow(": localhost:42069\r\n\r\n")]
    [DataRow(" : localhost:42069\r\n\r\n")]
    [DataRow(" :localhost:42069\r\n\r\n")]
    [TestMethod]
    public void MissingHeaderName(string data)
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