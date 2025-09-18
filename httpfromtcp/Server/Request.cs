using System.Text;

namespace httpfromtcp.Server;

/// <summary>
/// RequestLine<br/>
/// <br/>
/// <b>request-line CRLF</b><br/><br/>
/// <i>request-line  = method SP request-target SP HTTP-version</i>
/// </summary>
public record RequestLine(
    string Method        = "",
    string RequestTarget = "",
    string HttpVersion   = ""
)
{
    public override string ToString()
    {
        return $"{Method} {RequestTarget} HTTP/{HttpVersion}\r\n";
    }
};

/// <summary>
/// Request parsed in format:
/// <list type="bullet">
/// <item>RequestLine:<br/><b>request-line CRLF</b></item>
/// <item>Headers:<br/><b>*( field-line CRLF )<br/>CRLF</b></item>
/// <item>Body:<br/><b>[ message-body ]</b></item>
/// </list>
/// </summary>
public class Request
{
    public RequestLine RequestLine { get; private set; } = new();
    public Headers     Headers     { get; }              = new();
    public List<byte>  Body        { get; }              = [];
    public Exception?  Error       { get; private set; }

    private enum ParsingState
    {
        Initialized,
        ParsingHeaders,
        ParsingBody,
        Error,
        Done
    }

    private bool         Done => _state is ParsingState.Done or ParsingState.Error;
    private ParsingState _state = ParsingState.Initialized;


    /// <summary>
    /// Loop over stream of data until whole request is parsed.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="initialBuff">Initial length of parsing buffer.</param>
    /// <returns>Parsed request</returns>`
    public static Request FromStream(Stream stream, int initialBuff = 2048)
    {
        Reader reader = new(stream);
        Request request = new();
        try
        {
            int buffLen = 0;
            byte[] buff = new byte[initialBuff];
            while (!request.Done)
            {
                if (buffLen == buff.Length)
                {
                    byte[] newBuff = new byte[buff.Length * 2];
                    Array.Copy(buff, newBuff, buff.Length);
                    buff = newBuff;
                }

                int readBytes = reader.DataAvailable
                    ? reader.Read(buff, buffLen, buff.Length - buffLen)
                    : 0;

                buffLen += readBytes;

                int parsedBytes = request.Parse(buff.AsSpan()[..buffLen]);
                if (parsedBytes > 0)
                {
                    buffLen -= parsedBytes;
                    Array.Copy(buff, parsedBytes, buff, 0, buffLen); // Remove parsed elements from buffer
                }
            }
        }
        catch (Exception e)
        {
            request._state = ParsingState.Error;
            request.Error = e;
            return request;
        }

        return request;
    }

    /// <summary>
    /// Parse data into request.<br/>
    /// </summary>
    /// <param name="data">Text data to parse.</param>
    /// <returns>Number of parsed bytes. 0 if not enough data to parse.</returns>
    private int Parse(Span<byte> data)
    {
        switch (_state)
        {
            case ParsingState.Initialized:
            {
                int parsed = ParseRequestLine(data);
                if (parsed != 0)
                {
                    _state = ParsingState.ParsingHeaders;
                }
                return parsed;
            }
            case ParsingState.ParsingHeaders:
            {
                var (parsed, done) = Headers.Parse(data);
                if (!done)
                {
                    return parsed;
                }
                if (!Headers.TryGetValue("Content-Length", out var contentLengthValue))
                {
                    _state = ParsingState.Done;
                    return parsed;
                }
                if (!int.TryParse(contentLengthValue, out var contentLength))
                {
                    throw new IncorrectFormatException($"Invalid Content-Length value: {contentLengthValue}");
                }
                _state = contentLength > 0
                    ? ParsingState.ParsingBody
                    : ParsingState.Done;
                return parsed;
            }
            case ParsingState.ParsingBody:
            {
                int contentLength = int.Parse(Headers.Get("Content-Length"));
                if (Body.Count < contentLength && data.Length == 0)
                {
                    throw new IncorrectFormatException(
                        $"Content-Length mismatch. Body shorter than Content-Length.");
                }

                int parsed = ParseBody(data);
                if (Body.Count < contentLength)
                {
                    return parsed;
                }
                if (Body.Count == contentLength)
                {
                    _state = ParsingState.Done;
                    return parsed;
                }
                if (Body.Count > contentLength)
                {
                    throw new IncorrectFormatException(
                        $"Content-Length mismatch. Body longer than Content-Length.");
                }
                break;
            }
            default:
            {
                return 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Parse request line part.<br/>
    /// </summary>
    /// <param name="data">Text to parse from.</param>
    /// <returns></returns>
    /// <exception cref="IncorrectFormatException"></exception>
    private int ParseRequestLine(Span<byte> data)
    {
        byte[] separator = Encoding.UTF8.GetBytes(Constants.Separator);
        int retIdx = data.IndexOf(separator);
        if (retIdx == -1)
        {
            return 0;
        }
        int read = retIdx + separator.Length;

        string str = Encoding.UTF8.GetString(data);
        string requestLine = str[..retIdx];
        string[] requestLineParts = requestLine.Split(" "); // Split by SP
        if (requestLineParts.Length < 3)
        {
            throw new IncorrectFormatException($"Incorrect request-line format {requestLine}");
        }

        string method = requestLineParts[0];
        string requestTarget = requestLineParts[1];
        string httpVersion = requestLineParts[2].Split("/") switch
        {
            [_, var version] => version == "1.1"
                ? version
                : throw new IncorrectFormatException($"Unsupported HTTP version {version}"),
            _ => throw new IncorrectFormatException($"Incorrect request-line format {requestLine}"),
        };
        RequestLine = new RequestLine
        {
            Method = method,
            RequestTarget = requestTarget,
            HttpVersion = httpVersion
        };
        return read;
    }

    private int ParseBody(Span<byte> data)
    {
        Body.AddRange(data);
        return data.Length;
    }
}

[Serializable]
public class IncorrectFormatException : Exception
{
    public IncorrectFormatException()
    {
    }

    public IncorrectFormatException(string message) : base(message)
    {
    }

    public IncorrectFormatException(string message, Exception inner) : base(message, inner)
    {
    }
}