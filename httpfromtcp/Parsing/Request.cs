using System.Text;

namespace httpfromtcp.Parsing;

internal enum ParsingState
{
    Initialized,
    ParsingHeaders,
    ParsingBody,
    Done
}

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
    public RequestLine RequestLine;
    public readonly Headers Headers = new();
    public byte[] Body = [];

    private ParsingState _state = ParsingState.Initialized;
    private bool Done => _state == ParsingState.Done;

    /// <summary>
    /// Loop over stream of data until whole request is parsed.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <param name="length">Max length of parsing buffer.</param>
    /// <returns>Parsed request</returns>
    public static Request FromStream(Stream stream, int length = 1024)
    {
        Request request = new();
        int buffLen = 0;
        byte[] buff = new byte[length];
        while (!request.Done)
        {
            if (buffLen == buff.Length)
            {
                byte[] newBuff = new byte[buff.Length * 2];
                Array.Copy(buff, newBuff, buff.Length);
                buff = newBuff;
            }

            buffLen += stream.Read(buff, buffLen, buff.Length - buffLen);

            int parsedBytes = request.Parse(buff.AsSpan()[..buffLen]);
            if (parsedBytes != 0)
            {
                buffLen -= parsedBytes;
                // Remove parsed elements from buffer
                Array.Copy(buff, parsedBytes, buff, 0, buffLen);
            }
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
        int contentLength = 0;

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

                bool hasBody = Headers.TryGetValue("Content-Length", out _);
                _state = hasBody
                    ? ParsingState.ParsingBody
                    : ParsingState.Done;

                return parsed;
            }
            case ParsingState.ParsingBody:
            {
                if (!Headers.TryGetValue("Content-Length", out var contentLengthValue))
                {
                    throw new IncorrectFormatException("Missing Content-Length");
                }

                if (!int.TryParse(contentLengthValue, out contentLength))
                {
                    throw new IncorrectFormatException($"Invalid Content-Length value: {contentLengthValue}");
                }

                if (Body.Length != contentLength)
                {
                    return ParseBody(data);
                }

                _state = ParsingState.Done;
                return 0;
            }
            case ParsingState.Done:
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
        // Split by SP
        string[] requestLineParts = requestLine.Split(" ");
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
        RequestLine = new RequestLine(method, requestTarget, httpVersion);

        return read;
    }

    private int ParseBody(Span<byte> data)
    {
        Body = data.ToArray();
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