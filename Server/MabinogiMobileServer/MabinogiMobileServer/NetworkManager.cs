using CoreModule;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

        Listener listener = Listener.Instance;

        public void RunServer() => listener.Run();

        public async Task AcceptClient()
        {
            // accept new client
            Socket newClient = await listener.AcceptClient();

            // allocate player ID to New client
            int newPlayerID = new Random().Next(1, 100);
            while (GameManager.Instance.ConntectedClient.ContainsKey(newPlayerID) is true)
                newPlayerID = new Random().Next(1, 100);

            // create & add new player
            Player newPlayer = new Player()
            {
                sock = newClient,
                PlayerID = newPlayerID,
            };
            GameManager.Instance.AddPlayer(newPlayer);
            Console.WriteLine($"New Client connected : {newPlayerID}");

            // send world state to new player
            IPacket initWorldStatePacket = new InitialWorldStatePacket(newPlayerID, GameManager.Instance.SerializePlayerInfomations(newPlayer));
            await SendPacket(PacketID.InitialWorldState, initWorldStatePacket, newClient);

            // broadcast packet that new client connected
            TransformPacket newPlayerInfoPacket = new TransformPacket(newPlayer.PlayerID, newPlayer.Transform);
            await Broadcast(PacketID.Transform, newPlayerInfoPacket, newPlayer.PlayerID);
        }

        public async Task ReadPacket(Player player)
        {
            // read header
            byte[] header = new byte[PacketHeader.HeaderSize];
            int readLen = 0;
            while (readLen < header.Length)
            {
                readLen += await player.sock.ReceiveAsync(header[readLen..]);

                // close client
                if (readLen is 0)
                {
                    await CloseClientSocket(player);
                    return;
                }
            }

            // deserialize header
            PacketID id = PacketID.Unknown;
            int dataSize = 0;
            PacketHeader.DeserializePacketHeader(header, out id, out dataSize);

            // read data
            byte[] data = new byte[dataSize];
            readLen = 0;
            while (readLen < data.Length)
                readLen += await player.sock.ReceiveAsync(data[readLen..]);

            // create packet handler & enter job queue
            GameManager.Instance.JobQueue.Enqueue(PacketHandler.Generator[id].Invoke(data));
        }

        public async Task SendPacket(PacketID id, IPacket data, Socket client)
        {
            byte[] packet = PacketHeader.AppendHeader(id, data.SerializeData());
            int sendLen = 0;
            while (sendLen < packet.Length)
                sendLen += await client.SendAsync(packet[sendLen..]);
        }

        public async Task Broadcast(PacketID id, IPacket data, int? excludeID = null)
        {
            foreach(var item in GameManager.Instance.ConntectedClient)
            {
                if(excludeID is null || (excludeID is not null && item.Key != excludeID))
                {
                    await SendPacket(id, data, item.Value.sock);
                }
            }
        }

        public async Task CloseClientSocket(Player disconnectedPlayer)
        {
            Console.WriteLine($"[close client] : {disconnectedPlayer.PlayerID}");
            GameManager.Instance.RemovePlayer(disconnectedPlayer);

            await Broadcast(PacketID.CloseClient, new CloseClientPacket(disconnectedPlayer.PlayerID));
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

            private Socket listenSocket = null!;
            private const int portNumber = 33355;

            private Listener()
            {
                try
                {
                    IPEndPoint LocalAddress = new IPEndPoint(address: IPAddress.Any, portNumber);
                    if (LocalAddress is null)
                        throw new MobinogiException("Address is null");

                    listenSocket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                    if (listenSocket is null)
                        throw new MobinogiException("Listener is null");

                    // set listener to non-blocking socket
                    listenSocket.NoDelay = true;
                    listenSocket.Blocking = false;

                    listenSocket.Bind(LocalAddress);
                }
                catch (MobinogiException e)
                {
                    e.OutputExceptionLog();
                }
            }

            public void Run() => listenSocket.Listen(100);

            public void Dispose()
            {
                listenSocket?.Close();
            }

            public async Task<Socket> AcceptClient()
            {
                Socket client = await listenSocket.AcceptAsync();
                Console.WriteLine($"client connected!");
                return client;
            }
        }
    }
}
