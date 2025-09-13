using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener listener = new(IPAddress.Parse("127.0.0.1"), 42069);

try
{
    listener.Start();
    using TcpClient handler = listener.AcceptTcpClient();
    await using NetworkStream stream = handler.GetStream();

    foreach (var line in GetLines(stream))
    {
        Console.WriteLine($"READ: {line}");
    }
}
catch
{
    listener.Stop();
    throw;
}


IEnumerable<string> GetLines(Stream steam)
{
    var currenLine = new StringBuilder();
    while (true)
    {
        byte[] buff = new byte[8];
        int read = steam.Read(buff);
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
            Console.WriteLine($"READ: {currenLine}{lines[0]}");
            currenLine.Clear();

            foreach (var line in lines[1..^1])
            {
                yield return line;
            }
        }
        currenLine.Append(lines[^1]);
    }
}