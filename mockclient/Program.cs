using System.Net;
using System.Net.Sockets;
using System.Text;

IPEndPoint ipEndPoint = new(IPAddress.Parse("127.0.0.1"), 42069);

using TcpClient client = new();
await client.ConnectAsync(ipEndPoint);
await using NetworkStream stream = client.GetStream();

await stream.WriteAsync(Encoding.UTF8.GetBytes("Do you have what it takes to be an engineer at TheStartup™?\r\nAre you willing to work 80 hours a week in hopes that your 0.001% equity is worth something?\r\nCan you say \"synergy\" and \"democratize\" with a straight face?\r\nAre you prepared to eat top ramen at your desk 3 meals a day?\r\nend"));