using System;
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

        [Obsolete("temp code", false)]
        private const int TransformCount = 7;
        public float[] Transform { get; set; } = new float[TransformCount];

        public required Socket ClientSocket { get; init; }

        public void SerializePlayerInfo(Span<byte> buffer)
        {
            int position = 0;

            // serialize player ID
            BitConverter.TryWriteBytes(buffer.Slice(position, sizeof(int)), PlayerID);
            position += sizeof(int);

            // serialize transform
            foreach (float value in Transform)
            {
                BitConverter.TryWriteBytes(buffer.Slice(position, sizeof(float)), value);
                position += sizeof(float);
            }
        }

        public void Dispose()
        {
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }
    }
}
