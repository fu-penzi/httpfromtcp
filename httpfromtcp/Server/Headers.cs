using System.Text;
using System.Text.RegularExpressions;

namespace httpfromtcp.Server;

/// <summary>
/// Headers:<br/><br/>
/// <b>*( field-line CRLF )<br/>
/// CRLF<br/></b>
/// <br/>
/// <i>field-line   = field-name ":" OWS field-value OWS</i>
/// </summary>
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

    public IReadOnlyDictionary<string, string> Data => _data.AsReadOnly();

    public string Get(string key) => _data[key.ToLower()];

    public bool TryGetValue(string key, out string? value)
    {
        return _data.TryGetValue(key.ToLower(), out value);
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var kvp in _data)
        {
            builder.Append($"- {kvp.Key}: {kvp.Value}\n");
        }
        return builder.ToString();
    }

    /// <summary>
    /// Parse headers part of request<br/>
    /// </summary>
    /// <param name="data">Text data to parse.</param>
    /// <returns><b>read</b> - Number of bytes parsed. 0 if needs more data.<br/>
    /// <b>done</b> - True if all headers parsed.</returns>
    /// <exception cref="IncorrectFormatException"></exception>
    public (int read, bool done) Parse(Span<byte> data)
    {
        byte[] separator = Encoding.UTF8.GetBytes(Constants.Separator);
        int retIdx = data.IndexOf(separator);
        switch (retIdx)
        {
            case -1:
                return (0, false);
            case 0: // CRLF marks end of Headers lines
                return (separator.Length, true);
        }

        int read = retIdx + separator.Length;
        string header = Encoding.UTF8.GetString(data[..retIdx]).Trim(); // Trim OWS
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
        if (name.Length != parts[0].Length) // Forbidden whitespace before colon
        {
            throw new IncorrectFormatException(
                $"Incorrect header format {header}. Unnecessary whitespace before ':'.");
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