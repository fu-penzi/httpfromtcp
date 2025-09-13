using System.Text;

namespace httpfromtcp;

public struct Request
{
    RequestLine RequestLine;
}
public struct RequestLine
{
    string HttpVersion;
    string RequestTarget;
    string Method;
}

public class HttpServer
{

    public static IEnumerable<string> GetLines(Stream stream)
    {
        var currenLine = new StringBuilder();
        while (true)
        {
            byte[] buff = new byte[8];
            int read = stream.Read(buff);
            if (read == 0)
            {
                if (currenLine.Length != 0)
                {
                    yield return currenLine.ToString();
                }
                break;
            }

            string str = Encoding.Default.GetString(buff[..read]);
            string[] lines = str.Split("\n");
            if (lines.Length > 1)
            {
                yield return $"{currenLine}{lines[0]}";
                currenLine.Clear();

                foreach (var line in lines[1..^1])
                {
                    yield return line;
                }
            }
            currenLine.Append(lines[^1]);
        }
    }
}
