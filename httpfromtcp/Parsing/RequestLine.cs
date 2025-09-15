namespace httpfromtcp.Parsing;

/// <summary>
/// RequestLine<br/>
/// <br/>
/// <b>request-line CRLF</b><br/><br/>
/// <i>request-line  = method SP request-target SP HTTP-version</i>
/// </summary>
public record struct RequestLine(
    string Method,
    string RequestTarget,
    string HttpVersion
);