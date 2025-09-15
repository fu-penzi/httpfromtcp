using System.Text;

namespace httpfromtcp;

public class Headers
{
    public Dictionary<string, string> Data { get; } = [];

    public (int read, bool done) Parse(Span<byte> data)
    {
        byte[] separator = Encoding.UTF8.GetBytes(Constants.Separator);
        int retIdx = data.IndexOf(separator);
        switch (retIdx)
        {
            case -1:
                return (0, false);
            case 0:
                return (separator.Length, true);
        }

        int read = retIdx + separator.Length;

        string header = Encoding.UTF8.GetString(data[..retIdx]);
        if (char.IsWhiteSpace(header[0]) || char.IsWhiteSpace(header[^1]))
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Unnecessary trailing or leading whitespace.");
        }

        string[] parts = header.Split(':', 2);
        if (parts.Length < 2)
        {
            throw new IncorrectFormatException($"Incorrect header format {header}");
        }

        string name = parts[0].Trim();
        string value = parts[1].Trim();
        if (name.Length == 0 || value.Length == 0)
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Empty header name of value.");
        }

        if (name.Length != parts[0].Length)
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Unnecessary whitespace before ':'.");
        }

        if (value.Length == parts[1].Length)
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. No space after ':'.");
        }

        if (!Data.TryAdd(name, value))
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Duplicated header {name}.");
        }

        return (read, false);
    }
}