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
        Dictionary<Player, Socket> ClientList = new Dictionary<Player, Socket>();

        private NetworkManager() { }

        public void RunServer() => listener.Run();

        public void AcceptClient()
        {
            Socket? NewClient = listener.AcceptClient();
            if (NewClient is not null)
            {
                // add client to ClientList
                Player NewPlayer = new Player()
                {
                    PlayerID = new Random().Next(1, 100),
                    yPos = 0
                };
                ClientList.Add(NewPlayer, NewClient);

                // allocate client ID to New client
                SendData(BitConverter.GetBytes(NewPlayer.PlayerID), NewClient);

                // send client info to New client
                byte[] buffer;
                foreach (var item in ClientList)
                {
                    if (item.Key.PlayerID != NewPlayer.PlayerID)
                    {
                        buffer = SerializeData(item.Key.PlayerID, item.Key.yPos);
                        SendData(buffer, NewClient);
                    }
                }

                Console.WriteLine($"New Client connected : {NewPlayer.PlayerID}");

                // broadcast packet that new client connected
                buffer = SerializeData(NewPlayer.PlayerID, NewPlayer.yPos);
                Broadcast(buffer, NewPlayer.PlayerID);
            }
        }

        public void ReadData()
        {
            foreach (var item in ClientList)
            {
                if (item.Value.Available > 0)
                {
                    byte[] buffer = new byte[8];
                    float data = 0.0f;
                    int PlayerID = 0;

                    // read data
                    using (NetworkStream ns = new NetworkStream(item.Value))
                    {
                        ns.Read(buffer, 0, 8);
                    }
                    DeserializeData(buffer, out PlayerID, out data);

                    // edit client info in server
                    foreach (var client in ClientList)
                    {
                        if (client.Key.PlayerID == PlayerID)
                        {
                            client.Key.yPos = data;
                            break;
                        }
                    }

                    // broadcast
                    Broadcast(buffer, item.Key.PlayerID);
                }
            }
        }

        public byte[] SerializeData(int PlayerID, float Data)
        {
            byte[] buffer = new byte[8];

            // serialize player ID
            byte[] SerializeResult = BitConverter.GetBytes(PlayerID);
            Array.Copy(SerializeResult, 0, buffer, 0, SerializeResult.Length);

            // serialize data
            SerializeResult = BitConverter.GetBytes(Data);
            Array.Copy(SerializeResult, 0, buffer, 4, SerializeResult.Length);

            return buffer;
        }

        public void DeserializeData(byte[] buffer, out int PlayerID, out float Data)
        {
            byte[] DeserializeResult = new byte[4];

            // deserialize player ID
            Array.Copy(buffer, 0, DeserializeResult, 0, 4);
            PlayerID = BitConverter.ToInt32(DeserializeResult);

            // deserialize data
            Array.Copy(buffer, 4, DeserializeResult, 0, 4);
            Data = BitConverter.ToSingle(DeserializeResult);
        }

        public void SendData(byte[] Buffer, Socket client)
        {
            using (NetworkStream ns = new NetworkStream(client))
            {
                ns.Write(Buffer, 0, Buffer.Length);
            }
        }

        public void Broadcast(byte[] Buffer, int? ExcludeID = null)
        {
            foreach(var item in ClientList)
            {
                if(ExcludeID is null || (ExcludeID is not null && item.Key.PlayerID != ExcludeID))
                {
                    SendData(Buffer, item.Value);
                    Console.WriteLine($"send message to {item.Key.PlayerID}");
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
                if (ListenSocket?.Poll(0, SelectMode.SelectRead) is true)
                {
                    client = ListenSocket.Accept();
                    Console.WriteLine($"client connected!");
                }

                return client;
            }
        }
    }
}
