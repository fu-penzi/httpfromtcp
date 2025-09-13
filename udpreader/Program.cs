using System.Net;
using System.Net.Sockets;
using System.Text;

IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 42069);
using UdpClient udpClient = new(42069);
try
{
    udpClient.Connect(ipEndPoint);
    Console.WriteLine($"Listening on: {ipEndPoint}");
}
catch
{
    Console.WriteLine("Error connecting UDP endpoint");
    throw;
}

while (true)
{
    Console.Write("> ");

    string line;
    try
    {
        line = Console.ReadLine() ?? "";
    }
    catch (IOException e)
    {
        Console.WriteLine($"Error reading input: {e.Message}");
        continue;
    }


    byte[] sendMessage = Encoding.UTF8.GetBytes(line);
    try
    {
        udpClient.Send(sendMessage);
    }
    catch (SocketException)
    {
        Console.WriteLine("Error sending message");
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("Error: Socket not connected");
    }
}
