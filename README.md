# httpfromtcp

Simple HTTP server implementing https://datatracker.ietf.org/doc/html/rfc9112.

## Usage
```C#
Server server = new(port: 42000);
server.Serve();
server.Handle(Http.Method.Get, "/json", (_) => Task.FromResult(new Response()
{
    StatusCode = Http.StatusCode._200,
    Headers = new Headers(new Dictionary<string, string>()
        {
            { "Content-Type", "application/json" }
        }
    ),
    Body = """{"test": "value"}"""u8.ToArray()
}));
```

## Sending binary response

Video file as a response.
```C#
server.Handle(Http.Method.Get, "/video", (_) =>
{
    return Task.FromResult(new Response()
    {
        StatusCode = Http.StatusCode._200,
        Headers = new Headers(new Dictionary<string, string>()
            {
                { "Content-Type", "video/mp4" }
            }
        ),
        Body = File.ReadAllBytes("./assets/test.mp4")
    });
});
```

Png file as a response.
```C#
server.Handle(Http.Method.Get, "/png", (_) =>
{
    return Task.FromResult(new Response()
    {
        StatusCode = Http.StatusCode._200,
        Headers = new Headers(new Dictionary<string, string>()
            {
                { "Content-Type", "image/png" }
            }
        ),
        Body = File.ReadAllBytes("./assets/test.png")
    });
});
```
