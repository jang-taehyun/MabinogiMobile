using CoreModule;

namespace MabinogiMobileServer
{
    internal class ServerMain
    {
        static void Main(string[] args)
        {
            NetworkManager.NetworkManagerInstance.RunServer();

            while (true)
            {
                // accept client
                NetworkManager.NetworkManagerInstance.AcceptClient();

                // process packet
                foreach (var item in NetworkManager.NetworkManagerInstance.ClientList)
                {
                    PacketID ID = PacketID.Unknown;
                    IPacket? ReceivePacket = NetworkManager.NetworkManagerInstance.ReadPacket(ID: out ID, item.Value);

                    if (ReceivePacket is not null)
                        PacketHandler.handler[ID].Invoke(ReceivePacket);
                }

                // close client
                NetworkManager.NetworkManagerInstance.CloseClientSocket();
            }
        }
    }
}
