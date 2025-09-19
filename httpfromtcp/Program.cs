using httpfromtcp.Server;

Server server = new(port: 42000);

server.Serve();

server.Handle(Http.Method.Get, "/", (request) =>
{
    if (request.Error is not null)
    {
        Console.WriteLine(request.Error);
        return Task.FromResult(new Response()
        {
            StatusCode = Http.StatusCode._400,
            Headers = new Headers(new Dictionary<string, string>()
                {
                    { "Content-Type", "text/html" }
                }
            ),
            Body = """
                   <html>
                     <head>
                       <title>400 Bad Request</title>
                     </head>
                     <body>
                       <h1>Bad Request</h1>
                     </body>
                   </html>
                   """u8.ToArray()
        });
    }

    return Task.FromResult(new Response()
    {
        StatusCode = Http.StatusCode._200,
        Headers = new Headers(new Dictionary<string, string>()
            {
                { "Content-Type", "text/html" }
            }
        ),
        Body = """
               <html>
                 <head>
                   <title>200 OK</title>
                 </head>
                 <body>
                   <h1>Success!</h1>
                 </body>
               </html>
               """u8.ToArray()
    });
});


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

server.Handle(Http.Method.Get, "/video", (_) =>
{
    byte[] file = File.ReadAllBytes("./assets/test.mp4");

    return Task.FromResult(new Response()
    {
        StatusCode = Http.StatusCode._200,
        Headers = new Headers(new Dictionary<string, string>()
            {
                { "Content-Type", "video/mp4" }
            }
        ),
        Body = file
    });
});

server.Handle(Http.Method.Get, "/png", (_) =>
{
    byte[] file = File.ReadAllBytes("./assets/test.png");

    return Task.FromResult(new Response()
    {
        StatusCode = Http.StatusCode._200,
        Headers = new Headers(new Dictionary<string, string>()
            {
                { "Content-Type", "image/png" }
            }
        ),
        Body = file
    });
});

while (true) ;