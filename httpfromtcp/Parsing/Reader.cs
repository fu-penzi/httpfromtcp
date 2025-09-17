using System.Net.Sockets;

namespace httpfromtcp.Parsing;

public interface IReader
{
    int Read(byte[] buffer, int offset, int count);
    bool DataAvailable { get; }
}

public class Reader(Stream stream) : IReader
{
    public int Read(byte[] buffer, int offset, int count)
    {
        return stream.Read(buffer, offset, count);
    }

    public bool DataAvailable
    {
        get
        {
            if (stream is NetworkStream networkStream)
            {
                return networkStream.DataAvailable;
            }
            else
            {
                return true;
            }
        }
    }
}