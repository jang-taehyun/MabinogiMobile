using CoreModule;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MabinogiMobileServer
{
    class NetworkManager
    {
        // singleton //
        private NetworkManager() { }
        private static NetworkManager? _inst;
        public static NetworkManager Instance
        {
            get
            {
                if (_inst is null)
                    _inst = new NetworkManager();
                return _inst;
            }
        }

        Listener ListenerSocket = Listener.Instance;
        public Queue<Player> CloseClientQueue { get; private set; } = new Queue<Player>();

        public void RunServer() => ListenerSocket.Run();

        public void AcceptClient()
        {
            Socket? newClient = ListenerSocket.AcceptClient();
            if (newClient is not null)
            {
                // allocate player ID to New client
                Player newPlayer = new Player()
                { 
                    sock = newClient
                };
                do
                {
                    newPlayer.PlayerID = new Random().Next(1, 100);
                } while (GameManager.Instance.ConntectedClient.ContainsKey(newPlayer.PlayerID) is true);
                IPacket newPlayerIdPacket = new AllocatedPlayerIDPacket(newPlayer.PlayerID);
                SendPacket(PacketID.AllocatedPlayerID, newPlayerIdPacket, newClient);

                // add new client to ClientList
                GameManager.Instance.AddPlayer(newPlayer);
                Console.WriteLine($"New Client connected : {newPlayer.PlayerID}");

                // send client list info to New client
                foreach (var item in GameManager.Instance.ConntectedClient)
                {
                    if (item.Key != newPlayer.PlayerID)
                    {
                        TransformPacket OtherPlayerInfoPacket = new TransformPacket(item.Value.PlayerID, item.Value.Transform);
                        SendPacket(PacketID.Transform, OtherPlayerInfoPacket, newClient);
                    }
                }

                // broadcast packet that new client connected
                TransformPacket newPlayerInfoPacket = new TransformPacket(newPlayer.PlayerID, newPlayer.Transform);
                Broadcast(PacketID.Transform, newPlayerInfoPacket, newPlayer.PlayerID);
            }
        }

        public IPacketHandler? ReadPacket(out PacketID id, Player player)
        {
            IPacketHandler? packet = null;
            id = PacketID.Unknown;
            int packetSize = 0;

            // close client
            if (player.sock.Poll(0, SelectMode.SelectRead) == true && player.sock.Available == 0)
            {
                CloseClientQueue.Enqueue(player);
                return packet;
            }

            // no data to read
            if ((player.sock.Available > 0) is false)
                return packet;

            // read header
            using (NetworkStream ns = new NetworkStream(player.sock))
            {
                byte[] header = new byte[PacketHeader.HeaderSize];
                int readLen = 0;
                while (readLen < header.Length)
                {
                    readLen += ns.Read(header, readLen, PacketHeader.HeaderSize - readLen);
                }
                PacketHeader.DeserializePacketHeader(header, out id, out packetSize);
            }

            // read data
            return PacketHandlerGenerator.Generator[id].Invoke(player.sock, packetSize);
        }

        public void SendPacket(PacketID id, IPacket data, Socket client)
        {
            byte[] packet = PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(id, data.PacketSize), data.SerializePacket());
            using (NetworkStream ns = new NetworkStream(client))
            {
                ns.Write(packet, 0, packet.Length);
            }
        }

        public void Broadcast(PacketID id, IPacket data, int? excludeID = null)
        {
            foreach(var item in GameManager.Instance.ConntectedClient)
            {
                if(excludeID is null || (excludeID is not null && item.Key != excludeID))
                {
                    SendPacket(id, data, item.Value.sock);
                }
            }
        }

        public static byte[] ReadData(Socket socket, int PacketSize)
        {
            byte[] buffer = new byte[PacketSize];
            using (NetworkStream ns = new NetworkStream(socket))
            {
                ns.ReadExactly(buffer, 0, buffer.Length);
            }

            return buffer;
        }

        public void CloseClientSocket()
        {
            while (CloseClientQueue.Count > 0)
            {
                Player CloseClient = CloseClientQueue.Dequeue();
                Console.WriteLine($"[close client] : {CloseClient.PlayerID}");

                GameManager.Instance.RemovePlayer(CloseClient);
                Broadcast(PacketID.CloseClient, new CloseClientPacket(CloseClient.PlayerID));
            }
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
                        throw new MobinogiException("Address is null");

                    ListenSocket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                    if (ListenSocket is null)
                        throw new MobinogiException("Listener is null");

                    ListenSocket.Bind(LocalAddress);
                }
                catch (MobinogiException e)
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
