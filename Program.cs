using System.Text;

string path = "C:\\PROJEKTY\\httpfromtcp\\httpfromtcp\\messages.txt";

try
{
    var currenLine = new StringBuilder();
    using (FileStream fs = File.OpenRead(path))
    {
        byte[] buff = new byte[8];

        for (int read = 0; (read = fs.Read(buff)) > 0;)
        {
            string str = Encoding.Default.GetString(buff[..read]);
            string[] lines = str.Split("\n");
            if (lines.Length > 1)
            {
                Console.WriteLine($"READ: {currenLine}{lines[0]}");
                currenLine.Clear();
                foreach (var line in lines[1..^1])
                {
                    Console.WriteLine($"READ: {line}");
                }
            }
            currenLine.Append(lines[^1]);
        }
    }

    if (currenLine.Length != 0)
    {
        Console.WriteLine($"READ: {currenLine}");
    }
}
catch (IOException e)
{
    Console.WriteLine($"Err: {e.Message}\n");
    return;
}