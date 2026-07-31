using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MabinogiMobileServer
{
    class NetworkManager : IDisposable
    {
        private static NetworkManager _inst;
        public static NetworkManager NetworkManagerInstance
        {
            get
            {
                if (_inst is null)
                    _inst = new NetworkManager();
                return _inst;
            }
        }

        Listener listener = Listener.Instance;
        Dictionary<int, Socket> ClientList = new Dictionary<int, Socket>();

        private NetworkManager() { }

        public void RunServer() => listener.Run();

        public void AcceptClient()
        {
            Socket? client = listener.AcceptClient();
            if (client is not null)
            {
                ClientList.Add(ClientList.Count, client);
            }
        }

        public void ReadData()
        {
            foreach (var item in ClientList)
            {
                if (item.Value.Available > 0)
                {
                    byte[] buffer = new byte[4];
                    using (NetworkStream ns = new NetworkStream(item.Value))
                    {
                        ns.Read(buffer, 0, 4);
                    }

                    float data = BitConverter.ToSingle(buffer);
                    SendData(buffer, item.Key);
                }
            }
        }

        public void SendData(byte[] Buffer, int SenderID)
        {
            // send data all client
            foreach (var item in ClientList)
            {
                if (item.Key != SenderID)
                {
                    using (NetworkStream ns = new NetworkStream(item.Value))
                    {
                        ns.Write(Buffer, 0, Buffer.Length);
                    }
                }
            }
        }

        public void Dispose()
        {
            // end
            foreach (var item in ClientList)
                item.Value.Close();
        }


        // Listener class
        private class Listener : IDisposable
        {
            private static Listener? _inst = null;
            public static Listener Instance
            {
                get
                {
                    if (_inst is null)
                        _inst = new Listener();
                    return _inst;
                }
            }

            private Socket? ListenSocket = null;
            private const int PortNumber = 33355;

            private Listener()
            {
                try
                {
                    IPEndPoint LocalAddress = new IPEndPoint(address: IPAddress.Any, PortNumber);
                    if (LocalAddress is null)
                        throw new Exception("Address is null");

                    ListenSocket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                    if (ListenSocket is null)
                        throw new Exception("Listener is null");

                    ListenSocket.Bind(LocalAddress);
                }
                catch (Exception e)
                {
                    e.OutputExceptionLog();
                }
            }

            public void Run() => ListenSocket?.Listen(100);

            public void Dispose()
            {
                ListenSocket?.Close();
            }

            public Socket? AcceptClient()
            {
                Socket? client = null;

                // client connected
                if (ListenSocket?.Available > 0)
                {
                    client = ListenSocket.Accept();
                    Console.WriteLine($"client connected!");
                }

                return client;
            }
        }
    }
}
