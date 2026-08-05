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

                byte[] buffer = PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(PacketID.AllocatedPlayerID), new AllocatedPlayerIDPacket(NewPlayer.PlayerID).Buffer);
                SendData(buffer, NewClient);

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
                        SendData(PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(PacketID.Transform), packet.Buffer), NewClient);
                    }
                }

                // broadcast packet that new client connected
                packet = new TransformPacket(NewPlayer.PlayerID, NewPlayer.Transform);
                Broadcast(PacketHeader.AppendPacket(PacketHeader.SerializePacketHeader(PacketID.Transform), packet.Buffer), NewPlayer.PlayerID);
            }
        }

        public IPacket? ReadData(PacketID packetID, Socket socket)
        {
            IPacket? packet = null;

            if ((socket.Available > 0) is false)
                return packet;

            byte[] buffer;
            if (packetID is PacketID.AllocatedPlayerID)
            {
                buffer = new byte[AllocatedPlayerIDPacket.PacketSize];
                using (NetworkStream ns = new NetworkStream(socket))
                {
                    ns.ReadExactly(buffer, 0, buffer.Length);
                }

                packet = new AllocatedPlayerIDPacket(buffer);
            }

            if (packetID is PacketID.Transform)
            {
                buffer = new byte[TransformPacket.PacketSize];
                using (NetworkStream ns = new NetworkStream(socket))
                {
                    ns.ReadExactly(buffer, 0, buffer.Length);
                }

                packet = new TransformPacket(buffer);
            }

            return packet;
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
                if(ExcludeID is null || (ExcludeID is not null && item.Key != ExcludeID))
                {
                    SendData(Buffer, item.Value.sock);
                }
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
