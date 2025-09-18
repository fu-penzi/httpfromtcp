using httpfromtcp.Server;

Server server = new(port: 42069);

server.Serve();

server.Handle((request) =>
{
    if (request.Error is not null)
    {
        return new Response()
        {
            StatusCode = StatusCode._400,
            Headers = new Headers(new()
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
        };
    }

    // Console.WriteLine(
    //     $"""
    //
    //      Request line:
    //      {request.RequestLine}
    //      """
    // );
    // Console.WriteLine(
    //     $"Headers:\n{request.Headers}\n" +
    //     $"Body:\n{Encoding.UTF8.GetString(request.Body.ToArray())}\n" +
    //     $"Error:\n{request.Error}");

    return new Response()
    {
        StatusCode = StatusCode._200,
        Headers = new Headers(new()
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
    };
});

while (true) ;