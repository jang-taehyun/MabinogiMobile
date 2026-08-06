using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CoreModule;

namespace MabinogiMobileServer
{
    class NetworkManager : IDisposable
    {
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
        public List<Player> CloseClientList { get; private set; } = new List<Player>();

        private NetworkManager() { }

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
                SendData(PacketID.AllocatedPlayerID, new AllocatedPlayerIDPacket(NewPlayer.PlayerID).Buffer, NewClient);

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
                        SendData(PacketID.Transform, packet.Buffer, NewClient);
                    }
                }

                // broadcast packet that new client connected
                packet = new TransformPacket(NewPlayer.PlayerID, NewPlayer.Transform);
                Broadcast(PacketID.Transform, packet.Buffer, NewPlayer.PlayerID);
            }
        }

        public IPacket? ReadData(out PacketID ID, Player player)
        {
            IPacket? packet = null;
            ID = PacketID.Unknown;

            // close client
            if (player.sock.Poll(0, SelectMode.SelectRead) == true && player.sock.Available == 0)
            {
                CloseClientList.Add(player);
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

            byte[] buffer;
            if (ID is PacketID.AllocatedPlayerID)
            {
                buffer = new byte[AllocatedPlayerIDPacket.PacketSize];
                using (NetworkStream ns = new NetworkStream(player.sock))
                {
                    ns.ReadExactly(buffer, 0, buffer.Length);
                }

                packet = new AllocatedPlayerIDPacket(buffer);
            }

            if (ID is PacketID.Transform)
            {
                buffer = new byte[TransformPacket.PacketSize];
                using (NetworkStream ns = new NetworkStream(player.sock))
                {
                    ns.ReadExactly(buffer, 0, buffer.Length);
                }

                packet = new TransformPacket(buffer);
            }

            if (ID is PacketID.Attack)
            {
                buffer = new byte[AttackPacket.PacketSize];
                using (NetworkStream ns = new NetworkStream(player.sock))
                {
                    ns.ReadExactly(buffer, 0, buffer.Length);
                }

                packet = new AttackPacket(buffer);
            }

            return packet;
        }

        public void SendData(PacketID ID, byte[] Buffer, Socket client)
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
                    SendData(ID, Buffer, item.Value.sock);
                }
            }
        }

        public void CloseClientSocket()
        {
            while (CloseClientList.Count > 0)
            {
                int ClosePlayerID = CloseClientList[0].PlayerID;
                Console.WriteLine($"[close client] : {CloseClientList[0].PlayerID}");

                ClientList.Remove(CloseClientList[0].PlayerID);
                CloseClientList[0].sock.Shutdown(SocketShutdown.Both);
                CloseClientList[0].sock.Close();
                CloseClientList.RemoveAt(0);

                Broadcast(PacketID.CloseClient, new CloseClientPacket(ClosePlayerID).Buffer);
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
