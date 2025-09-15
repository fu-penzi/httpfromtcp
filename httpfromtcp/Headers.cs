using System.Text;
using System.Text.RegularExpressions;

namespace httpfromtcp;

public partial class Headers
{
    private readonly Dictionary<string, string> _data = [];


    // A field-name must contain only:
    // Uppercase letters: A-Z
    // Lowercase letters: a-z
    // Digits: 0-9
    // Special characters: !, #, $, %, &, ', *, +, -, ., ^, _, `, |, ~
    [GeneratedRegex(@"^[A-Za-z0-9!#$%&'*+\-.^_`|~]+$")]
    private static partial Regex HeaderNameRegex();

    public string Get(string key)
    {
        return _data[key.ToLower()];
    }

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

        string name = parts[0].Trim().ToLower();
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


        if (!HeaderNameRegex().IsMatch(name))
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Invalid characters in header name.");
        }

        if (!_data.TryAdd(name, value))
        {
            _data[name] += $", {value}";
        }

        return (read, false);
    }
}