using System;
using System.Linq;
using System.Net.Sockets;

namespace MabinogiMobileServer
{
    class Player : IDisposable
    {
        public static int SerializeSize
        {
            get
            {
                return sizeof(int) + sizeof(float) * TransformCount;
            }
        }

        public required int PlayerID { get; init; }

        private const int TransformCount = 7;
        public float[] Transform { get; set; } = null!;

        public required Socket sock { get; init; }

        public void SerializePlayerInfo(Span<byte> buffer)
        {
            int offset = 0;

            // serialize player ID
            BitConverter.TryWriteBytes(buffer.Slice(offset, sizeof(int)), PlayerID);
            offset += sizeof(int);

            // serialize transform
            foreach (float value in Transform)
            {
                BitConverter.TryWriteBytes(buffer.Slice(offset, sizeof(float)), value);
                offset += sizeof(float);
            }
        }

        public void Dispose()
        {
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();
        }
    }
}
