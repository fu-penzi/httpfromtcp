using System.Text;

namespace httpfromtcp.Server;

public enum StatusCode
{
    _200 = 200,
    _400 = 400,
    _500 = 500
}

/// <summary>
/// StatusLine<br/>
/// <b>status-line = HTTP-version SP status-code SP [ reason-phrase ]</b>
/// </summary>
public record StatusLine(
    string     HttpVersion,
    StatusCode StatusCode,
    string     ReasonPhrase
)
{
    public override string ToString()
    {
        return $"HTTP/{HttpVersion} {Convert.ChangeType(StatusCode, StatusCode.GetTypeCode())} {ReasonPhrase}\r\n";
    }
}

public class Response
{
    public StatusCode StatusCode { get; init; } = StatusCode._200;
    public Headers    Headers    { get; init; } = new();
    public byte[]     Body       { get; init; } = [];

    internal void WriteStatusLine(Stream stream)
    {
        StatusLine statusLine = new
        (
            HttpVersion: "1.1",
            StatusCode: StatusCode,
            ReasonPhrase: StatusCodeMessage(StatusCode)
        );
        stream.Write(Encoding.UTF8.GetBytes(statusLine.ToString()));
    }

    internal void WriteHeaders(Stream stream)
    {
        stream.Write(Encoding.UTF8.GetBytes(Headers + "\r\n"));
    }

    internal void WriteBody(Stream stream)
    {
        stream.Write(Body);
    }

    private static string StatusCodeMessage(StatusCode statusCode)
    {
        return statusCode switch
        {
            StatusCode._200 => "OK",
            StatusCode._400 => "Bad request",
            StatusCode._500 => "Internal Server Error",
            _               => ""
        };
    }

    internal void AddDefaultHeaders(int contentLength)
    {
        Headers.Add("Content-Length", $"{contentLength}");
        Headers.Add("Connection", "close");
        if (!Headers.Data.ContainsKey("Content-Type"))
        {
            Headers.Add("Content-Type", "text/plain");
        }
    }
}