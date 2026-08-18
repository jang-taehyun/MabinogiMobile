using CoreModule;
using System;
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

        // listen socket //
        Listener listener = Listener.Instance;
        public void RunServer() => listener.Run();

        // accept //
        public async Task AcceptClient()
        {
            while(true)
            {
                // accept new client
                Socket newClient = await listener.AcceptClient();

                // allocate player ID to New client
                // todo : temp code
                int newPlayerID = new Random().Next(1, 100);
                while (PlayerManager.Instance[newPlayerID] is not null)
                    newPlayerID = new Random().Next(1, 100);

                // create & add new player
                Player newPlayer = new Player()
                {
                    ClientSocket = newClient,
                    PlayerID = newPlayerID,
                };
                PlayerManager.Instance.AddPlayer(newPlayer);
                Console.WriteLine($"New Client connected : {newPlayerID}");

                // create accept process
                var acceptProcess = new
                {
                    Process = (Action)delegate ()
                    {
                        Player player = PlayerManager.Instance[newPlayerID]!;

                        // send world state to new player
                        IPacket initWorldStatePacket = new InitialWorldStatePacket(newPlayerID, PlayerManager.Instance.SerializePlayerInfomations(player));
                        _ = NetworkManager.Instance.SendPacket(PacketID.InitialWorldState, initWorldStatePacket, player.ClientSocket);

                        // broadcast packet that new client connected
                        TransformPacket newPlayerInfoPacket = new TransformPacket(player.PlayerID, player.Position, player.Forward);
                        NetworkManager.Instance.Broadcast(PacketID.Transform, newPlayerInfoPacket, player.PlayerID);

                        // create read loop
                        _ = ReadPacket(player);
                    }
                };

                // enter accept process to job queue
                JobManager.Instance.EnqueueJob(acceptProcess);
            }
        }

        // read //
        private async Task ReadPacket(Player player)
        {
            // read header
            byte[] header = new byte[PacketHeader.HeaderSize];

            try
            {
                while (true)
                {
                    int readLength = 0;
                    while (readLength < header.Length)
                    {
                        int length = await player.ClientSocket.ReceiveAsync(header.AsMemory<byte>(readLength));
                        readLength += length;

                        // close client
                        if (length is 0)
                            break;
                    }

                    // deserialize header
                    PacketID id = PacketID.Unknown;
                    int dataSize = 0;
                    PacketHeader.DeserializePacketHeader(header, out id, out dataSize);

                    // read data
                    byte[] data = new byte[dataSize];
                    readLength = 0;
                    while (readLength < data.Length)
                        readLength += await player.ClientSocket.ReceiveAsync(data.AsMemory<byte>(readLength));

                    // create packet handler & enter job queue
                    JobManager.Instance.EnqueueJob(PacketHandler.Generator[id].Invoke(data));
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine($"exception in read packet {player.PlayerID}, error code : {e.ErrorCode}");
                Console.WriteLine(e.Message);
            }
            finally
            {
                // enter close process to job queue
                int disconnectedPlayerId = player.PlayerID;
                var closeClientProcess = new
                {
                    Process = (Action)delegate ()
                    {
                        Player disconnectedPlayer = PlayerManager.Instance[disconnectedPlayerId]!;
                        CloseClientSocket(disconnectedPlayer);
                        Console.WriteLine($"client close {disconnectedPlayerId}");
                    }
                };
                JobManager.Instance.EnqueueJob(closeClientProcess);
            }
        }

        // write //
        public async Task SendPacket(PacketID id, IPacket data, Socket client)
        {
            byte[] packet = PacketHeader.AppendHeader(id, data.SerializeData());
            int sendLen = 0;
            while (sendLen < packet.Length)
                sendLen += await client.SendAsync(packet.AsMemory<byte>(sendLen));
        }
        public void Broadcast(PacketID id, IPacket data, int? excludeID = null)
        {
            foreach(Player player in PlayerManager.Instance)
            {
                if(excludeID is null || (excludeID is not null && player.PlayerID != excludeID))
                {
                    _= SendPacket(id, data, player.ClientSocket);
                }
            }
        }

        // close //
        public void CloseClientSocket(Player disconnectedPlayer)
        {
            Console.WriteLine($"[close client] : {disconnectedPlayer.PlayerID}");
            PlayerManager.Instance.RemovePlayer(disconnectedPlayer.PlayerID);

            Broadcast(PacketID.CloseClient, new CloseClientPacket(disconnectedPlayer.PlayerID));
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
