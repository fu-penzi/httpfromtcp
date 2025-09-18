using System.Text;

namespace httpfromtcp.Server;

/// <summary>
/// StatusLine<br/>
/// <b>status-line = HTTP-version SP status-code SP [ reason-phrase ]</b>
/// </summary>
public record StatusLine
{
    public required Http.StatusCode StatusCode   { get; init; }
    public          string          HttpVersion  { get; init; } = "1.1";
    public          string          ReasonPhrase { get; }

    public StatusLine()
    {
        ReasonPhrase = Http.StatusCodeMessage(StatusCode);
    }

    public override string ToString()
    {
        return $"HTTP/{HttpVersion} {Convert.ChangeType(StatusCode, StatusCode.GetTypeCode())} {ReasonPhrase}\r\n";
    }
}

public class Response
{
    public Http.StatusCode StatusCode { get; init; } = Http.StatusCode._200;
    public Headers         Headers    { get; init; } = new();
    public byte[]          Body       { get; init; } = [];

    internal static Response GetNotFoundResponse()
    {
        return new Response()
        {
            StatusCode = Http.StatusCode._404,
            Headers =
                new Headers(new Dictionary<string, string>()
                    { { "Content-Type", "text/html" } }),
            Body = """
                   <html>
                     <head>
                       <title>404 Not Found</title>
                     </head>
                     <body>
                       <h1>404 - Page not found</h1>
                     </body>
                   </html>
                   """u8.ToArray()
        };
    }

    public void WriteStatusLine(Stream stream)
    {
        stream.Write(Encoding.UTF8.GetBytes(new StatusLine() { StatusCode = StatusCode }.ToString()));
    }

    public void WriteHeaders(Stream stream)
    {
        stream.Write(Encoding.UTF8.GetBytes(Headers + "\r\n"));
    }

    public void WriteBody(Stream stream)
    {
        stream.Write(Body);
    }

    public void AddDefaultHeaders()
    {
        Headers.Add("Content-Length", $"{Body.Length}");
        Headers.Add("Connection", "close");
        if (!Headers.TryGetValue("content-type", out _))
        {
            Headers.Add("Content-Type", "text/plain");
        }
    }
}