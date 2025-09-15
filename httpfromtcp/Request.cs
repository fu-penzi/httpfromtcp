using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace httpfromtcp;
enum ParsingState { INITIALIZED, DONE }

public record struct RequestLine(
    string Method,
    string RequestTarget,
    string HttpVersion
);

public class Request
{
    public RequestLine RequestLine;

    public bool Done => _state == ParsingState.DONE;
    ParsingState _state = ParsingState.INITIALIZED;

    public static Request FromStream(Stream stream, int length = 1024)
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



    int Parse(Span<byte> data)
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
        byte[] separator = Encoding.UTF8.GetBytes(Constants.Separator);
        int retIdx = data.IndexOf(separator);
        if (retIdx == -1)
        {
            return 0;
        }
        int read = retIdx + separator.Length;

        string str = Encoding.UTF8.GetString(data);
        string requestLine = str[..retIdx];
        string[] requestLineParts = requestLine.Split(" ");
        if (requestLineParts.Length < 3)
        {
            throw new IncorrectFormatException($"Incorrect request-line format {requestLine}");
        }

        string method = requestLineParts[0];
        string requestTarget = requestLineParts[1];
        string httpVersion = requestLineParts[2].Split("/") switch
        {
            [_, var version] => version == "1.1" ? version
            : throw new IncorrectFormatException($"Unsupported HTTP version {version}"),
            _ => throw new IncorrectFormatException($"Incorrect request-line format {requestLine}"),
        };
        RequestLine = new RequestLine(method, requestTarget, httpVersion);
            
        
        return read;
    }

}


[Serializable]
public class IncorrectFormatException : Exception
{
    public IncorrectFormatException() { }
    public IncorrectFormatException(string message) : base(message) { }
    public IncorrectFormatException(string message, Exception inner) : base(message, inner) { }
}
