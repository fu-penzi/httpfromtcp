using httpfromtcp.Server;

Server server = new(port: 42069);

server.Serve();
server.Close();
server.Serve();
server.Close();
server.Serve();
server.Close();
server.Serve();
while (true) ;