using CoreModule;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace MabinogiMobileServer
{
    class NetworkManager : IDisposable
    {
        // singleton //
        private NetworkManager() { }
        private static NetworkManager? _inst;
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
        public Dictionary<int, Player> ClientList { get; private set; } = new Dictionary<int, Player>();
        public Queue<Player> CloseClientQueue { get; private set; } = new Queue<Player>();

        // todo : if you add packet, register PacketObjectGenerator
        public readonly Dictionary<PacketID, Func<Socket, IPacket>> PacketObjectGenerator = new Dictionary<PacketID, Func<Socket, IPacket>>()
        {
            { PacketID.AllocatedPlayerID,   (Socket sock) => new AllocatedPlayerIDPacket(ReadData(sock, AllocatedPlayerIDPacket.PacketSize))    },
            { PacketID.Transform,           (Socket sock) => new TransformPacket(ReadData(sock, TransformPacket.PacketSize))                    },
            { PacketID.Attack,              (Socket sock) => new AttackPacket(ReadData(sock, AttackPacket.PacketSize))                          },
            { PacketID.CloseClient,         (Socket sock) => new CloseClientPacket(ReadData(sock, CloseClientPacket.PacketSize))                },
        };

        public void RunServer() => listener.Run();

        public void AcceptClient()
        {
            Socket? NewClient = listener.AcceptClient();
            if (NewClient is not null)
            {
                // allocate client ID to New client
                Player NewPlayer = new Player()
                { 
                    sock = NewClient
                };
                do
                {
                    NewPlayer.PlayerID = new Random().Next(1, 100);
                } while (ClientList.ContainsKey(NewPlayer.PlayerID) is true);
                SendPacket(PacketID.AllocatedPlayerID, new AllocatedPlayerIDPacket(NewPlayer.PlayerID).Buffer, NewClient);

                // add new client to ClientList
                ClientList.Add(NewPlayer.PlayerID, NewPlayer);
                Console.WriteLine($"New Client connected : {NewPlayer.PlayerID}");

                // send client list info to New client
                TransformPacket packet;
                foreach (var item in ClientList)
                {
                    if (item.Key != NewPlayer.PlayerID)
                    {
                        packet = new TransformPacket(item.Value.PlayerID, item.Value.Transform);
                        SendPacket(PacketID.Transform, packet.Buffer, NewClient);
                    }
                }

                // broadcast packet that new client connected
                packet = new TransformPacket(NewPlayer.PlayerID, NewPlayer.Transform);
                Broadcast(PacketID.Transform, packet.Buffer, NewPlayer.PlayerID);
            }
        }

        public IPacket? ReadPacket(out PacketID ID, Player player)
        {
            IPacket? packet = null;
            ID = PacketID.Unknown;

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
                int ReadLen = 0;
                while (ReadLen < header.Length)
                {
                    ReadLen += ns.Read(header, ReadLen, PacketHeader.HeaderSize - ReadLen);
                }
                ID = PacketHeader.DeserializePacketHeader(header);
            }

            // read data
            return PacketObjectGenerator[ID].Invoke(player.sock);
        }

        public void SendPacket(PacketID ID, byte[] Buffer, Socket client)
        {
            byte[] packet = PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(ID), Buffer);
            using (NetworkStream ns = new NetworkStream(client))
            {
                ns.Write(packet, 0, packet.Length);
            }
        }

        public void Broadcast(PacketID ID, byte[] Buffer, int? ExcludeID = null)
        {
            foreach(var item in ClientList)
            {
                if(ExcludeID is null || (ExcludeID is not null && item.Key != ExcludeID))
                {
                    SendPacket(ID, Buffer, item.Value.sock);
                }
            }
        }

        private static byte[] ReadData(Socket socket, int PacketSize)
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

                ClientList.Remove(CloseClient.PlayerID);
                CloseClient.sock.Shutdown(SocketShutdown.Both);
                CloseClient.sock.Close();

                Broadcast(PacketID.CloseClient, new CloseClientPacket(CloseClient.PlayerID).Buffer);
            }
        }

        public void Dispose()
        {
            // end
            foreach (var item in ClientList)
                item.Value.sock.Close();
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
