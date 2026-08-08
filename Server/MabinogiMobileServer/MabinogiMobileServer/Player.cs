using System;
using System.Net.Sockets;

namespace MabinogiMobileServer
{
    class Player : IDisposable
    {
        public int PlayerID = 0;
        public float[] Transform = new float[10];
        public required Socket sock { get; init; }

        public void Dispose()
        {
            sock.Shutdown(SocketShutdown.Both);
            sock.Close();
        }
    }
}
