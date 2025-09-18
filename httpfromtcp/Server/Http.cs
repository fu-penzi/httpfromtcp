namespace httpfromtcp.Server;

public static class Http
{
    public enum Method
    {
        Get,
        Put,
        Post,
        Delete,
        Invalid
    }

    public enum StatusCode
    {
        _200 = 200,
        _400 = 400,
        _404 = 404,
        _500 = 500
    }

    public static Method ParseMethod(string httpMethod)
    {
        return httpMethod switch
        {
            "GET"    => Method.Get,
            "POST"   => Method.Post,
            "PUT"    => Method.Put,
            "DELETE" => Method.Delete,
            _        => Method.Invalid
        };
    }

    public static string StatusCodeMessage(Http.StatusCode statusCode)
    {
        return statusCode switch
        {
            Http.StatusCode._200 => "OK",
            Http.StatusCode._400 => "Bad request",
            Http.StatusCode._404 => "Not found",
            Http.StatusCode._500 => "Internal Server Error",
            _                    => ""
        };
    }
}