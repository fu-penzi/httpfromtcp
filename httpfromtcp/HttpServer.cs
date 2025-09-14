using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;

namespace httpfromtcp;

[Serializable]
public class IncorrectFormatException : Exception
{
    public IncorrectFormatException() { }
    public IncorrectFormatException(string message) : base(message) { }
    public IncorrectFormatException(string message, Exception inner) : base(message, inner) { }
}

public record struct RequestLine(string Method, string RequestTarget, string HttpVersion);
public record struct Request(RequestLine RequestLine)
{
    public static readonly byte[] Separator = Encoding.UTF8.GetBytes("\r\n");
    public readonly bool Done => _state == ParsingState.DONE;

    enum ParsingState { INITIALIZED, DONE }
    ParsingState _state = ParsingState.INITIALIZED;

    public int Parse(Span<byte> data)
    {
        int read = 0;
        switch (_state)
        {
            case ParsingState.INITIALIZED:
                int n = ParseRequestLine(data);
                if (n == 0)
                {
                    return 0;
                }
                read += n;
                _state = ParsingState.DONE;

                return read;
            case ParsingState.DONE:
                return 0;
        }
        return 0;
    }

    int ParseRequestLine(Span<byte> data)
    {
        string str = Encoding.UTF8.GetString(data);
        int retIdx = data.IndexOf(Separator);
        if (retIdx == -1)
        {
            return 0;
        }
        int read = retIdx + Separator.Length;

        string requestLine = str[..retIdx];
        string[] requestLineParts = requestLine.Split();
        if (requestLineParts.Length < 3)
        {
            throw new IncorrectFormatException($"Incorrect request-line format {requestLine}");
        }

        string Method = requestLineParts[0];
        string RequestTarget = requestLineParts[1];
        string HttpVersion = requestLineParts[2].Split("/") switch
        {
            [_, var version] => version == "1.1" ? version
            : throw new IncorrectFormatException($"Unsupported HTTP version {version}"),
            _ => throw new IncorrectFormatException($"Incorrect request-line format {requestLine}"),
        };
        RequestLine = new RequestLine(Method, RequestTarget, HttpVersion);

        return read;
    }

}

public class HttpServer
{
    public Request RequestFromReader(Stream stream, int length = 1024)
    {
        Request r = new();
        int buffLen = 0;
        byte[] buff = new byte[length];
        while (!r.Done)
        {
            if (buffLen == buff.Length)
            {
                byte[] newBuff = new byte[buff.Length * 2];
                Array.Copy(buff, newBuff, buff.Length);
                buff = newBuff;
            }

            buffLen += stream.Read(buff, buffLen, 3);

            int parsedBytes = r.Parse(buff.AsSpan()[..buffLen]);
            if (parsedBytes != 0)
            {
                buffLen -= parsedBytes;
                // Remove parsed elements from buffer
                Array.Copy(buff, parsedBytes, buff, 0, buffLen);
            }
        }
        return r;
    }

    //public static IEnumerable<string> GetLines(Stream stream)
    //{
    //    var currenLine = new StringBuilder();
    //    while (true)
    //    {
    //        byte[] buff = new byte[8];
    //        int read = stream.Read(buff);
    //        if (read == 0)
    //        {
    //            if (currenLine.Length != 0)
    //            {
    //                yield return currenLine.ToString();
    //            }
    //            break;
    //        }

    //        string str = Encoding.Default.GetString(buff[..read]);
    //        string[] lines = str.Split("\n");
    //        if (lines.Length > 1)
    //        {
    //            yield return $"{currenLine}{lines[0]}";
    //            currenLine.Clear();

    //            foreach (var line in lines[1..^1])
    //            {
    //                yield return line;
    //            }
    //        }
    //        currenLine.Append(lines[^1]);
    //    }
    //}
}
