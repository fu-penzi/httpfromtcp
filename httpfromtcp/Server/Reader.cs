using System.Net.Sockets;

namespace httpfromtcp.Server;

internal class Reader(Stream stream)
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